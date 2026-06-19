using System.Collections.Generic;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Infrastructure.Services;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Twin;

/// <summary>
/// Property-based test cho <see cref="IPredictionService"/> (R18.5): xác suất đạt mục tiêu
/// do dịch vụ dự đoán trả về phải <b>đơn điệu không giảm theo thời lượng học mỗi ngày</b>
/// (HoursPerDay), với mọi xác suất nằm trong khoảng hợp lệ [0, 1].
///
/// System-under-test là <see cref="PlaceholderPredictionService"/> — mô hình tham chiếu
/// deterministic đã đảm bảo tính đơn điệu (giống cách Property 17/18 dùng stub/reference
/// model). KHÔNG gọi ML Service Python thật; mô hình thật (task 18.4) được huấn luyện với hệ
/// số HoursPerDay dương nên cũng thỏa thuộc tính này, nhưng property test ở đây kiểm trên
/// triển khai dự phòng deterministic để chạy nhanh, ổn định và không phụ thuộc I/O.
///
/// Validates: Requirements 18.5
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public sealed class PredictionMonotonicByHoursProperties
{
    /// <summary>Sinh điểm trình độ hiện tại trong [0, 100] (giữ nguyên giữa hai lời gọi).</summary>
    private static Gen<double> CurrentLevelScoreGen =>
        Gen.Choose(0, 1000).Select(i => i / 10.0);

    /// <summary>Sinh một số giờ học mỗi ngày không âm trong [0, 24] với bước 0.1.</summary>
    private static Gen<double> HoursGen =>
        Gen.Choose(0, 240).Select(i => i / 10.0);

    /// <summary>Sinh một cặp giờ học không âm để sau đó sắp xếp thành h1 ≤ h2.</summary>
    private static Gen<(double, double)> HoursPairGen =>
        HoursGen.SelectMany(a => HoursGen.Select(b => (a, b)));

    private static PredictionFeatures BuildFeatures(double currentLevelScore, double hoursPerDay) =>
        new(
            LearningGoal: "IELTS",
            CurrentLevelScore: currentLevelScore,
            HoursPerDay: hoursPerDay,
            TargetDays: 90,
            Features: new Dictionary<string, double>());

    // Feature: ai-learning-path, Property 23: Dự đoán đơn điệu không giảm theo thời lượng học
    // Với mọi cặp PredictionFeatures chỉ khác nhau ở HoursPerDay (h1 <= h2), các trường khác
    // giữ nguyên, xác suất p(h1) <= p(h2), và mọi xác suất đều nằm trong [0, 1].
    [Property(MaxTest = 100)]
    public Property Prediction_IsMonotonicNonDecreasing_ByHoursPerDay()
    {
        return Prop.ForAll(
            Arb.From(CurrentLevelScoreGen),
            Arb.From(HoursPairGen),
            (currentLevelScore, hoursPair) =>
            {
                // Sắp xếp cặp giờ học thành h1 <= h2 (cả hai đều không âm).
                var h1 = System.Math.Min(hoursPair.Item1, hoursPair.Item2);
                var h2 = System.Math.Max(hoursPair.Item1, hoursPair.Item2);

                var service = new PlaceholderPredictionService();

                var p1 = service
                    .PredictSuccessProbabilityAsync(BuildFeatures(currentLevelScore, h1))
                    .GetAwaiter().GetResult();
                var p2 = service
                    .PredictSuccessProbabilityAsync(BuildFeatures(currentLevelScore, h2))
                    .GetAwaiter().GetResult();

                bool monotonic = p1 <= p2;
                bool inRange = p1 is >= 0d and <= 1d && p2 is >= 0d and <= 1d;

                return (monotonic && inRange).Label(
                    $"level={currentLevelScore} h1={h1} h2={h2} " +
                    $"=> p1={p1} p2={p2} (expected p1 <= p2, both within [0, 1])");
            });
    }
}
