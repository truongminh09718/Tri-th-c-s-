using System.Net;
using System.Text.Json;
using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.Career;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Paths;
using AiLearningPath.Infrastructure.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AiLearningPath.Tests.ExternalServices;

/// <summary>
/// Property-based test cho <see cref="ResilientContentGenerator"/> — dự phòng an toàn khi
/// dịch vụ Gemini ngoài lỗi (R17.2, R17.3, R17.4, R17.5).
///
/// <para>
/// Adapter Gemini (<see cref="GeminiContentGenerator"/>) là sealed class dùng
/// <see cref="HttpClient"/>; để mô phỏng dịch vụ ngoài lỗi/timeout một cách sạch sẽ, ta
/// dựng nó với một <see cref="HttpMessageHandler"/> giả luôn ném
/// <see cref="HttpRequestException"/> / <see cref="TaskCanceledException"/> hoặc trả HTTP 5xx.
/// Fallback là <see cref="PlaceholderContentGenerator"/> thật (deterministic ngoại trừ
/// <c>Guid</c> Id của câu hỏi đánh giá).
/// </para>
///
/// Validates: Requirements 17.2, 17.3, 17.4, 17.5
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class ResilientContentGeneratorFallbackProperties
{
    /// <summary>Các loại nội dung hợp lệ mà Content_Generator hỗ trợ.</summary>
    private static readonly string[] Kinds =
    {
        AssessmentContentKind.Assessment, // "Assessment"
        PathContentKind.LearningPath,     // "LearningPath"
        CareerContentKind.CareerPath,     // "CareerPath"
    };

    /// <summary>Cách dịch vụ Gemini ngoài "lỗi" mà decorator phải dự phòng an toàn.</summary>
    private enum FailureMode
    {
        HttpRequestError,   // adapter ném HttpRequestException (mạng/HTTP lỗi) → R17.3
        Timeout,            // adapter bị hủy do timeout nội bộ → R17.4
        ServerError500,     // Gemini trả 500 → EnsureSuccessStatusCode ném → R17.3
        ServerError503,     // Gemini trả 503 → EnsureSuccessStatusCode ném → R17.3
    }

    /// <summary>
    /// <see cref="HttpMessageHandler"/> giả mô phỏng dịch vụ Gemini lỗi theo
    /// <see cref="FailureMode"/> đã chọn — không phụ thuộc mạng và phản hồi tức thì.
    /// </summary>
    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        private readonly FailureMode _mode;

        public FailingHttpMessageHandler(FailureMode mode) => _mode = mode;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            switch (_mode)
            {
                case FailureMode.HttpRequestError:
                    throw new HttpRequestException("Mô phỏng Gemini lỗi kết nối HTTP.");

                case FailureMode.Timeout:
                    // Mô phỏng timeout nội bộ: token liên kết (CancelAfter) bị hủy trong khi
                    // token gốc của caller chưa hủy → decorator coi là lỗi ngoài và fallback.
                    throw new TaskCanceledException("Mô phỏng Gemini hết thời gian chờ.");

                case FailureMode.ServerError500:
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

                case FailureMode.ServerError503:
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

                default:
                    throw new HttpRequestException("Lỗi không xác định.");
            }
        }
    }

    /// <summary>Dựng adapter Gemini lỗi với handler giả tương ứng và endpoint cấu hình.</summary>
    private static GeminiContentGenerator CreateFailingGemini(FailureMode mode)
    {
        var httpClient = new HttpClient(new FailingHttpMessageHandler(mode));
        var options = Options.Create(new GeminiOptions
        {
            ApiKey = "test-key",
            Endpoint = "https://gemini.test/v1/models/gemini:generateContent",
            TimeoutSeconds = 30,
        });

        return new GeminiContentGenerator(
            httpClient, options, NullLogger<GeminiContentGenerator>.Instance);
    }

    /// <summary>Sinh chuỗi mục tiêu học tập/nghề nghiệp không rỗng, không chỉ chứa khoảng trắng.</summary>
    private static Gen<string> LearningGoalGen =>
        Arb.Default.NonEmptyString().Generator
            .Select(s => s.Get)
            .Where(s => !string.IsNullOrWhiteSpace(s));

    /// <summary>Sinh bộ đầu vào: Kind hợp lệ + LearningGoal ngẫu nhiên + chế độ lỗi của Gemini.</summary>
    private static Gen<(string Kind, string Goal, FailureMode Mode)> InputGen =>
        from kind in Gen.Elements(Kinds)
        from goal in LearningGoalGen
        from mode in Gen.Elements(Enum.GetValues<FailureMode>())
        select (kind, goal, mode);

    // Feature: ai-learning-path, Property 24: Dự phòng an toàn của Content_Generator khi dịch vụ ngoài lỗi
    // Với mọi ContentGenerationRequest hợp lệ (Kind ∈ {Assessment, LearningPath, CareerPath} + LearningGoal
    // bất kỳ), khi adapter Gemini ném lỗi/timeout hoặc trả HTTP 5xx, ResilientContentGenerator.GenerateAsync
    // KHÔNG ném lỗi và trả về cùng kết quả (cùng schema, nội dung khớp ngoại trừ Id ngẫu nhiên) mà
    // PlaceholderContentGenerator trả cho cùng yêu cầu (R17.2, R17.3, R17.4, R17.5).
    [Property(MaxTest = 100)]
    public Property GenerateAsync_WhenGeminiFails_FallsBackToPlaceholderWithSameSchema()
    {
        return Prop.ForAll(Arb.From(InputGen), input =>
        {
            var (kind, goal, mode) = input;

            var request = new ContentGenerationRequest(
                kind, goal, new Dictionary<string, string>());

            var fallback = new PlaceholderContentGenerator();
            var resilient = new ResilientContentGenerator(
                CreateFailingGemini(mode),
                fallback,
                NullLogger<ResilientContentGenerator>.Instance);

            // (1) Không ném lỗi ra ngoài dù Gemini lỗi/timeout/5xx (R17.3, R17.4).
            ContentGenerationResult actual = resilient.GenerateAsync(request).GetAwaiter().GetResult();

            // Kết quả placeholder kỳ vọng cho cùng yêu cầu.
            ContentGenerationResult expected = fallback.GenerateAsync(request).GetAwaiter().GetResult();

            // (2) Trả về cùng kết quả/schema mà placeholder sinh cho yêu cầu đó (R17.2, R17.5).
            bool matches = ContentEquivalent(kind, expected.Content, actual.Content);

            return matches.Label(
                $"kind={kind} mode={mode} goal='{goal}'\nexpected={expected.Content}\nactual={actual.Content}");
        });
    }

    /// <summary>
    /// So khớp nội dung theo Kind: LearningPath/CareerPath là deterministic nên so sánh nguyên văn;
    /// Assessment có <c>Guid</c> Id ngẫu nhiên nên so sánh schema (số câu, SkillArea, Prompt, Options,
    /// CorrectOption) bỏ qua Id.
    /// </summary>
    private static bool ContentEquivalent(string kind, string expected, string actual)
    {
        if (kind == AssessmentContentKind.Assessment)
        {
            return AssessmentSchemaEquivalent(expected, actual);
        }

        // LearningPath (plain text) và CareerPath (JSON deterministic): khớp nguyên văn.
        return string.Equals(expected, actual, StringComparison.Ordinal);
    }

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>So sánh hai payload đánh giá theo cấu trúc, bỏ qua trường Id ngẫu nhiên.</summary>
    private static bool AssessmentSchemaEquivalent(string expected, string actual)
    {
        var e = JsonSerializer.Deserialize<GeneratedAssessmentContent>(expected, ReadOptions);
        var a = JsonSerializer.Deserialize<GeneratedAssessmentContent>(actual, ReadOptions);

        if (e?.Questions is null || a?.Questions is null)
        {
            return false;
        }

        if (e.Questions.Count != a.Questions.Count)
        {
            return false;
        }

        for (int i = 0; i < e.Questions.Count; i++)
        {
            var eq = e.Questions[i];
            var aq = a.Questions[i];

            if (!string.Equals(eq.SkillArea, aq.SkillArea, StringComparison.Ordinal) ||
                !string.Equals(eq.Prompt, aq.Prompt, StringComparison.Ordinal) ||
                !string.Equals(eq.CorrectOption, aq.CorrectOption, StringComparison.Ordinal))
            {
                return false;
            }

            var eOptions = eq.Options ?? Array.Empty<string>();
            var aOptions = aq.Options ?? Array.Empty<string>();
            if (!eOptions.SequenceEqual(aOptions, StringComparer.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
