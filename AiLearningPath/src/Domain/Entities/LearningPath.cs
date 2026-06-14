namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Lộ trình học tập cá nhân hóa chia theo tháng → tuần → ngày.
/// Cấp gốc của phân cấp LearningPath → PathPhase → LearningTask.
/// </summary>
public class LearningPath
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới chủ sở hữu (ownership-based authorization, R14.1).</summary>
    public Guid UserId { get; set; }

    public string LearningGoal { get; set; } = string.Empty;
    public int TargetDays { get; set; }

    /// <summary>Cảnh báo về tính khả thi của khoảng thời gian mục tiêu (R7.5).</summary>
    public bool FeasibilityWarning { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Quan hệ điều hướng
    public User? User { get; set; }
    public ICollection<PathPhase> Phases { get; set; } = new List<PathPhase>();
}
