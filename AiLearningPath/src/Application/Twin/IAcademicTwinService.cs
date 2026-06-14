using AiLearningPath.Domain.Common;

namespace AiLearningPath.Application.Twin;

/// <summary>
/// Dịch vụ mô phỏng và dự đoán xác suất đạt mục tiêu theo thời lượng học (R9).
/// Điều phối nghiệp vụ: kiểm tra tiên quyết (R9.4), gọi dịch vụ dự đoán ML
/// (<see cref="ExternalServices.IPredictionService"/>) và đảm bảo các thuộc tính đúng đắn
/// của kết quả (xác suất hợp lệ trong [0, 1]; đơn điệu không giảm theo thời lượng).
/// Mọi thao tác đều gắn với <c>userId</c> của tài khoản đã xác thực để hỗ trợ
/// ownership-based authorization (R14).
/// </summary>
public interface IAcademicTwinService
{
    /// <summary>
    /// Mô phỏng kết quả với một thời lượng học mỗi ngày (R9.1): kiểm tra tiên quyết,
    /// gọi dịch vụ dự đoán dựa trên dữ liệu mới nhất của sinh viên (R9.3) và trả về
    /// xác suất đạt mục tiêu ước lượng trong khoảng [0, 1].
    /// </summary>
    /// <param name="userId">Định danh chủ sở hữu (tài khoản đã xác thực).</param>
    /// <param name="hoursPerDay">Số giờ học mỗi ngày dùng để mô phỏng.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Dự đoán xác suất đạt mục tiêu tương ứng với thời lượng đã cho.</returns>
    /// <exception cref="TwinPrerequisitesException">
    /// Khi sinh viên chưa có Learning Goal hoặc chưa hoàn thành AI Assessment (R9.4).
    /// </exception>
    Task<Prediction> SimulateAsync(
        Guid userId, double hoursPerDay, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mô phỏng kết quả với nhiều mức thời lượng học mỗi ngày (R9.2): trả về đúng một
    /// dự đoán cho mỗi mức (số lượng kết quả bằng số lượng đầu vào) và đảm bảo xác suất
    /// đạt mục tiêu không giảm khi thời lượng học tăng (đơn điệu không giảm).
    /// </summary>
    /// <param name="userId">Định danh chủ sở hữu (tài khoản đã xác thực).</param>
    /// <param name="hoursOptions">Danh sách các mức thời lượng học mỗi ngày cần mô phỏng.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>
    /// Danh sách dự đoán, một phần tử cho mỗi mức thời lượng đầu vào (giữ nguyên thứ tự đầu vào).
    /// </returns>
    /// <exception cref="TwinPrerequisitesException">
    /// Khi sinh viên chưa có Learning Goal hoặc chưa hoàn thành AI Assessment (R9.4).
    /// </exception>
    Task<IReadOnlyList<Prediction>> SimulateRangeAsync(
        Guid userId, IReadOnlyList<double> hoursOptions, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kết quả mô phỏng Academic Twin cho một mức thời lượng học mỗi ngày.
/// </summary>
/// <param name="HoursPerDay">Số giờ học mỗi ngày được mô phỏng.</param>
/// <param name="SuccessProbability">
/// Xác suất đạt mục tiêu ước lượng, luôn nằm trong khoảng [0, 1] (R9.1, R9.2).
/// </param>
public sealed record Prediction(double HoursPerDay, double SuccessProbability);

/// <summary>
/// Ngoại lệ phát sinh khi sinh viên chưa đủ điều kiện tiên quyết để mô phỏng Academic Twin
/// (chưa có Learning Goal hoặc chưa hoàn thành AI Assessment — R9.4). Mang theo
/// <see cref="ValidationResult"/> để lớp API chuyển thành phản hồi lỗi 400 nhất quán.
/// </summary>
public sealed class TwinPrerequisitesException : Exception
{
    public TwinPrerequisitesException(ValidationResult validation)
        : base(validation.FirstError ?? "Chưa đủ điều kiện tiên quyết để mô phỏng Academic Twin.")
    {
        Validation = validation;
    }

    /// <summary>Chi tiết kết quả kiểm tra tiên quyết bị thất bại.</summary>
    public ValidationResult Validation { get; }
}
