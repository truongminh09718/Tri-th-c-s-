namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Hồ sơ học tập cá nhân: phong cách học, khung giờ hiệu quả (JSON), tốc độ,
/// khả năng tập trung và điểm mạnh/yếu (JSON).
/// </summary>
public class LearningDnaProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới chủ sở hữu (ownership-based authorization, R14.1).</summary>
    public Guid UserId { get; set; }

    public string LearningStyle { get; set; } = string.Empty;

    /// <summary>Khung giờ học hiệu quả serialize dạng JSON.</summary>
    public string EffectiveHoursJson { get; set; } = string.Empty;

    public double LearningSpeed { get; set; }
    public double FocusAbility { get; set; }

    /// <summary>Danh sách điểm mạnh serialize dạng JSON.</summary>
    public string StrengthsJson { get; set; } = string.Empty;

    /// <summary>Danh sách điểm yếu serialize dạng JSON.</summary>
    public string WeaknessesJson { get; set; } = string.Empty;

    // Quan hệ điều hướng
    public User? User { get; set; }
}
