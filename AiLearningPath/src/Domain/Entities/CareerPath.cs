namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Lộ trình nghề nghiệp: kỹ năng, chứng chỉ và dự án gợi ý (lưu JSON).
/// </summary>
public class CareerPath
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới chủ sở hữu (ownership-based authorization, R14.1).</summary>
    public Guid UserId { get; set; }

    public string Career { get; set; } = string.Empty;

    /// <summary>Danh sách kỹ năng serialize dạng JSON.</summary>
    public string SkillsJson { get; set; } = string.Empty;

    /// <summary>Danh sách chứng chỉ serialize dạng JSON.</summary>
    public string CertificationsJson { get; set; } = string.Empty;

    /// <summary>Danh sách dự án gợi ý serialize dạng JSON.</summary>
    public string ProjectsJson { get; set; } = string.Empty;

    // Quan hệ điều hướng
    public User? User { get; set; }
}
