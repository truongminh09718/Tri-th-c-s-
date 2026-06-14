using AiLearningPath.Domain.Common;

namespace AiLearningPath.Domain.Auth;

/// <summary>
/// Chính sách mật khẩu thuần (pure function), không phụ thuộc I/O.
/// Quy tắc hiện tại: mật khẩu phải có độ dài tối thiểu <see cref="MinLength"/> ký tự (R1.3).
/// </summary>
public static class PasswordPolicy
{
    /// <summary>Độ dài tối thiểu hợp lệ của mật khẩu.</summary>
    public const int MinLength = 8;

    /// <summary>
    /// Kiểm tra mật khẩu theo chính sách độ dài.
    /// Trả về hợp lệ khi và chỉ khi <paramref name="password"/> không null và có
    /// độ dài >= <see cref="MinLength"/> ký tự.
    /// </summary>
    public static ValidationResult Validate(string? password)
    {
        if (password is null || password.Length < MinLength)
        {
            return ValidationResult.Failure(
                $"Mật khẩu phải có độ dài tối thiểu {MinLength} ký tự.");
        }

        return ValidationResult.Success();
    }
}
