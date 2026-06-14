using System.Security.Cryptography;

namespace AiLearningPath.Domain.Auth;

/// <summary>
/// Băm và xác minh mật khẩu dùng PBKDF2 (HMAC-SHA256) với salt ngẫu nhiên cho mỗi mật khẩu.
/// Salt được lưu kèm hash để có thể xác minh lại (R1.4).
///
/// Định dạng chuỗi kết quả: "{iterations}.{base64(salt)}.{base64(hash)}".
/// Hàm <see cref="Hash"/> không thuần (dùng salt ngẫu nhiên) nhưng <see cref="Verify"/>
/// luôn xác minh được mật khẩu gốc, đảm bảo round-trip.
/// </summary>
public static class PasswordHasher
{
    private const int SaltSize = 16;        // 128-bit salt
    private const int KeySize = 32;         // 256-bit derived key
    private const int Iterations = 100_000; // số vòng lặp PBKDF2
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char Delimiter = '.';

    /// <summary>
    /// Băm mật khẩu với một salt ngẫu nhiên mới và trả về chuỗi mã hóa
    /// chứa iterations, salt và hash để có thể xác minh sau này.
    /// </summary>
    public static string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join(
            Delimiter,
            Iterations,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    /// <summary>
    /// Xác minh mật khẩu thuần với chuỗi hash đã lưu.
    /// Trả về true khi và chỉ khi mật khẩu khớp; an toàn với chuỗi hash sai định dạng.
    /// So sánh dùng thuật toán chống side-channel (fixed-time).
    /// </summary>
    public static bool Verify(string password, string hash)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(hash);

        var parts = hash.Split(Delimiter);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out int iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;
        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (expectedHash.Length == 0)
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, Algorithm, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
