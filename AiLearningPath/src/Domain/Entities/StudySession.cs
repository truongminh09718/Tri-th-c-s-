namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Một phiên học của sinh viên, dùng để tổng hợp tổng số giờ học.
/// </summary>
public class StudySession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới chủ sở hữu (ownership-based authorization, R14.1).</summary>
    public Guid UserId { get; set; }

    public DateTime StartedAt { get; set; }
    public double DurationHours { get; set; }

    // Quan hệ điều hướng
    public User? User { get; set; }
}
