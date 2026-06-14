using System.Security.Claims;

namespace AiLearningPath.Application.Auth;

/// <summary>
/// Dịch vụ xác thực: đăng ký tài khoản, đăng nhập và sinh/xác minh JWT (R1, R2).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Đăng ký tài khoản mới (R1):
    /// kiểm tra chính sách mật khẩu, từ chối email trùng (R1.2), băm mật khẩu (R1.4)
    /// và lưu tài khoản vào cơ sở dữ liệu (R1.1).
    /// </summary>
    Task<RegisterResult> RegisterAsync(
        string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đăng nhập (R2): xác minh email/mật khẩu và sinh JWT khi hợp lệ (R2.1),
    /// từ chối với thông báo lỗi xác thực khi sai thông tin (R2.2).
    /// </summary>
    Task<LoginResult> LoginAsync(
        string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác minh một JWT. Trả về <see cref="ClaimsPrincipal"/> khi token hợp lệ;
    /// trả về <c>null</c> khi token không hợp lệ hoặc đã hết hạn.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string jwt);
}
