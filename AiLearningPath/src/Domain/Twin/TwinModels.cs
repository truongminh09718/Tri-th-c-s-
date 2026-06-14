namespace AiLearningPath.Domain.Twin;

/// <summary>
/// Ngữ cảnh học tập thuần của sinh viên dùng làm đầu vào cho <see cref="TwinValidator"/>.
/// Đây là mô hình độc lập với entity EF Core, chỉ mang các trường cần thiết để
/// kiểm tra tiên quyết mô phỏng Academic Twin (R9.4): sinh viên đã chọn Learning Goal
/// và đã có kết quả đánh giá năng lực (AssessmentResult) hay chưa.
/// </summary>
/// <param name="LearningGoal">
/// Mục tiêu học tập đã chọn. Null/rỗng nghĩa là sinh viên chưa chọn Learning Goal.
/// </param>
/// <param name="HasAssessmentResult">
/// True nếu sinh viên đã hoàn thành AI Assessment và có kết quả đánh giá; ngược lại False.
/// </param>
public sealed record StudentContext(string? LearningGoal, bool HasAssessmentResult)
{
    /// <summary>
    /// True khi sinh viên đã chọn một Learning Goal (chuỗi không null và không rỗng).
    /// </summary>
    public bool HasLearningGoal => !string.IsNullOrEmpty(LearningGoal);
}
