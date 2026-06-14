namespace AiLearningPath.Domain.Paths;

/// <summary>
/// Nhiệm vụ học tập ở dạng mô hình thuần (độc lập với entity EF Core
/// <c>LearningTask</c>). Mang lĩnh vực kỹ năng, mô tả và số giờ ước lượng để
/// hoàn thành.
/// </summary>
/// <param name="Skill">Lĩnh vực kỹ năng mà nhiệm vụ tập trung.</param>
/// <param name="Description">Mô tả ngắn gọn của nhiệm vụ.</param>
/// <param name="EstimatedHours">Số giờ ước lượng để hoàn thành, không âm.</param>
public sealed record PathTaskPlan(string Skill, string Description, double EstimatedHours);

/// <summary>
/// Một ngày học trong lộ trình. <see cref="DayIndex"/> là số thứ tự ngày tuyệt đối
/// (đánh số từ 1) trong toàn bộ lộ trình, dùng để kiểm chứng việc bao phủ thời gian
/// mục tiêu không trùng/hở.
/// </summary>
/// <param name="DayIndex">Số thứ tự ngày tuyệt đối trong lộ trình (1-based).</param>
/// <param name="Task">Nhiệm vụ học tập của ngày.</param>
public sealed record PathDayPlan(int DayIndex, PathTaskPlan Task);

/// <summary>
/// Giai đoạn của lộ trình, ánh xạ tới một tuần cụ thể trong một tháng (cấp giữa của
/// phân cấp tháng → tuần → ngày). Độc lập với entity EF Core <c>PathPhase</c>.
/// </summary>
/// <param name="MonthIndex">Chỉ số tháng (1-based).</param>
/// <param name="WeekIndex">Chỉ số tuần trong tháng (1-based).</param>
/// <param name="Title">Tiêu đề giai đoạn.</param>
/// <param name="Days">Danh sách ngày học thuộc giai đoạn, theo thứ tự ngày tăng dần.</param>
public sealed record PathPhasePlan(
    int MonthIndex,
    int WeekIndex,
    string Title,
    IReadOnlyList<PathDayPlan> Days);

/// <summary>
/// Lộ trình học tập cá nhân hóa ở dạng mô hình thuần (độc lập với entity EF Core
/// <c>LearningPath</c>), được tổ chức phân cấp theo tháng → tuần → ngày (R7.1, R7.2).
/// Lớp ứng dụng chịu trách nhiệm ánh xạ <see cref="PathPlan"/> sang các entity
/// <c>LearningPath</c> / <c>PathPhase</c> / <c>LearningTask</c> để lưu trữ.
/// </summary>
/// <param name="LearningGoal">Mục tiêu học tập của lộ trình.</param>
/// <param name="TargetDays">Số ngày mục tiêu mà lộ trình phải bao phủ.</param>
/// <param name="Phases">Danh sách giai đoạn, theo thứ tự tháng rồi tuần tăng dần.</param>
public sealed record PathPlan(
    string LearningGoal,
    int TargetDays,
    IReadOnlyList<PathPhasePlan> Phases)
{
    /// <summary>Tất cả ngày học của lộ trình, gộp từ mọi giai đoạn.</summary>
    public IEnumerable<PathDayPlan> AllDays => Phases.SelectMany(p => p.Days);

    /// <summary>Tổng số ngày được bao phủ bởi toàn bộ giai đoạn.</summary>
    public int TotalDays => Phases.Sum(p => p.Days.Count);

    /// <summary>Tổng số giờ học ước lượng của toàn bộ nhiệm vụ trong lộ trình.</summary>
    public double TotalEstimatedHours => Phases.Sum(p => p.Days.Sum(d => d.Task.EstimatedHours));
}

/// <summary>
/// Kết quả ước lượng tính khả thi của lộ trình theo khoảng thời gian mục tiêu (R7.5).
/// </summary>
/// <param name="HasWarning">
/// True khi tổng số giờ học ước lượng vượt quá tổng số giờ khả dụng — cần cảnh báo.
/// </param>
/// <param name="EstimatedHours">Tổng số giờ học ước lượng của lộ trình.</param>
/// <param name="AvailableHours">Tổng số giờ khả dụng trong khoảng thời gian mục tiêu.</param>
public sealed record FeasibilityResult(bool HasWarning, double EstimatedHours, double AvailableHours);
