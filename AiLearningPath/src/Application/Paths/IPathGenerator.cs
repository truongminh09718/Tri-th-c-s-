using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;

namespace AiLearningPath.Application.Paths;

/// <summary>
/// Dịch vụ sinh lộ trình học tập cá nhân hóa (R7).
/// Điều phối nghiệp vụ: kiểm tra tiên quyết (R7.4) qua domain logic thuần
/// <see cref="Domain.Paths.PathBuilder.CheckPrerequisites"/>, gọi
/// <see cref="ExternalServices.IContentGenerator"/> (bọc Gemini) sinh nội dung lộ trình
/// (R7.1), xây cấu trúc tháng → tuần → ngày bằng <see cref="Domain.Paths.PathBuilder.Build"/>
/// (R7.1, R7.2), ước lượng tính khả thi bằng <see cref="Domain.Paths.PathBuilder.AssessFeasibility"/>
/// (R7.5), rồi lưu <see cref="LearningPath"/> liên kết với hồ sơ sinh viên (R7.3).
/// Mọi thao tác gắn với <c>userId</c> của tài khoản đã xác thực để hỗ trợ
/// ownership-based authorization (R14).
/// </summary>
public interface IPathGenerator
{
    /// <summary>
    /// Sinh lộ trình học tập cá nhân hóa cho sinh viên trong khoảng thời gian mục tiêu
    /// <paramref name="targetDays"/> ngày (R7.1–R7.5).
    /// </summary>
    /// <param name="userId">Định danh chủ sở hữu (tài khoản đã xác thực).</param>
    /// <param name="targetDays">Số ngày mục tiêu mà lộ trình phải bao phủ (phải >= 1).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Lộ trình đã được tạo và lưu (kèm cờ cảnh báo tính khả thi nếu có).</returns>
    /// <exception cref="PathValidationException">
    /// Khi khoảng thời gian mục tiêu không hợp lệ, hoặc sinh viên chưa hoàn thành AI
    /// Assessment (R7.4).
    /// </exception>
    /// <exception cref="PathGenerationException">
    /// Khi nội dung lộ trình do dịch vụ sinh nội dung AI trả về không hợp lệ.
    /// </exception>
    Task<LearningPath> GenerateAsync(
        Guid userId, int targetDays, CancellationToken cancellationToken = default);
}

/// <summary>
/// Loại nội dung yêu cầu dịch vụ sinh nội dung AI (<see cref="ExternalServices.IContentGenerator"/>)
/// tạo ra cho lộ trình học tập.
/// </summary>
public static class PathContentKind
{
    public const string LearningPath = "LearningPath";
}

/// <summary>Giá trị mặc định dùng khi sinh lộ trình.</summary>
public static class PathGenerationDefaults
{
    /// <summary>
    /// Số giờ học ước lượng hệ thống gán cho nhiệm vụ mỗi ngày của lộ trình. Dùng làm
    /// cơ sở so sánh với số giờ khả dụng của sinh viên khi đánh giá tính khả thi (R7.5).
    /// </summary>
    public const double EstimatedDailyHours = 2.0;
}

/// <summary>
/// Ngoại lệ phát sinh khi đầu vào sinh lộ trình không hợp lệ hoặc chưa đủ điều kiện
/// tiên quyết (R7.4 — chưa hoàn thành AI Assessment). Mang theo <see cref="ValidationResult"/>
/// để lớp API chuyển thành phản hồi lỗi 400 nhất quán.
/// </summary>
public sealed class PathValidationException : Exception
{
    public PathValidationException(ValidationResult validation)
        : base(validation.FirstError ?? "Dữ liệu sinh lộ trình không hợp lệ.")
    {
        Validation = validation;
    }

    /// <summary>Chi tiết kết quả kiểm tra hợp lệ bị thất bại.</summary>
    public ValidationResult Validation { get; }
}

/// <summary>
/// Ngoại lệ phát sinh khi nội dung lộ trình do dịch vụ sinh nội dung AI trả về không
/// hợp lệ (rỗng). Lớp API chuyển thành phản hồi 502.
/// </summary>
public sealed class PathGenerationException : Exception
{
    public PathGenerationException(string message) : base(message)
    {
    }
}
