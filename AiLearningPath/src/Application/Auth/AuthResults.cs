using AiLearningPath.Domain.Common;

namespace AiLearningPath.Application.Auth;

/// <summary>
/// Kết quả của thao tác đăng ký tài khoản (R1).
/// Khi thành công, <see cref="UserId"/> chứa định danh tài khoản mới được tạo;
/// khi thất bại, <see cref="Error"/> mô tả nguyên nhân (mật khẩu không hợp lệ, email trùng).
/// </summary>
public sealed class RegisterResult
{
    private RegisterResult(bool succeeded, Guid? userId, ApiError? error)
    {
        Succeeded = succeeded;
        UserId = userId;
        Error = error;
    }

    /// <summary>True nếu đăng ký thành công.</summary>
    public bool Succeeded { get; }

    /// <summary>Định danh tài khoản mới (khi thành công).</summary>
    public Guid? UserId { get; }

    /// <summary>Thông tin lỗi (khi thất bại).</summary>
    public ApiError? Error { get; }

    /// <summary>Tạo kết quả đăng ký thành công.</summary>
    public static RegisterResult Success(Guid userId) => new(true, userId, null);

    /// <summary>Tạo kết quả đăng ký thất bại kèm mã lỗi và thông báo.</summary>
    public static RegisterResult Failure(string code, string message) =>
        new(false, null, new ApiError(code, message));
}

/// <summary>
/// Kết quả của thao tác đăng nhập (R2).
/// Khi thành công, <see cref="Token"/> chứa JWT đã ký kèm thời điểm hết hạn;
/// khi thất bại, <see cref="Error"/> mô tả lỗi xác thực.
/// </summary>
public sealed class LoginResult
{
    private LoginResult(bool succeeded, string? token, DateTime? expiresAtUtc, ApiError? error)
    {
        Succeeded = succeeded;
        Token = token;
        ExpiresAtUtc = expiresAtUtc;
        Error = error;
    }

    /// <summary>True nếu đăng nhập thành công.</summary>
    public bool Succeeded { get; }

    /// <summary>JWT đã ký (khi thành công).</summary>
    public string? Token { get; }

    /// <summary>Thời điểm hết hạn của token theo UTC (khi thành công).</summary>
    public DateTime? ExpiresAtUtc { get; }

    /// <summary>Thông tin lỗi (khi thất bại).</summary>
    public ApiError? Error { get; }

    /// <summary>Tạo kết quả đăng nhập thành công.</summary>
    public static LoginResult Success(string token, DateTime expiresAtUtc) =>
        new(true, token, expiresAtUtc, null);

    /// <summary>Tạo kết quả đăng nhập thất bại kèm mã lỗi và thông báo.</summary>
    public static LoginResult Failure(string code, string message) =>
        new(false, null, null, new ApiError(code, message));
}
