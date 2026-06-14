using AiLearningPath.Application.Progress;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Progress;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiLearningPath.Api.Controllers;

/// <summary>
/// Endpoint theo dõi tiến độ học tập (R8): xem dashboard và đánh dấu hoàn thành nhiệm vụ.
/// Mọi route đều gắn <c>{userId}</c> để áp dụng ownership-based authorization (R14):
/// middleware <c>OwnershipAuthorizationMiddleware</c> đảm bảo người gọi chỉ xem/cập nhật
/// tiến độ của chính mình (401 nếu chưa xác thực, 403 nếu truy cập tài khoản khác).
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IProgressDashboardService _dashboardService;

    public DashboardController(IProgressDashboardService dashboardService)
    {
        _dashboardService = dashboardService ?? throw new ArgumentNullException(nameof(dashboardService));
    }

    /// <summary>
    /// Lấy dashboard tiến độ của sinh viên (R8.1, R8.3, R8.4): Learning Score, tỷ lệ hoàn
    /// thành kế hoạch, tổng số giờ học và dữ liệu biểu đồ theo tuần/tháng. Khi chưa có dữ
    /// liệu học tập, trả về trạng thái rỗng kèm hướng dẫn bắt đầu học.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.GetDashboardAsync(userId, cancellationToken);
        return Ok(DashboardResponse.From(dashboard));
    }

    /// <summary>
    /// Đánh dấu một nhiệm vụ học tập là đã hoàn thành (R8.2): cập nhật tỷ lệ hoàn thành kế
    /// hoạch và Learning Score rồi trả về dashboard đã cập nhật.
    /// </summary>
    [HttpPost("tasks/{taskId:guid}/complete")]
    [ProducesResponseType(typeof(DashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteTask(
        Guid userId, Guid taskId, CancellationToken cancellationToken)
    {
        var dashboard = await _dashboardService.CompleteTaskAsync(userId, taskId, cancellationToken);
        return Ok(DashboardResponse.From(dashboard));
    }
}

/// <summary>
/// Phản hồi dashboard tiến độ học tập (R8.1, R8.3, R8.4). Khi <see cref="IsEmpty"/> là
/// <c>true</c>, các chỉ số ở giá trị mặc định và <see cref="Guidance"/> chứa hướng dẫn
/// bắt đầu học.
/// </summary>
public sealed record DashboardResponse(
    double LearningScore,
    double CompletionRate,
    double TotalStudyHours,
    int CompletedTasks,
    int TotalTasks,
    ChartData Chart,
    bool IsEmpty,
    string? Guidance)
{
    public static DashboardResponse From(DashboardView view) => new(
        view.LearningScore,
        view.CompletionRate,
        view.TotalStudyHours,
        view.CompletedTasks,
        view.TotalTasks,
        view.Chart,
        view.IsEmpty,
        view.Guidance);
}
