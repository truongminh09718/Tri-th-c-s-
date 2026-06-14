namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Tài khoản đăng nhập của sinh viên. Email là duy nhất, mật khẩu lưu dưới dạng hash.
/// </summary>
public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Email duy nhất dùng để đăng nhập.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Mật khẩu đã được băm (không lưu plaintext).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Quan hệ điều hướng
    public Profile? Profile { get; set; }
    public LearningDnaProfile? LearningDnaProfile { get; set; }
    public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
    public ICollection<LearningPath> LearningPaths { get; set; } = new List<LearningPath>();
    public ICollection<CareerPath> CareerPaths { get; set; } = new List<CareerPath>();
    public ICollection<StudySession> StudySessions { get; set; } = new List<StudySession>();
    public ICollection<ProgressSnapshot> ProgressSnapshots { get; set; } = new List<ProgressSnapshot>();
}
