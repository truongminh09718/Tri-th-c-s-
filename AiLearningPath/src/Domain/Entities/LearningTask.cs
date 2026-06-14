namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Nhiệm vụ học tập cụ thể trong một giai đoạn.
/// Cấp lá của phân cấp LearningPath → PathPhase → LearningTask.
/// </summary>
public class LearningTask
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới giai đoạn cha.</summary>
    public Guid PathPhaseId { get; set; }

    public string Skill { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Completed { get; set; }
    public double EstimatedHours { get; set; }

    // Quan hệ điều hướng
    public PathPhase? PathPhase { get; set; }
}
