using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiLearningPath.Application.Auth;
using AiLearningPath.Domain.Auth;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AiLearningPath.Infrastructure.Auth;

/// <summary>
/// Triển khai <see cref="IAuthService"/> trên EF Core + JWT (R1, R2).
///
/// <para>
/// <b>RegisterAsync</b>: kiểm tra chính sách mật khẩu (R1.3), từ chối email trùng (R1.2),
/// băm mật khẩu bằng <see cref="PasswordHasher"/> (R1.4) rồi lưu tài khoản mới (R1.1).
/// </para>
/// <para>
/// <b>LoginAsync</b>: xác minh mật khẩu bằng <see cref="PasswordHasher.Verify"/> và sinh JWT (R2.1);
/// trả về lỗi xác thực thống nhất khi email không tồn tại hoặc sai mật khẩu (R2.2).
/// </para>
/// <para>
/// <b>ValidateToken</b>: xác minh chữ ký, issuer, audience và hạn dùng của token.
/// </para>
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly JwtSettings _jwt;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public AuthService(AppDbContext db, IOptions<JwtSettings> jwtOptions)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        ArgumentNullException.ThrowIfNull(jwtOptions);
        _jwt = jwtOptions.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
    }

    /// <inheritdoc />
    public async Task<RegisterResult> RegisterAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return RegisterResult.Failure("VALIDATION_ERROR", "Email không được để trống.");
        }

        // R1.3: chính sách độ dài mật khẩu.
        var policy = PasswordPolicy.Validate(password);
        if (!policy.IsValid)
        {
            return RegisterResult.Failure(
                "VALIDATION_ERROR",
                policy.FirstError ?? "Mật khẩu không hợp lệ.");
        }

        // R1.2: từ chối email đã tồn tại.
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            return RegisterResult.Failure(
                "EMAIL_ALREADY_USED", "Email này đã được sử dụng.");
        }

        // R1.4 + R1.1: băm mật khẩu và lưu tài khoản mới.
        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = PasswordHasher.Hash(password)
        };

        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Bảo vệ trước điều kiện đua (race) khi hai yêu cầu cùng email xảy ra đồng thời:
            // ràng buộc unique trên Email sẽ ném lỗi và ta quy về cùng thông báo R1.2.
            return RegisterResult.Failure(
                "EMAIL_ALREADY_USED", "Email này đã được sử dụng.");
        }

        return RegisterResult.Success(user.Id);
    }

    /// <inheritdoc />
    public async Task<LoginResult> LoginAsync(
        string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeEmail(email);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // R2.2: không tiết lộ email tồn tại hay không — dùng cùng một thông báo lỗi.
        if (user is null || !PasswordHasher.Verify(password ?? string.Empty, user.PasswordHash))
        {
            return LoginResult.Failure(
                "INVALID_CREDENTIALS", "Email hoặc mật khẩu không đúng.");
        }

        // R2.1: sinh JWT cho tài khoản đã xác thực.
        var (token, expiresAtUtc) = GenerateToken(user);
        return LoginResult.Success(token, expiresAtUtc);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? ValidateToken(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwt.Issuer,
            ValidAudience = _jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            return _tokenHandler.ValidateToken(jwt, parameters, out _);
        }
        catch (Exception)
        {
            // Token không hợp lệ/hết hạn/sai chữ ký → trả null.
            return null;
        }
    }

    private (string Token, DateTime ExpiresAtUtc) GenerateToken(User user)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwt.ExpiryMinutes);

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256);

        // Đặt id người dùng vào cả NameIdentifier và sub để middleware ownership
        // (đọc NameIdentifier/sub/userId) có thể lấy được requesterId.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return (_tokenHandler.WriteToken(token), expires);
    }

    private static string NormalizeEmail(string? email) =>
        (email ?? string.Empty).Trim().ToLowerInvariant();
}
