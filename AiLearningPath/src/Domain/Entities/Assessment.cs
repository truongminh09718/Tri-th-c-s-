namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Bài đánh giá năng lực đầu vào cho một Learning Goal.
/// QuestionsJson lưu bộ câu hỏi dưới dạng JSON (nvarchar(max)).
/// </summary>
public class Assessment
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới chủ sở hữu (ownership-based authorization, R14.1).</summary>
    public Guid UserId { get; set; }

    public string LearningGoal { get; set; } = string.Empty;

    /// <summary>Bộ câu hỏi serialize dạng JSON.</summary>
    public string QuestionsJson { get; set; } = string.Empty;

    /// <summary>Trạng thái bài đánh giá (ví dụ: InProgress, Completed).</summary>
    public string Status { get; set; } = string.Empty;

    // Quan hệ điều hướng
    public User? User { get; set; }
    public AssessmentResult? Result { get; set; }
}
