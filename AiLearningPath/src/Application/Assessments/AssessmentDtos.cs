namespace AiLearningPath.Application.Assessments;

/// <summary>
/// Trạng thái của một bài đánh giá lưu trong <see cref="Domain.Entities.Assessment.Status"/>.
/// </summary>
public static class AssessmentStatus
{
    /// <summary>Bài đánh giá đã được tạo và đang chờ nộp.</summary>
    public const string InProgress = "InProgress";

    /// <summary>Bài đánh giá đã được nộp và chấm điểm.</summary>
    public const string Completed = "Completed";
}

/// <summary>
/// Loại nội dung yêu cầu dịch vụ sinh nội dung AI (<see cref="ExternalServices.IContentGenerator"/>)
/// tạo ra cho bài đánh giá năng lực.
/// </summary>
public static class AssessmentContentKind
{
    public const string Assessment = "Assessment";
}

/// <summary>
/// Một câu hỏi đầy đủ của bài đánh giá như được lưu trong <c>QuestionsJson</c>.
/// Bao gồm cả các tùy chọn và đáp án đúng phục vụ chấm điểm phía máy chủ.
/// Khác với <see cref="Domain.Assessments.AssessmentQuestion"/> (chỉ chứa dữ liệu cần để chấm),
/// kiểu này mang đủ thông tin để hiển thị cho sinh viên.
/// </summary>
/// <param name="Id">Định danh duy nhất của câu hỏi.</param>
/// <param name="SkillArea">Lĩnh vực kỹ năng câu hỏi đo lường.</param>
/// <param name="Prompt">Nội dung câu hỏi hiển thị cho sinh viên.</param>
/// <param name="Options">Danh sách lựa chọn trả lời.</param>
/// <param name="CorrectOption">Đáp án đúng dùng để chấm điểm.</param>
public sealed record AssessmentQuestionItem(
    Guid Id,
    string SkillArea,
    string Prompt,
    IReadOnlyList<string> Options,
    string CorrectOption);

/// <summary>
/// Cấu trúc JSON do <see cref="ExternalServices.IContentGenerator"/> trả về khi sinh
/// bộ câu hỏi đánh giá. Lớp Application phân tích payload này thành danh sách câu hỏi.
/// </summary>
/// <param name="Questions">Danh sách câu hỏi được sinh ra.</param>
public sealed record GeneratedAssessmentContent(IReadOnlyList<GeneratedQuestion> Questions);

/// <summary>
/// Một câu hỏi trong payload sinh nội dung AI. <see cref="Id"/> có thể rỗng — khi đó
/// lớp Application sẽ gán một định danh mới để đảm bảo mỗi câu hỏi có Id duy nhất.
/// </summary>
/// <param name="Id">Định danh câu hỏi (tùy chọn; sinh mới nếu rỗng).</param>
/// <param name="SkillArea">Lĩnh vực kỹ năng câu hỏi đo lường.</param>
/// <param name="Prompt">Nội dung câu hỏi.</param>
/// <param name="Options">Danh sách lựa chọn trả lời.</param>
/// <param name="CorrectOption">Đáp án đúng.</param>
public sealed record GeneratedQuestion(
    Guid? Id,
    string SkillArea,
    string Prompt,
    IReadOnlyList<string>? Options,
    string CorrectOption);

/// <summary>
/// Số lượng câu hỏi mặc định yêu cầu dịch vụ AI sinh ra cho một bài đánh giá.
/// </summary>
public static class AssessmentDefaults
{
    public const int QuestionCount = 10;
}
