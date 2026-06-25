namespace AiLearningPath.Api.Models;

public sealed class AiPredictionRequest
{
    public double StudyHoursPerDay { get; set; }
    public double AttendanceRate { get; set; }
    public double MockTestScore { get; set; }
    public double HistoricalGpa { get; set; }

    /// <summary>
    /// Mục tiêu học tập (liên kết với hồ sơ người dùng), ví dụ "TOEIC", "IELTS", "GPA".
    /// Quyết định loại điểm được mô phỏng. Rỗng => mặc định TOEIC.
    /// </summary>
    public string? GoalType { get; set; }

    /// <summary>
    /// Điểm số mục tiêu lấy từ hồ sơ (ví dụ 700 cho TOEIC, 6.5 cho IELTS, 3.2 cho GPA).
    /// Rỗng => dùng mặc định theo loại mục tiêu.
    /// </summary>
    public double? TargetScore { get; set; }
}
