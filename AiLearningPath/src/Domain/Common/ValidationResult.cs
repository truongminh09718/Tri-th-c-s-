namespace AiLearningPath.Domain.Common;

/// <summary>
/// Kết quả của một phép kiểm tra hợp lệ (validation) thuần.
/// Bao gồm cờ <see cref="IsValid"/> và danh sách thông báo lỗi khi không hợp lệ.
/// Đây là kiểu dùng chung cho toàn bộ logic nghiệp vụ thuần (PasswordPolicy,
/// ProfileValidator, AssessmentGrader, PathBuilder, TwinValidator...).
/// </summary>
public sealed class ValidationResult
{
    private static readonly IReadOnlyList<string> NoErrors = Array.Empty<string>();

    private ValidationResult(bool isValid, IReadOnlyList<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    /// <summary>True nếu đầu vào hợp lệ; ngược lại False.</summary>
    public bool IsValid { get; }

    /// <summary>Danh sách thông báo lỗi. Rỗng khi <see cref="IsValid"/> là true.</summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Thông báo lỗi đầu tiên (nếu có), tiện cho việc hiển thị nhanh.</summary>
    public string? FirstError => Errors.Count > 0 ? Errors[0] : null;

    /// <summary>Tạo kết quả hợp lệ (không có lỗi).</summary>
    public static ValidationResult Success() => new(true, NoErrors);

    /// <summary>Tạo kết quả không hợp lệ với một thông báo lỗi.</summary>
    public static ValidationResult Failure(string error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ValidationResult(false, new[] { error });
    }

    /// <summary>Tạo kết quả không hợp lệ với nhiều thông báo lỗi.</summary>
    public static ValidationResult Failure(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var list = errors.ToArray();
        if (list.Length == 0)
        {
            throw new ArgumentException("Phải cung cấp ít nhất một thông báo lỗi.", nameof(errors));
        }

        return new ValidationResult(false, list);
    }
}
