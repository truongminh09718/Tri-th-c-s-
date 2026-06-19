using AiLearningPath.Application.Assessments;
using AiLearningPath.Domain.Assessments;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiLearningPath.Api.Controllers;

/// <summary>
/// Endpoint bắt đầu và nộp bài đánh giá năng lực đầu vào (R5).
/// Mọi route đều gắn <c>{userId}</c> để áp dụng ownership-based authorization (R14):
/// middleware <c>OwnershipAuthorizationMiddleware</c> đảm bảo người gọi chỉ thao tác
/// trên bài đánh giá của chính mình (401 nếu chưa xác thực, 403 nếu truy cập tài khoản khác).
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/assessments")]
[Produces("application/json")]
public sealed class AssessmentController : ControllerBase
{
    private readonly IAssessmentEngine _assessmentEngine;

    public AssessmentController(IAssessmentEngine assessmentEngine)
    {
        _assessmentEngine = assessmentEngine ?? throw new ArgumentNullException(nameof(assessmentEngine));
    }

    /// <summary>
    /// Bắt đầu một bài đánh giá cho một Learning Goal đã chọn (R5.1): sinh bộ câu hỏi
    /// tương ứng và lưu bài đánh giá, trả về câu hỏi (không kèm đáp án đúng) cho sinh viên.
    /// </summary>
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartAssessmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Start(
        Guid userId, [FromBody] StartAssessmentRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.LearningGoal))
        {
            return BadRequest(ApiErrorResponse.Create(
                "VALIDATION_ERROR", "Mục tiêu học tập không được để trống."));
        }

        var assessment = await _assessmentEngine.StartAsync(userId, request.LearningGoal, cancellationToken);
        var response = StartAssessmentResponse.From(assessment);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Nộp bài đánh giá đã hoàn thành (R5.2, R5.3, R5.4): chấm bài và trả về kết quả
    /// gồm trình độ, điểm mạnh, điểm yếu và điểm số.
    /// </summary>
    [HttpPost("{assessmentId:guid}/submit")]
    [ProducesResponseType(typeof(AssessmentResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(
        Guid userId,
        Guid assessmentId,
        [FromBody] SubmitAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null || request.Answers is null)
        {
            return BadRequest(ApiErrorResponse.Create(
                "VALIDATION_ERROR", "Danh sách câu trả lời không được để trống."));
        }

        var answers = request.Answers
            .Select(a => new Answer(a.QuestionId, a.SelectedOption))
            .ToList();

        var result = await _assessmentEngine.SubmitAsync(userId, assessmentId, answers, cancellationToken);
        return Ok(AssessmentResultResponse.From(result));
    }
}

/// <summary>Body yêu cầu bắt đầu bài đánh giá.</summary>
/// <param name="LearningGoal">Mục tiêu học tập của bài đánh giá (phải thuộc danh mục hỗ trợ).</param>
public sealed record StartAssessmentRequest(string LearningGoal);

/// <summary>Body yêu cầu nộp bài đánh giá.</summary>
/// <param name="Answers">Danh sách câu trả lời của sinh viên.</param>
public sealed record SubmitAssessmentRequest(IReadOnlyList<AnswerInput> Answers);

/// <summary>Một câu trả lời trong yêu cầu nộp bài.</summary>
/// <param name="QuestionId">Định danh câu hỏi được trả lời.</param>
/// <param name="SelectedOption">Lựa chọn của sinh viên (rỗng nghĩa là chưa trả lời).</param>
public sealed record AnswerInput(Guid QuestionId, string? SelectedOption);

/// <summary>
/// Phản hồi khi bắt đầu bài đánh giá. Trả về danh sách câu hỏi <b>không kèm đáp án đúng</b>
/// để tránh lộ đáp án phía client.
/// </summary>
public sealed record StartAssessmentResponse(
    Guid Id,
    Guid UserId,
    string LearningGoal,
    string Status,
    IReadOnlyList<AssessmentQuestionView> Questions)
{
    public static StartAssessmentResponse From(Assessment assessment)
    {
        var stored = JsonFieldSerializer.Deserialize<List<AssessmentQuestionItem>>(assessment.QuestionsJson)
            ?? new List<AssessmentQuestionItem>();

        var questions = stored
            .Select(q => new AssessmentQuestionView(q.Id, q.SkillArea, q.Prompt, q.Options))
            .ToList();

        return new StartAssessmentResponse(
            assessment.Id, assessment.UserId, assessment.LearningGoal, assessment.Status, questions);
    }
}

/// <summary>Câu hỏi hiển thị cho sinh viên (không kèm đáp án đúng).</summary>
/// <param name="Id">Định danh câu hỏi.</param>
/// <param name="SkillArea">Lĩnh vực kỹ năng.</param>
/// <param name="Prompt">Nội dung câu hỏi.</param>
/// <param name="Options">Danh sách lựa chọn trả lời.</param>
public sealed record AssessmentQuestionView(
    Guid Id,
    string SkillArea,
    string Prompt,
    IReadOnlyList<string> Options);

/// <summary>Phản hồi kết quả chấm bài đánh giá (R5.2).</summary>
public sealed record AssessmentResultResponse(
    Guid Id,
    Guid AssessmentId,
    Guid UserId,
    string Level,
    double Score,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<SkillBreakdownItem> SkillBreakdown)
{
    public static AssessmentResultResponse From(AssessmentResult result) => new(
        result.Id,
        result.AssessmentId,
        result.UserId,
        result.Level,
        result.Score,
        JsonFieldSerializer.Deserialize<List<string>>(result.StrengthsJson) ?? new List<string>(),
        JsonFieldSerializer.Deserialize<List<string>>(result.WeaknessesJson) ?? new List<string>(),
        JsonFieldSerializer.Deserialize<List<SkillBreakdownItem>>(result.SkillBreakdownJson)
            ?? new List<SkillBreakdownItem>());
}
