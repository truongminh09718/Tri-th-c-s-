namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Hồ sơ cá nhân của sinh viên. StudyHoursPerDay ∈ [0, 24]; TargetScore nullable (chỉ với IELTS/TOEIC).
/// </summary>
public class Profile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới chủ sở hữu (ownership-based authorization, R14.1).</summary>
    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public string LearningGoal { get; set; } = string.Empty;

    /// <summary>Điểm số mục tiêu, chỉ áp dụng cho một số Learning Goal (IELTS, TOEIC).</summary>
    public int? TargetScore { get; set; }

    public string CareerGoal { get; set; } = string.Empty;

    /// <summary>Số giờ học mỗi ngày, hợp lệ trong khoảng [0, 24].</summary>
    public double StudyHoursPerDay { get; set; }

    // Quan hệ điều hướng
    public User? User { get; set; }
}
