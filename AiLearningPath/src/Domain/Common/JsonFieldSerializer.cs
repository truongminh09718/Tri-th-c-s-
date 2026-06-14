using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiLearningPath.Domain.Common;

/// <summary>
/// Helper thuần (pure) để serialize/deserialize các trường danh sách/đối tượng phức tạp
/// của entity (QuestionsJson, StrengthsJson, WeaknessesJson, EffectiveHoursJson,
/// SkillsJson, CertificationsJson, ProjectsJson...) sang chuỗi JSON lưu trong cột
/// <c>nvarchar(max)</c> của SQL Server.
///
/// Thiết kế tách biệt khỏi I/O và mang tính xác định (deterministic): cùng một đầu vào
/// luôn cho cùng một chuỗi JSON, và <c>Deserialize(Serialize(x))</c> luôn tương đương với
/// <c>x</c> (round-trip — kiểm chứng bởi Property 20, Requirements 5.3, 6.2, 7.3, 10.4).
/// </summary>
public static class JsonFieldSerializer
{
    /// <summary>
    /// Tùy chọn serialize cố định để đảm bảo tính xác định và round-trip ổn định.
    /// Không indent (tiết kiệm dung lượng cột), không bỏ qua giá trị mặc định để
    /// đảm bảo deserialize tái tạo đúng đối tượng ban đầu.
    /// </summary>
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = false,
        // Cho phép ghi/đọc các giá trị số thực không hữu hạn (Infinity, -Infinity, NaN)
        // dưới dạng literal có tên ("Infinity", "-Infinity", "NaN") để bảo toàn round-trip
        // cho các trường ánh xạ số thực (ví dụ EffectiveHoursJson). Nếu không bật, các giá
        // trị này sẽ ném ArgumentException khi serialize và phá vỡ Property 20.
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
    };

    /// <summary>
    /// Serialize một giá trị (danh sách hoặc đối tượng phức tạp) thành chuỗi JSON
    /// để lưu vào cột <c>nvarchar(max)</c>.
    /// </summary>
    /// <typeparam name="T">Kiểu của giá trị cần serialize.</typeparam>
    /// <param name="value">Giá trị cần serialize. Có thể là null.</param>
    /// <returns>Chuỗi JSON tương ứng (ví dụ <c>"null"</c> khi <paramref name="value"/> là null).</returns>
    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, Options);

    /// <summary>
    /// Deserialize chuỗi JSON đã lưu trong cột <c>nvarchar(max)</c> trở lại giá trị
    /// kiểu <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Kiểu mong muốn của giá trị.</typeparam>
    /// <param name="json">Chuỗi JSON cần deserialize.</param>
    /// <returns>
    /// Giá trị được khôi phục. Trả về <c>default</c> khi <paramref name="json"/> là null/rỗng
    /// để tránh ngoại lệ với cột chưa được khởi tạo.
    /// </returns>
    public static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, Options);
    }
}
