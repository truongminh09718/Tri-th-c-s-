namespace AiLearningPath.Application.ExternalServices;

/// <summary>
/// Đặc trưng đầu vào cho mô hình dự đoán xác suất đạt mục tiêu (Academic Twin, R9).
/// Là dữ liệu thuần, không phụ thuộc I/O, để thuận tiện mock và kiểm thử.
/// </summary>
/// <param name="LearningGoal">Mục tiêu học tập đang hướng tới.</param>
/// <param name="CurrentLevelScore">Điểm trình độ hiện tại suy ra từ kết quả đánh giá.</param>
/// <param name="HoursPerDay">Số giờ học mỗi ngày dùng để mô phỏng.</param>
/// <param name="TargetDays">Khoảng thời gian mục tiêu (số ngày).</param>
/// <param name="Features">Đặc trưng bổ sung tùy chọn (tốc độ học, khả năng tập trung...).</param>
public sealed record PredictionFeatures(
    string LearningGoal,
    double CurrentLevelScore,
    double HoursPerDay,
    int TargetDays,
    IReadOnlyDictionary<string, double> Features);

/// <summary>
/// Trừu tượng hóa dịch vụ dự đoán ML (bọc ML Service Python + Scikit-learn).
/// Được tách thành interface để mock trong kiểm thử, cho phép kiểm thử logic
/// Academic Twin mà không gọi ML Service thật.
/// </summary>
public interface IPredictionService
{
    /// <summary>
    /// Dự đoán xác suất đạt mục tiêu, trả về giá trị trong khoảng [0, 1].
    /// Triển khai cụ thể (gọi ML Service) nằm ở lớp Infrastructure.
    /// </summary>
    Task<double> PredictSuccessProbabilityAsync(
        PredictionFeatures features,
        CancellationToken cancellationToken = default);
}
