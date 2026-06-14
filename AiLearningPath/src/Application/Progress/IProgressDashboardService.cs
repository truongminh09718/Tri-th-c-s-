using AiLearningPath.Domain.Progress;

namespace AiLearningPath.Application.Progress;

/// <summary>
/// Dịch vụ tổng hợp tiến độ học tập cho Progress Dashboard (R8).
/// Điều phối nghiệp vụ: nạp dữ liệu học tập của sinh viên (lộ trình, phiên học, mốc
/// tiến độ, kết quả đánh giá) rồi gọi domain logic thuần <see cref="ProgressCalculator"/>
/// để tính Learning Score, tỷ lệ hoàn thành, tổng số giờ học và dữ liệu biểu đồ (R8.1).
/// Khi sinh viên chưa có dữ liệu học tập, trả về trạng thái rỗng kèm hướng dẫn (R8.4).
/// Mọi thao tác gắn với <c>userId</c> của tài khoản đã xác thực để hỗ trợ
/// ownership-based authorization (R14).
/// </summary>
public interface IProgressDashboardService
{
    /// <summary>
    /// Tổng hợp dashboard tiến độ của sinh viên (R8.1, R8.3, R8.4): Learning Score,
    /// tỷ lệ hoàn thành kế hoạch, tổng số giờ học và dữ liệu biểu đồ theo tuần/tháng.
    /// Khi chưa có dữ liệu học tập, trả về <see cref="DashboardView"/> ở trạng thái rỗng
    /// kèm thông điệp hướng dẫn bắt đầu học.
    /// </summary>
    /// <param name="userId">Định danh chủ sở hữu (tài khoản đã xác thực).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    Task<DashboardView> GetDashboardAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đánh dấu một nhiệm vụ học tập là đã hoàn thành (R8.2), sau đó cập nhật tỷ lệ hoàn
    /// thành kế hoạch và Learning Score, ghi nhận một mốc tiến độ mới và trả về dashboard
    /// đã cập nhật.
    /// </summary>
    /// <param name="userId">Định danh chủ sở hữu (tài khoản đã xác thực).</param>
    /// <param name="taskId">Định danh nhiệm vụ học tập cần đánh dấu hoàn thành.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <exception cref="DashboardTaskNotFoundException">
    /// Khi không tìm thấy nhiệm vụ thuộc về lộ trình của sinh viên.
    /// </exception>
    Task<DashboardView> CompleteTaskAsync(
        Guid userId, Guid taskId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Khung nhìn dashboard tiến độ trả về cho lớp API (R8.1, R8.4). Khi <see cref="IsEmpty"/>
/// là <c>true</c>, các chỉ số đều ở giá trị mặc định và <see cref="Guidance"/> chứa hướng
/// dẫn để sinh viên bắt đầu học.
/// </summary>
/// <param name="LearningScore">Learning Score tổng hợp trong [0, 100].</param>
/// <param name="CompletionRate">Tỷ lệ hoàn thành kế hoạch trong [0, 1].</param>
/// <param name="TotalStudyHours">Tổng số giờ học, không âm.</param>
/// <param name="CompletedTasks">Số nhiệm vụ đã hoàn thành trên toàn bộ lộ trình.</param>
/// <param name="TotalTasks">Tổng số nhiệm vụ trên toàn bộ lộ trình.</param>
/// <param name="Chart">Dữ liệu biểu đồ tiến độ tổng hợp theo tuần và tháng (R8.3).</param>
/// <param name="IsEmpty">Cờ trạng thái rỗng khi chưa có dữ liệu học tập (R8.4).</param>
/// <param name="Guidance">Thông điệp hướng dẫn bắt đầu học khi ở trạng thái rỗng (R8.4).</param>
public sealed record DashboardView(
    double LearningScore,
    double CompletionRate,
    double TotalStudyHours,
    int CompletedTasks,
    int TotalTasks,
    ChartData Chart,
    bool IsEmpty,
    string? Guidance);

/// <summary>
/// Ngoại lệ phát sinh khi không tìm thấy nhiệm vụ học tập thuộc về lộ trình của sinh viên
/// khi đánh dấu hoàn thành (R8.2). Lớp API chuyển thành phản hồi 404.
/// </summary>
public sealed class DashboardTaskNotFoundException : Exception
{
    public DashboardTaskNotFoundException(Guid taskId)
        : base($"Không tìm thấy nhiệm vụ học tập {taskId}.")
    {
        TaskId = taskId;
    }

    public Guid TaskId { get; }
}
