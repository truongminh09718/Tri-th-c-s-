namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Ảnh chụp tiến độ tại một thời điểm: Learning Score và tỷ lệ hoàn thành.
/// </summary>
public class ProgressSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới chủ sở hữu (ownership-based authorization, R14.1).</summary>
    public Guid UserId { get; set; }

    public DateTime RecordedAt { get; set; }
    public double LearningScore { get; set; }
    public double CompletionRate { get; set; }

    // Quan hệ điều hướng
    public User? User { get; set; }
}
