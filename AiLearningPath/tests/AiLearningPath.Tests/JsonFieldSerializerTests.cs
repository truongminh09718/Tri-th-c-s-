using System.Collections.Generic;
using AiLearningPath.Domain.Common;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests;

/// <summary>
/// Property-based + example tests cho <see cref="JsonFieldSerializer"/>.
///
/// Property 20 (round-trip serialization): với mọi giá trị thuộc các kiểu được lưu dưới
/// dạng JSON (danh sách chuỗi, danh sách số, ánh xạ khung giờ hiệu quả...),
/// <c>Deserialize(Serialize(value))</c> phải tạo ra một đối tượng tương đương với
/// <c>value</c> ban đầu.
///
/// Vì serializer mang tính xác định (deterministic), tính tương đương được kiểm chứng
/// bằng cách so sánh chuỗi JSON tái-serialize: nếu round-trip bảo toàn giá trị thì
/// <c>Serialize(Deserialize(Serialize(value)))</c> phải bằng <c>Serialize(value)</c>.
/// Cách so sánh này đúng đắn cho mọi kiểu (kể cả null và collection lồng nhau) mà không
/// phụ thuộc vào cách so sánh tham chiếu của collection.
/// </summary>
public class JsonFieldSerializerTests
{
    /// <summary>
    /// Kiểm tra một giá trị bất kỳ round-trip được: serialize -> deserialize -> serialize
    /// phải cho cùng một chuỗi JSON với lần serialize đầu tiên.
    /// </summary>
    private static bool RoundTrips<T>(T value)
    {
        var json = JsonFieldSerializer.Serialize(value);
        var restored = JsonFieldSerializer.Deserialize<T>(json);
        var reserialized = JsonFieldSerializer.Serialize(restored);
        return json == reserialized;
    }

    // Feature: ai-learning-path, Property 20: Round-trip serialization dữ liệu JSON
    // Trường danh sách chuỗi: StrengthsJson, WeaknessesJson, SkillsJson, CertificationsJson, ProjectsJson...
    [Property(MaxTest = 100)]
    public bool RoundTrip_Preserves_StringList(List<string> value) => RoundTrips(value);

    // Feature: ai-learning-path, Property 20: Round-trip serialization dữ liệu JSON
    // Trường danh sách số (ví dụ điểm/chỉ số trong QuestionsJson).
    [Property(MaxTest = 100)]
    public bool RoundTrip_Preserves_IntList(List<int> value) => RoundTrips(value);

    // Feature: ai-learning-path, Property 20: Round-trip serialization dữ liệu JSON
    // Trường ánh xạ (ví dụ EffectiveHoursJson: giờ trong ngày -> hiệu quả).
    [Property(MaxTest = 100)]
    public bool RoundTrip_Preserves_Dictionary(Dictionary<int, double> value) => RoundTrips(value);

    [Fact]
    public void RoundTrip_StringList_Example()
    {
        var original = new List<string> { "Reading", "Listening", "Grammar" };
        var json = JsonFieldSerializer.Serialize(original);
        var restored = JsonFieldSerializer.Deserialize<List<string>>(json);

        Assert.NotNull(restored);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void RoundTrip_EmptyList_Example()
    {
        var original = new List<string>();
        var json = JsonFieldSerializer.Serialize(original);
        var restored = JsonFieldSerializer.Deserialize<List<string>>(json);

        Assert.NotNull(restored);
        Assert.Empty(restored!);
    }

    [Fact]
    public void Deserialize_NullOrEmpty_ReturnsDefault()
    {
        Assert.Null(JsonFieldSerializer.Deserialize<List<string>>(null));
        Assert.Null(JsonFieldSerializer.Deserialize<List<string>>(string.Empty));
    }
}
