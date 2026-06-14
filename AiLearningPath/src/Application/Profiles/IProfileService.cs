using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;

namespace AiLearningPath.Application.Profiles;

/// <summary>
/// Dịch vụ quản lý hồ sơ cá nhân và lựa chọn Learning Goal (R3, R4).
/// Mọi thao tác đều gắn với <c>userId</c> của tài khoản đã xác thực để hỗ trợ
/// ownership-based authorization (R14).
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Tạo mới hoặc cập nhật toàn bộ hồ sơ của sinh viên (R3.1). Áp dụng validation
    /// số giờ học và (nếu có) validation Learning Goal trước khi lưu.
    /// </summary>
    /// <exception cref="ProfileValidationException">Khi đầu vào không hợp lệ.</exception>
    Task<Profile> CreateOrUpdateAsync(Guid userId, ProfileInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trả về hồ sơ hiện tại của sinh viên (R3.2).
    /// </summary>
    /// <exception cref="ProfileNotFoundException">Khi sinh viên chưa có hồ sơ.</exception>
    Task<Profile> GetAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật một trường hồ sơ với giá trị mới (R3.3). Áp dụng validation số giờ học
    /// khi cập nhật trường <c>StudyHoursPerDay</c> (R3.4) và validation Learning Goal
    /// khi cập nhật trường <c>LearningGoal</c> (R4.4).
    /// </summary>
    /// <exception cref="ProfileValidationException">Khi giá trị không hợp lệ.</exception>
    /// <exception cref="ProfileNotFoundException">Khi sinh viên chưa có hồ sơ.</exception>
    Task<Profile> UpdateFieldAsync(Guid userId, string field, object? value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lưu lựa chọn Learning Goal cho hồ sơ (R4.2). Áp dụng validation goal (R4.4) và
    /// lưu điểm số mục tiêu khi goal yêu cầu (R4.3).
    /// </summary>
    /// <exception cref="ProfileValidationException">Khi goal không hợp lệ hoặc thiếu điểm mục tiêu bắt buộc.</exception>
    Task<Profile> SelectGoalAsync(Guid userId, LearningGoalSelection selection, CancellationToken cancellationToken = default);
}

/// <summary>
/// Ngoại lệ phát sinh khi đầu vào hồ sơ không hợp lệ. Mang theo <see cref="ValidationResult"/>
/// để lớp API chuyển thành phản hồi lỗi 400 nhất quán.
/// </summary>
public sealed class ProfileValidationException : Exception
{
    public ProfileValidationException(ValidationResult validation)
        : base(validation.FirstError ?? "Dữ liệu hồ sơ không hợp lệ.")
    {
        Validation = validation;
    }

    /// <summary>Chi tiết kết quả kiểm tra hợp lệ bị thất bại.</summary>
    public ValidationResult Validation { get; }
}

/// <summary>
/// Ngoại lệ phát sinh khi không tìm thấy hồ sơ của sinh viên (R3.2). Lớp API chuyển
/// thành phản hồi 404.
/// </summary>
public sealed class ProfileNotFoundException : Exception
{
    public ProfileNotFoundException(Guid userId)
        : base($"Không tìm thấy hồ sơ cho người dùng {userId}.")
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}
