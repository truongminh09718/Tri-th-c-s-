namespace AiLearningPath.Application.Profiles;

/// <summary>
/// Dữ liệu đầu vào để tạo hoặc cập nhật hồ sơ cá nhân (R3.1).
/// Bao gồm họ tên, mã sinh viên, ngành học, mục tiêu học tập, mục tiêu nghề nghiệp
/// và số giờ học mỗi ngày.
/// </summary>
/// <param name="FullName">Họ tên đầy đủ của sinh viên.</param>
/// <param name="StudentCode">Mã số sinh viên.</param>
/// <param name="Major">Ngành học.</param>
/// <param name="LearningGoal">Mục tiêu học tập (tùy chọn ở bước tạo hồ sơ; nếu cung cấp phải hợp lệ).</param>
/// <param name="TargetScore">Điểm số mục tiêu (chỉ áp dụng cho goal yêu cầu điểm, ví dụ IELTS/TOEIC).</param>
/// <param name="CareerGoal">Mục tiêu nghề nghiệp.</param>
/// <param name="StudyHoursPerDay">Số giờ học mỗi ngày, hợp lệ trong khoảng [0, 24].</param>
public sealed record ProfileInput(
    string FullName,
    string StudentCode,
    string Major,
    string? LearningGoal,
    int? TargetScore,
    string CareerGoal,
    double StudyHoursPerDay);

/// <summary>
/// Lựa chọn Learning Goal kèm điểm số mục tiêu tùy chọn (R4.2, R4.3).
/// </summary>
/// <param name="Goal">Mã Learning Goal được chọn (phải thuộc danh mục hỗ trợ).</param>
/// <param name="TargetScore">
/// Điểm số mục tiêu. Bắt buộc khi goal yêu cầu điểm số
/// (<see cref="AiLearningPath.Domain.Profiles.LearningGoalCatalog.RequiresTargetScore"/>).
/// </param>
public sealed record LearningGoalSelection(
    string Goal,
    int? TargetScore);
