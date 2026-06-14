namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Giai đoạn của lộ trình, ánh xạ theo tháng/tuần.
/// Cấp giữa của phân cấp LearningPath → PathPhase → LearningTask.
/// </summary>
public class PathPhase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới lộ trình cha.</summary>
    public Guid LearningPathId { get; set; }

    public int MonthIndex { get; set; }
    public int WeekIndex { get; set; }
    public string Title { get; set; } = string.Empty;

    // Quan hệ điều hướng
    public LearningPath? LearningPath { get; set; }
    public ICollection<LearningTask> Tasks { get; set; } = new List<LearningTask>();
}
