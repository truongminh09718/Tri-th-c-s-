namespace AiLearningPath.Domain.Common;

/// <summary>
/// Ngữ cảnh tiên quyết của một sinh viên, dùng làm đầu vào thuần cho các phép kiểm
/// tra điều kiện tiên quyết (prerequisite checks) trước khi sinh lộ trình (R7.4)
/// hoặc mô phỏng Academic Twin (R9.4).
/// </summary>
/// <remarks>
/// Đây là mô hình thuần, độc lập với entity EF Core. Chỉ mang các cờ trạng thái
/// cần thiết để xác định một bước nghiệp vụ có đủ điều kiện thực hiện hay không.
/// </remarks>
/// <param name="HasLearningGoal">True nếu sinh viên đã chọn một Learning Goal.</param>
/// <param name="HasAssessmentResult">True nếu sinh viên đã có kết quả AI Assessment.</param>
public sealed record StudentContext(bool HasLearningGoal, bool HasAssessmentResult);
