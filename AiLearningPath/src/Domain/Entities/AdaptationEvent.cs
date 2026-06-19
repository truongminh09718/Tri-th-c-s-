namespace AiLearningPath.Domain.Entities;

public class AdaptationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid LearningPathId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string AddedTasksJson { get; set; } = "[]";
    public string FocusSkillsJson { get; set; } = "[]";
    public bool UsedFallback { get; set; }
    public bool FromCache { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
    public LearningPath? LearningPath { get; set; }
}
