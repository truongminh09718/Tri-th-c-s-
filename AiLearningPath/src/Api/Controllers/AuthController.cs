using AiLearningPath.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiLearningPath.Api.Controllers;

/// <summary>
/// Endpoint xác thực: đăng ký tài khoản và đăng nhập (R1, R2).
/// Các endpoint này công khai (<see cref="AllowAnonymousAttribute"/>) vì người dùng
/// chưa có token khi gọi tới.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Đăng ký tài khoản mới (R1). Trả về 201 kèm id tài khoản khi thành công;
    /// 400 với body lỗi nhất quán khi mật khẩu không hợp lệ hoặc email đã được dùng.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(
            request.Email, request.Password, cancellationToken);

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return StatusCode(
            StatusCodes.Status201Created,
            new { userId = result.UserId });
    }

    /// <summary>
    /// Đăng nhập (R2). Trả về 200 kèm JWT khi thông tin đúng (R2.1);
    /// 401 với body lỗi xác thực khi email/mật khẩu không đúng (R2.2).
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            request.Email, request.Password, cancellationToken);

        if (!result.Succeeded)
        {
            return Unauthorized(new { error = result.Error });
        }

        return Ok(new
        {
            token = result.Token,
            expiresAtUtc = result.ExpiresAtUtc
        });
    }
}

/// <summary>Payload yêu cầu đăng ký.</summary>
public sealed record RegisterRequest(string Email, string Password);

/// <summary>Payload yêu cầu đăng nhập.</summary>
public sealed record LoginRequest(string Email, string Password);
