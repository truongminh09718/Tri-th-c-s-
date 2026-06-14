using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AiLearningPath.Application.Auth;
using AiLearningPath.Infrastructure.Auth;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AiLearningPath.Tests.Auth;

/// <summary>
/// Unit test cho luồng đăng ký/đăng nhập của <see cref="AuthService"/> (R1.1, R2.1, R2.2).
///
/// <list type="bullet">
///   <item>Tạo tài khoản thành công và lưu vào DB (R1.1).</item>
///   <item>Đăng nhập đúng thông tin trả về JWT hợp lệ (R2.1).</item>
///   <item>Đăng nhập sai thông tin (sai mật khẩu hoặc email không tồn tại) bị từ chối (R2.2).</item>
/// </list>
///
/// Dùng EF Core InMemory provider để không phụ thuộc vào SQL Server thật.
/// JWT được cấu hình qua <see cref="IOptions{TOptions}"/> với <see cref="JwtSettings"/>.
/// </summary>
public sealed class AuthServiceFlowTests
{
    private const string JwtKey = "test-signing-key-please-use-at-least-32-bytes-long!!";
    private const string JwtIssuer = "ai-learning-path-tests";
    private const string JwtAudience = "ai-learning-path-clients";

    /// <summary>Tạo một DbContext InMemory mới với database tên duy nhất cho từng test.</summary>
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static JwtSettings CreateJwtSettings() => new()
    {
        Key = JwtKey,
        Issuer = JwtIssuer,
        Audience = JwtAudience,
        ExpiryMinutes = 60
    };

    private static AuthService CreateService(AppDbContext db) =>
        new(db, Options.Create(CreateJwtSettings()));

    // R1.1: đăng ký với email và mật khẩu hợp lệ tạo tài khoản mới và lưu vào DB.
    [Fact]
    public async Task RegisterAsync_WithValidCredentials_CreatesAccountAndPersists()
    {
        using var db = CreateDbContext();
        var sut = CreateService(db);

        var result = await sut.RegisterAsync("Student@Example.com", "StrongPass123");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.UserId);
        Assert.Null(result.Error);

        // Tài khoản phải được lưu vào DB với email chuẩn hóa (lowercase).
        var saved = await db.Users.SingleAsync();
        Assert.Equal(result.UserId, saved.Id);
        Assert.Equal("student@example.com", saved.Email);

        // R1.4: mật khẩu lưu dưới dạng băm, không phải plaintext.
        Assert.NotEqual("StrongPass123", saved.PasswordHash);
        Assert.False(string.IsNullOrWhiteSpace(saved.PasswordHash));
    }

    // R2.1: đăng nhập với email/mật khẩu đúng trả về một JWT hợp lệ.
    [Fact]
    public async Task LoginAsync_WithCorrectCredentials_ReturnsValidJwt()
    {
        using var db = CreateDbContext();
        var sut = CreateService(db);

        const string email = "login@example.com";
        const string password = "CorrectPass123";
        var register = await sut.RegisterAsync(email, password);
        Assert.True(register.Succeeded);

        var login = await sut.LoginAsync(email, password);

        Assert.True(login.Succeeded);
        Assert.Null(login.Error);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.NotNull(login.ExpiresAtUtc);

        // Token phải xác minh được bằng đúng cấu hình đã ký.
        var principal = sut.ValidateToken(login.Token!);
        Assert.NotNull(principal);

        // Token chứa định danh của tài khoản vừa đăng nhập.
        var subject = principal!.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        Assert.Equal(register.UserId!.Value.ToString(), subject);

        // Xác minh độc lập rằng token được ký đúng key/issuer/audience.
        AssertTokenIsSignedWithExpectedParameters(login.Token!);
    }

    // R2.2: sai mật khẩu bị từ chối với lỗi xác thực, không trả token.
    [Fact]
    public async Task LoginAsync_WithWrongPassword_IsRejected()
    {
        using var db = CreateDbContext();
        var sut = CreateService(db);

        const string email = "wrongpass@example.com";
        await sut.RegisterAsync(email, "CorrectPass123");

        var login = await sut.LoginAsync(email, "WrongPass999");

        Assert.False(login.Succeeded);
        Assert.Null(login.Token);
        Assert.Null(login.ExpiresAtUtc);
        Assert.NotNull(login.Error);
        Assert.Equal("INVALID_CREDENTIALS", login.Error!.Code);
    }

    // R2.2: email không tồn tại bị từ chối với cùng lỗi xác thực (không tiết lộ email tồn tại hay không).
    [Fact]
    public async Task LoginAsync_WithUnknownEmail_IsRejected()
    {
        using var db = CreateDbContext();
        var sut = CreateService(db);

        var login = await sut.LoginAsync("does-not-exist@example.com", "AnyPassword123");

        Assert.False(login.Succeeded);
        Assert.Null(login.Token);
        Assert.NotNull(login.Error);
        Assert.Equal("INVALID_CREDENTIALS", login.Error!.Code);
    }

    /// <summary>
    /// Xác minh chữ ký/issuer/audience của token một cách độc lập với AuthService,
    /// đảm bảo JWT trả về thực sự hợp lệ theo cấu hình (R2.1).
    /// </summary>
    private static void AssertTokenIsSignedWithExpectedParameters(string token)
    {
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = JwtIssuer,
            ValidAudience = JwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
            ClockSkew = TimeSpan.Zero
        };

        var principal = new JwtSecurityTokenHandler()
            .ValidateToken(token, parameters, out var validatedToken);

        Assert.NotNull(principal);
        Assert.IsType<JwtSecurityToken>(validatedToken);
    }
}
