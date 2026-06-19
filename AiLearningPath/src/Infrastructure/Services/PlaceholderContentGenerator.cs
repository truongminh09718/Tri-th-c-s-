using System.Globalization;
using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.Career;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Paths;
using AiLearningPath.Domain.Common;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai placeholder (xác định) của <see cref="IContentGenerator"/> dùng khi dịch vụ
/// Gemini thật CHƯA được cấu hình (thiếu API key/endpoint trong appsettings).
///
/// Mục đích: cho phép ứng dụng khởi động và chạy luồng end-to-end (đăng ký → profile →
/// goal → assessment → DNA → path → dashboard → twin → career) mà không phụ thuộc mạng
/// hay khóa API thật. Nội dung sinh ra là deterministic và gắn với Learning Goal/nghề
/// nghiệp nhận được, có cùng cấu trúc JSON mà các service ở lớp Application kỳ vọng phân
/// tích — nên hành vi giống hệt các stub đã dùng trong kiểm thử.
///
/// LƯU Ý VẬN HÀNH: đây KHÔNG phải nội dung do AI thật sinh ra. Khi triển khai production,
/// cấu hình section "Gemini" (ApiKey + Endpoint) và thay bằng adapter HTTP gọi Gemini thật.
/// </summary>
public sealed class PlaceholderContentGenerator : IContentGenerator
{
    /// <inheritdoc />
    public Task<ContentGenerationResult> GenerateAsync(
        ContentGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var content = request.Kind switch
        {
            AssessmentContentKind.Assessment => GenerateAssessment(request),
            PathContentKind.LearningPath => GenerateLearningPath(request),
            CareerContentKind.CareerPath => GenerateCareerPath(request),
            _ => GenerateLearningPath(request),
        };

        return Task.FromResult(new ContentGenerationResult(content));
    }

    /// <summary>
    /// Sinh bộ câu hỏi đánh giá deterministic theo Learning Goal (R5.1). Số câu hỏi lấy từ
    /// tham số <c>questionCount</c> nếu có, ngược lại dùng <see cref="AssessmentDefaults.QuestionCount"/>.
    ///
    /// <para>
    /// Ưu tiên lấy câu hỏi THẬT từ <see cref="AssessmentQuestionBank"/> cho các Learning Goal
    /// được hỗ trợ (TOEIC, IELTS, môn đại học, Frontend/Backend, Data Analyst, AI Engineer),
    /// nhờ đó chức năng đánh giá đo được năng lực thật ngay cả khi chưa cấu hình Gemini. Với
    /// goal chưa có trong ngân hàng, sinh cấu trúc tổng hợp tối thiểu để giữ luồng hoạt động.
    /// </para>
    /// </summary>
    private static string GenerateAssessment(ContentGenerationRequest request)
    {
        var count = AssessmentDefaults.QuestionCount;
        if (request.Parameters.TryGetValue("questionCount", out var raw) &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0)
        {
            count = parsed;
        }

        var goal = request.LearningGoal;

        // Ưu tiên ngân hàng câu hỏi thật theo Learning Goal được hỗ trợ.
        var bankQuestions = AssessmentQuestionBank.GetQuestions(goal, count);
        if (bankQuestions.Count > 0)
        {
            var fromBank = bankQuestions
                .Select(t => new GeneratedQuestion(
                    Id: Guid.NewGuid(),
                    SkillArea: t.SkillArea,
                    Prompt: t.Prompt,
                    Options: t.Options,
                    CorrectOption: t.CorrectOption))
                .ToList();

            return JsonFieldSerializer.Serialize(new GeneratedAssessmentContent(fromBank));
        }

        // Fallback cho goal chưa có trong ngân hàng: cấu trúc tổng hợp tối thiểu, vẫn xác định.
        var questions = Enumerable.Range(1, count)
            .Select(i => new GeneratedQuestion(
                Id: Guid.NewGuid(),
                SkillArea: $"{goal}-Skill-{i}",
                Prompt: $"[{goal}] Câu hỏi mẫu số {i}?",
                Options: new[] { "A", "B", "C", "D" },
                CorrectOption: "A"))
            .ToList();

        return JsonFieldSerializer.Serialize(new GeneratedAssessmentContent(questions));
    }

    /// <summary>
    /// Sinh nội dung lộ trình học tập deterministic, không rỗng, gắn với Learning Goal (R7.1).
    /// </summary>
    private static string GenerateLearningPath(ContentGenerationRequest request) =>
        $"[{request.LearningGoal}] Học kiến thức trọng tâm, thực hành có hướng dẫn, "
        + "tự kiểm tra kết quả và ghi lại lỗi sai cần ôn tập.";

    /// <summary>
    /// Sinh lộ trình nghề nghiệp deterministic kèm chứng chỉ/dự án theo nghề (R10.2, R10.3).
    /// </summary>
    private static string GenerateCareerPath(ContentGenerationRequest request)
    {
        var career = request.LearningGoal;
        var content = new GeneratedCareerContent(
            Skills: new[] { $"{career}-Skill-1", $"{career}-Skill-2", $"{career}-Skill-3" },
            Certifications: new[] { $"{career}-Cert-1", $"{career}-Cert-2" },
            Projects: new[] { $"{career}-Project-1", $"{career}-Project-2" });

        return JsonFieldSerializer.Serialize(content);
    }
}
