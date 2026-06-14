using AiLearningPath.Domain.Assessments;

namespace AiLearningPath.Domain.LearningDna;

/// <summary>
/// Sở thích/thiết lập hồ sơ dùng làm đầu vào thuần cho <see cref="DnaBuilder"/>.
/// Đây là mô hình độc lập với entity EF Core (<c>Profile</c>), chỉ mang các trường
/// cần thiết để suy ra Learning DNA.
/// </summary>
/// <param name="StudyHoursPerDay">Số giờ học mỗi ngày của sinh viên, hợp lệ trong [0, 24].</param>
/// <param name="PreferredStudyPeriod">
/// Khung thời gian học ưa thích trong ngày: "Morning", "Afternoon", "Evening" hoặc "Night".
/// Giá trị null/rỗng/không hợp lệ được coi là "Evening" (mặc định).
/// </param>
public sealed record DnaProfileInput(double StudyHoursPerDay, string? PreferredStudyPeriod = null);

/// <summary>
/// Dữ liệu tiến độ học tập mới dùng để cập nhật Learning DNA Profile (R6.3).
/// Đây là mô hình đầu vào thuần, độc lập với entity EF Core.
/// </summary>
/// <param name="CompletionRate">Tỷ lệ hoàn thành kế hoạch gần đây, trong [0, 1].</param>
/// <param name="AverageFocusMinutes">Thời lượng tập trung trung bình mỗi phiên học (phút), không âm.</param>
/// <param name="ImprovedSkills">Các lĩnh vực kỹ năng đã tiến bộ (chuyển dần thành điểm mạnh).</param>
/// <param name="StruggledSkills">Các lĩnh vực kỹ năng còn gặp khó khăn (chuyển dần thành điểm yếu).</param>
public sealed record ProgressData(
    double CompletionRate,
    double AverageFocusMinutes,
    IReadOnlyList<string> ImprovedSkills,
    IReadOnlyList<string> StruggledSkills);

/// <summary>
/// Hồ sơ học tập cá nhân (Learning DNA) ở dạng mô hình thuần, độc lập với entity
/// EF Core <c>LearningDnaProfile</c> (R6.1). Gồm phong cách học, khung giờ học hiệu quả,
/// tốc độ tiếp thu, khả năng tập trung và điểm mạnh/yếu.
/// </summary>
/// <param name="LearningStyle">Phong cách học suy ra (Visual/Auditory/Kinesthetic/ReadingWriting).</param>
/// <param name="EffectiveHours">Danh sách khung giờ học hiệu quả (ví dụ: "18:00-20:00").</param>
/// <param name="LearningSpeed">Tốc độ tiếp thu, là hệ số trong [0.5, 2.0].</param>
/// <param name="FocusAbility">Khả năng tập trung, chuẩn hóa trong [0, 1].</param>
/// <param name="Strengths">Danh sách điểm mạnh (đã sắp xếp ổn định, tách biệt với điểm yếu).</param>
/// <param name="Weaknesses">Danh sách điểm yếu (đã sắp xếp ổn định, tách biệt với điểm mạnh).</param>
public sealed record LearningDnaResult(
    string LearningStyle,
    IReadOnlyList<string> EffectiveHours,
    double LearningSpeed,
    double FocusAbility,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses);
