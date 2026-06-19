namespace AiLearningPath.Domain.Entities;

/// <summary>
/// Kết quả chấm bài đánh giá: trình độ, điểm mạnh/yếu (JSON) và điểm số.
/// </summary>
public class AssessmentResult
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Khóa ngoại tới bài đánh giá tương ứng.</summary>
    public Guid AssessmentId { get; set; }

    /// <summary>Khóa ngoại tới chủ sở hữu (ownership-based authorization, R14.1).</summary>
    public Guid UserId { get; set; }

    public string Level { get; set; } = string.Empty;

    /// <summary>Danh sách điểm mạnh serialize dạng JSON.</summary>
    public string StrengthsJson { get; set; } = string.Empty;

    /// <summary>Danh sách điểm yếu serialize dạng JSON.</summary>
    public string WeaknessesJson { get; set; } = string.Empty;

    /// <summary>
    /// Chi tiết điểm theo từng lĩnh vực kỹ năng (số câu đúng/tổng, độ chính xác) serialize
    /// dạng JSON. Bảo đảm phần phân tích điểm mạnh/yếu luôn có dữ liệu để hiển thị, kể cả
    /// khi không có lĩnh vực nào vượt ngưỡng mạnh hay dưới ngưỡng yếu.
    /// </summary>
    public string SkillBreakdownJson { get; set; } = string.Empty;

    public double Score { get; set; }

    // Quan hệ điều hướng
    public Assessment? Assessment { get; set; }
    public User? User { get; set; }
}
