using AiLearningPath.Application.Paths;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiLearningPath.Api.Controllers;

/// <summary>
/// Endpoint sinh lộ trình học tập cá nhân hóa (R7).
/// Mọi route đều gắn <c>{userId}</c> để áp dụng ownership-based authorization (R14):
/// middleware <c>OwnershipAuthorizationMiddleware</c> đảm bảo người gọi chỉ sinh/truy cập
/// lộ trình của chính mình (401 nếu chưa xác thực, 403 nếu truy cập tài khoản khác).
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/learning-paths")]
[Produces("application/json")]
public sealed class PathController : ControllerBase
{
    private readonly IPathGenerator _pathGenerator;

    public PathController(IPathGenerator pathGenerator)
    {
        _pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
    }

    /// <summary>
    /// Sinh lộ trình học tập cá nhân hóa cho sinh viên (R7.1–R7.5): kiểm tra tiên quyết
    /// (đã hoàn thành AI Assessment), sinh nội dung, xây cấu trúc tháng → tuần → ngày,
    /// ước lượng tính khả thi và lưu lộ trình kèm cảnh báo nếu thời gian không đủ.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(LearningPathResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate(
        Guid userId, [FromBody] GeneratePathRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(ApiErrorResponse.Create(
                "VALIDATION_ERROR", "Yêu cầu sinh lộ trình không được để trống."));
        }

        var path = await _pathGenerator.GenerateAsync(userId, request.TargetDays, cancellationToken);
        var response = LearningPathResponse.From(path);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}

/// <summary>Body yêu cầu sinh lộ trình.</summary>
/// <param name="TargetDays">Số ngày mục tiêu mà lộ trình phải bao phủ (phải >= 1).</param>
public sealed record GeneratePathRequest(int TargetDays);

/// <summary>
/// Phản hồi lộ trình học tập đã sinh, tổ chức phân cấp theo tháng → tuần → ngày (R7.1, R7.2)
/// kèm cờ cảnh báo tính khả thi (R7.5).
/// </summary>
public sealed record LearningPathResponse(
    Guid Id,
    Guid UserId,
    string LearningGoal,
    int TargetDays,
    bool FeasibilityWarning,
    DateTime CreatedAt,
    IReadOnlyList<PathPhaseView> Phases)
{
    public static LearningPathResponse From(LearningPath path)
    {
        var phases = path.Phases
            .OrderBy(p => p.MonthIndex)
            .ThenBy(p => p.WeekIndex)
            .Select(p => new PathPhaseView(
                p.Id,
                p.MonthIndex,
                p.WeekIndex,
                p.Title,
                p.Tasks
                    .Select(t => new LearningTaskView(t.Id, t.Skill, t.Description, t.EstimatedHours, t.Completed))
                    .ToList()))
            .ToList();

        return new LearningPathResponse(
            path.Id,
            path.UserId,
            path.LearningGoal,
            path.TargetDays,
            path.FeasibilityWarning,
            path.CreatedAt,
            phases);
    }
}

/// <summary>Giai đoạn của lộ trình (cấp tháng/tuần) trả về cho client.</summary>
public sealed record PathPhaseView(
    Guid Id,
    int MonthIndex,
    int WeekIndex,
    string Title,
    IReadOnlyList<LearningTaskView> Tasks);

/// <summary>Nhiệm vụ học tập trả về cho client.</summary>
public sealed record LearningTaskView(
    Guid Id,
    string Skill,
    string Description,
    double EstimatedHours,
    bool Completed);
