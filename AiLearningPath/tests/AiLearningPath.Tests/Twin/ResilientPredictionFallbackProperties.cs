using System.Net;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Infrastructure.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AiLearningPath.Tests.Twin;

/// <summary>
/// Property-based test cho dự phòng an toàn (Resilient fallback, R18.2–R18.5) của
/// <see cref="ResilientPredictionService"/>. Khi adapter ML thật
/// (<see cref="MlPredictionService"/>) thất bại — lỗi HTTP, trả mã 500 hoặc hết thời
/// gian chờ — decorator KHÔNG được lan lỗi ra ngoài mà phải fallback sang
/// <see cref="PlaceholderPredictionService"/> thật và trả về một xác suất hợp lệ trong
/// khoảng [0, 1], đúng bằng giá trị placeholder cho cùng đặc trưng đầu vào.
///
/// Validates: Requirements 18.2, 18.3, 18.4, 18.5
/// Chạy tối thiểu 100 iteration với FsCheck.
///
/// Cách dựng primary lỗi: tạo <see cref="MlPredictionService"/> với một
/// <see cref="HttpClient"/> dùng <see cref="HttpMessageHandler"/> giả luôn gây thất bại
/// (ném <see cref="HttpRequestException"/>, trả 500, hoặc treo để kích hoạt timeout).
/// fallback là <see cref="PlaceholderPredictionService"/> thật (không mock).
/// </summary>
public class ResilientPredictionFallbackProperties
{
    /// <summary>Kiểu thất bại mà ML adapter sẽ biểu hiện.</summary>
    private enum FailureMode
    {
        ThrowHttpRequestException,
        ServerError500,
        Timeout,
    }

    /// <summary>
    /// Handler giả mô phỏng ML Service hỏng. Tùy <see cref="FailureMode"/>, nó ném lỗi
    /// HTTP, trả về 500, hoặc treo lâu hơn timeout để kích hoạt hủy do hết thời gian chờ.
    /// </summary>
    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        private readonly FailureMode _mode;

        public FailingHttpMessageHandler(FailureMode mode) => _mode = mode;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            switch (_mode)
            {
                case FailureMode.ThrowHttpRequestException:
                    throw new HttpRequestException("ML Service không phản hồi (giả lập lỗi mạng).");

                case FailureMode.Timeout:
                    // Treo cho tới khi token (đã liên kết với timeout của MlPredictionService) bị
                    // hủy, rồi ném OperationCanceledException để mô phỏng hết thời gian chờ (R18.4).
                    await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                    return new HttpResponseMessage(HttpStatusCode.OK); // không bao giờ tới đây

                case FailureMode.ServerError500:
                default:
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("internal error"),
                    };
            }
        }
    }

    private static ResilientPredictionService CreateResilientWithFailingPrimary(FailureMode mode)
    {
        var handler = new FailingHttpMessageHandler(mode);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ml-service.test") };

        // Timeout ngắn để trường hợp Timeout chạy nhanh trong test.
        var options = Options.Create(new MlServiceOptions
        {
            Endpoint = "http://ml-service.test",
            TimeoutSeconds = 1,
        });

        var primary = new MlPredictionService(
            httpClient, options, NullLogger<MlPredictionService>.Instance);
        var fallback = new PlaceholderPredictionService();

        return new ResilientPredictionService(
            primary, fallback, NullLogger<ResilientPredictionService>.Instance);
    }

    /// <summary>Sinh đặc trưng đầu vào hợp lệ cho mô hình dự đoán.</summary>
    private static Gen<PredictionFeatures> FeaturesGen =>
        from goal in Gen.Elements("IELTS", "GPA", "TOEIC", "SAT", "")
        from level in Gen.Choose(0, 1000).Select(i => i / 10.0)   // [0, 100]
        from hours in Gen.Choose(0, 240).Select(i => i / 10.0)    // [0, 24]
        from targetDays in Gen.Choose(1, 365)
        select new PredictionFeatures(
            LearningGoal: goal,
            CurrentLevelScore: level,
            HoursPerDay: hours,
            TargetDays: targetDays,
            Features: new Dictionary<string, double>());

    /// <summary>Sinh kiểu thất bại của ML adapter.</summary>
    private static Gen<FailureMode> FailureModeGen =>
        Gen.Elements(
            FailureMode.ThrowHttpRequestException,
            FailureMode.ServerError500,
            FailureMode.Timeout);

    // Feature: ai-learning-path, Property 25: Dự phòng an toàn của Prediction_Service khi dịch vụ ngoài lỗi
    // Với mọi PredictionFeatures hợp lệ và mọi kiểu thất bại của ML adapter (lỗi HTTP, 500,
    // timeout), ResilientPredictionService.PredictSuccessProbabilityAsync KHÔNG ném lỗi và
    // trả về xác suất trong [0, 1], đúng bằng giá trị placeholder cho cùng đặc trưng đó.
    [Fact]
    public async Task PredictSuccessProbability_WhenCallerCancellationIsRequested_PropagatesCancellation()
    {
        var resilient = CreateResilientWithFailingPrimary(FailureMode.Timeout);
        var features = new PredictionFeatures(
            LearningGoal: "GPA",
            CurrentLevelScore: 75,
            HoursPerDay: 2,
            TargetDays: 90,
            Features: new Dictionary<string, double>());

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => resilient.PredictSuccessProbabilityAsync(features, cts.Token));
    }

    [Property(MaxTest = 100)]
    public Property PredictSuccessProbability_FallsBackSafely_WhenMlAdapterFails()
    {
        return Prop.ForAll(
            Arb.From(FeaturesGen),
            Arb.From(FailureModeGen),
            (features, failureMode) =>
            {
                var resilient = CreateResilientWithFailingPrimary(failureMode);

                // 1) KHÔNG được ném lỗi ra ngoài (R18.2, R18.3).
                double actual = resilient
                    .PredictSuccessProbabilityAsync(features)
                    .GetAwaiter().GetResult();

                // 2) Phải trùng đúng giá trị placeholder cho cùng features.
                double expectedPlaceholder = new PlaceholderPredictionService()
                    .PredictSuccessProbabilityAsync(features)
                    .GetAwaiter().GetResult();

                // 3) Kết quả là xác suất hợp lệ trong [0, 1] (R18.4, R18.5).
                bool inRange = actual >= 0d && actual <= 1d;
                bool matchesPlaceholder = Math.Abs(actual - expectedPlaceholder) < 1e-12;

                return (inRange && matchesPlaceholder).Label(
                    $"failureMode={failureMode} hours={features.HoursPerDay} " +
                    $"level={features.CurrentLevelScore} => actual={actual} " +
                    $"expectedPlaceholder={expectedPlaceholder} (inRange={inRange})");
            });
    }
}
