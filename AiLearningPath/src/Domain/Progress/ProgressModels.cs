namespace AiLearningPath.Domain.Progress;

/// <summary>
/// Một phiên học ở dạng mô hình thuần (độc lập với entity EF Core
/// <c>StudySession</c>). Chỉ mang thông tin cần thiết để tính tổng số giờ học.
/// </summary>
/// <param name="StartedAt">Thời điểm bắt đầu phiên học.</param>
/// <param name="DurationHours">
/// Thời lượng phiên học tính bằng giờ. Giá trị âm hoặc không hợp lệ (NaN/Infinity)
/// được xử lý phòng vệ như 0 khi tính tổng.
/// </param>
public sealed record StudySessionInput(DateTime StartedAt, double DurationHours);

/// <summary>
/// Đầu vào thuần để tính Learning Score (R8.1, R8.2). Tổng hợp các tín hiệu tiến độ
/// sẵn có thành một chỉ số duy nhất nằm trong khoảng [0, 100].
/// </summary>
/// <param name="CompletionRate">
/// Tỷ lệ hoàn thành kế hoạch, kỳ vọng trong [0, 1]; giá trị ngoài khoảng được kẹp lại.
/// </param>
/// <param name="AssessmentScore">
/// Điểm đánh giá năng lực gần nhất, kỳ vọng trong [0, 100]; giá trị ngoài khoảng được kẹp lại.
/// </param>
/// <param name="StudyConsistency">
/// Mức độ đều đặn khi học, kỳ vọng trong [0, 1]; giá trị ngoài khoảng được kẹp lại.
/// </param>
public sealed record LearningScoreInput(
    double CompletionRate,
    double AssessmentScore,
    double StudyConsistency);

/// <summary>
/// Một mốc tiến độ ở dạng mô hình thuần (độc lập với entity EF Core
/// <c>ProgressSnapshot</c>), dùng làm đầu vào dựng dữ liệu biểu đồ.
/// </summary>
/// <param name="RecordedAt">Thời điểm ghi nhận mốc tiến độ.</param>
/// <param name="LearningScore">Learning Score tại thời điểm ghi nhận.</param>
/// <param name="CompletionRate">Tỷ lệ hoàn thành kế hoạch tại thời điểm ghi nhận.</param>
public sealed record ProgressPoint(
    DateTime RecordedAt,
    double LearningScore,
    double CompletionRate);

/// <summary>
/// Một điểm dữ liệu trên biểu đồ tiến độ đã được tổng hợp theo tuần hoặc tháng.
/// </summary>
/// <param name="Label">Nhãn của khoảng thời gian (ví dụ "2024-W12" hoặc "2024-03").</param>
/// <param name="AverageLearningScore">Learning Score trung bình trong khoảng thời gian.</param>
/// <param name="AverageCompletionRate">Tỷ lệ hoàn thành trung bình trong khoảng thời gian.</param>
public sealed record ChartPoint(
    string Label,
    double AverageLearningScore,
    double AverageCompletionRate);

/// <summary>
/// Dữ liệu biểu đồ tiến độ học tập (R8.1, R8.3) ở dạng mô hình thuần, gồm chuỗi điểm
/// tổng hợp theo tuần và theo tháng. Lớp ứng dụng/dashboard chịu trách nhiệm trình bày.
/// </summary>
/// <param name="Weekly">Chuỗi điểm tổng hợp theo tuần, sắp xếp tăng dần theo nhãn.</param>
/// <param name="Monthly">Chuỗi điểm tổng hợp theo tháng, sắp xếp tăng dần theo nhãn.</param>
public sealed record ChartData(
    IReadOnlyList<ChartPoint> Weekly,
    IReadOnlyList<ChartPoint> Monthly)
{
    /// <summary>Dữ liệu biểu đồ rỗng (chưa có mốc tiến độ nào).</summary>
    public static ChartData Empty { get; } = new(Array.Empty<ChartPoint>(), Array.Empty<ChartPoint>());
}
