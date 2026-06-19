namespace AiLearningPath.Domain.Entities;

public class StudySchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool UsedFallback { get; set; }
    public bool FromCache { get; set; }
    public string Provider { get; set; } = string.Empty;

    public User? User { get; set; }
    public ICollection<StudyScheduleItem> Items { get; set; } = new List<StudyScheduleItem>();
}

public class StudyScheduleItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudyScheduleId { get; set; }
    public Guid? LearningTaskId { get; set; }
    public DateTime ScheduledFor { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Skill { get; set; } = string.Empty;
    public double EstimatedHours { get; set; }

    public StudySchedule? Schedule { get; set; }
}
