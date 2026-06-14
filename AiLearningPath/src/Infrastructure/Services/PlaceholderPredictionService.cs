using AiLearningPath.Application.ExternalServices;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai placeholder (xác định) của <see cref="IPredictionService"/> dùng khi ML
/// Service (Python + Scikit-learn) thật CHƯA được cấu hình (thiếu endpoint trong appsettings).
///
/// Mục đích: cho phép Academic Twin (R9) chạy end-to-end mà không phụ thuộc microservice ML
/// thật. Dùng một mô hình tham chiếu đơn giản, deterministic và đơn điệu không giảm theo
/// thời lượng học mỗi ngày — phù hợp với hành vi mà <see cref="AcademicTwinService"/> kỳ vọng
/// (xác suất nằm trong [0, 1], tăng dần theo số giờ học). Đây là cùng dạng "reference model"
/// được nhắc tới trong Testing Strategy của tài liệu thiết kế.
///
/// LƯU Ý VẬN HÀNH: đây KHÔNG phải dự đoán từ mô hình ML thật. Khi triển khai production,
/// cấu hình section "MlService" (Endpoint) và thay bằng adapter HTTP gọi ML Service thật.
/// </summary>
public sealed class PlaceholderPredictionService : IPredictionService
{
    /// <inheritdoc />
    public Task<double> PredictSuccessProbabilityAsync(
        PredictionFeatures features, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(features);

        // Tín hiệu trình độ hiện tại (giả định thang điểm [0, 100]) đóng góp vào nền tảng.
        var levelSignal = Math.Clamp(features.CurrentLevelScore / 100d, 0d, 1d);

        // Tín hiệu nỗ lực: nhiều giờ học mỗi ngày làm tăng xác suất, bão hòa dần (hàm lõm,
        // đơn điệu không giảm theo HoursPerDay) để phản ánh hiệu suất giảm dần.
        var hours = Math.Max(0d, features.HoursPerDay);
        var effortSignal = 1d - Math.Exp(-hours / 4d);

        // Kết hợp tuyến tính rồi kẹp về khoảng xác suất hợp lệ [0, 1] (R9.1).
        var probability = (0.4 * levelSignal) + (0.6 * effortSignal);
        return Task.FromResult(Math.Clamp(probability, 0d, 1d));
    }
}
