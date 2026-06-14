using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AiLearningPath.Tests.Auth;

/// <summary>
/// Integration test cho pipeline xác thực/phân quyền thực (R2.4, R14.2, R14.3).
///
/// Dùng <see cref="WebApplicationFactory{TEntryPoint}"/> để dựng host thật từ
/// <c>Program</c>, đi qua middleware JWT + <c>OwnershipAuthorizationMiddleware</c>:
/// <list type="bullet">
///   <item>JWT thiếu/không hợp lệ/hết hạn → <c>401 Unauthorized</c> (R2.4, R14.3).</item>
///   <item>Người dùng đã xác thực truy cập tài nguyên của tài khoản khác → <c>403 Forbidden</c> (R14.2).</item>
/// </list>
///
/// Các tình huống 401/403 được middleware chặn trước khi chạm tới controller/DB,
/// nên không phụ thuộc vào SQL Server.
///
/// Token trong test được ký bằng đúng cấu hình "Jwt" (Key/Issuer/Audience) mà host
/// dùng để xác minh — đọc trực tiếp từ <see cref="IConfiguration"/> của host đang chạy.
/// </summary>
public sealed class JwtMiddlewareIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IConfiguration _configuration;

    public JwtMiddlewareIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        // Đọc cấu hình thực mà host dùng để xác minh JWT, đảm bảo token test
        // được ký bằng đúng Key/Issuer/Audience.
        _configuration = _factory.Services.GetRequiredService<IConfiguration>();
    }

    // R2.4 / R14.3: không kèm token → 401.
    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/users/{userId}/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // R2.4 / R14.3: token rác/không hợp lệ → 401.
    [Fact]
    public async Task ProtectedEndpoint_WithGarbageToken_Returns401()
    {
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}/profile");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", "this.is.not.a.valid.jwt");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // R14.3: token hết hạn → 401.
    [Fact]
    public async Task ProtectedEndpoint_WithExpiredToken_Returns401()
    {
        var client = _factory.CreateClient();
        var userId = Guid.NewGuid();

        // Token đã hết hạn (expires nằm trong quá khứ).
        var expiredToken = CreateToken(
            userId,
            notBefore: DateTime.UtcNow.AddMinutes(-10),
            expires: DateTime.UtcNow.AddMinutes(-5));

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userId}/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // R14.2: user A đã xác thực truy cập tài nguyên của user B → 403.
    [Fact]
    public async Task ProtectedEndpoint_AccessingOtherUsersResource_Returns403()
    {
        var client = _factory.CreateClient();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        // Token hợp lệ cho user A, nhưng route trỏ tới hồ sơ của user B.
        var validTokenForA = CreateToken(
            userA,
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(30));

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/users/{userB}/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", validTokenForA);

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Mint JWT ký bằng cùng cấu hình "Jwt" (Key/Issuer/Audience) mà host dùng,
    /// đặt userId vào cả NameIdentifier và sub để middleware ownership đọc được.
    /// </summary>
    private string CreateToken(Guid userId, DateTime notBefore, DateTime expires)
    {
        var jwt = _configuration.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key không được cấu hình.");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, $"{userId}@test.local"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
