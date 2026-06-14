namespace AiLearningPath.Application.Auth;

/// <summary>
/// Cấu hình JWT đọc từ <c>appsettings.json</c> mục <c>"Jwt"</c>.
/// Dùng để sinh và xác minh token trong <see cref="IAuthService"/>.
/// </summary>
public sealed class JwtSettings
{
    /// <summary>Tên section trong cấu hình.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Khóa bí mật đối xứng dùng để ký token (HMAC-SHA256).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Issuer hợp lệ của token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Audience hợp lệ của token.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Thời gian sống của token tính bằng phút.</summary>
    public int ExpiryMinutes { get; set; } = 60;
}
