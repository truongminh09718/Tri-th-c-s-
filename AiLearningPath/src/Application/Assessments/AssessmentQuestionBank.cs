using AiLearningPath.Domain.Profiles;

namespace AiLearningPath.Application.Assessments;

/// <summary>
/// Mẫu một câu hỏi trong ngân hàng câu hỏi tĩnh (<see cref="AssessmentQuestionBank"/>).
/// Khác với <see cref="AssessmentQuestionItem"/>, mẫu này KHÔNG mang <c>Id</c>: định danh
/// được gán khi sinh bài đánh giá để mỗi lần làm bài có Id riêng.
/// </summary>
/// <param name="SkillArea">Lĩnh vực kỹ năng câu hỏi đo lường (ví dụ: "Listening", "JavaScript").</param>
/// <param name="Prompt">Nội dung câu hỏi hiển thị cho sinh viên.</param>
/// <param name="Options">Danh sách lựa chọn trả lời (thường 4 phương án).</param>
/// <param name="CorrectOption">Đáp án đúng — phải là một phần tử thuộc <see cref="Options"/>.</param>
public sealed record AssessmentQuestionTemplate(
    string SkillArea,
    string Prompt,
    IReadOnlyList<string> Options,
    string CorrectOption);

/// <summary>
/// Ngân hàng câu hỏi đánh giá năng lực tĩnh (deterministic) cho từng Learning Goal được
/// hỗ trợ trong <see cref="LearningGoalCatalog"/> (R5.1): IELTS, TOEIC, môn đại học,
/// Frontend, Backend, Data Analyst, AI Engineer.
///
/// <para>
/// Đây là logic thuần, không phụ thuộc I/O: cùng một Learning Goal và cùng số câu hỏi yêu
/// cầu luôn cho ra cùng một bộ câu hỏi (ngoại trừ <c>Id</c> được gán lúc sinh bài). Ngân
/// hàng giúp chức năng đánh giá hoạt động thật ngay cả khi chưa cấu hình Gemini, đồng thời
/// đóng vai trò fallback chất lượng cho <see cref="ExternalServices.IContentGenerator"/>.
/// </para>
/// </summary>
public static partial class AssessmentQuestionBank
{
    /// <summary>
    /// Trả về true nếu ngân hàng có câu hỏi cho <paramref name="learningGoal"/>.
    /// </summary>
    public static bool HasQuestions(string? learningGoal) =>
        !string.IsNullOrEmpty(learningGoal) && Bank.Value.ContainsKey(learningGoal);

    /// <summary>
    /// Lấy <paramref name="count"/> câu hỏi cho <paramref name="learningGoal"/> theo thứ tự
    /// ổn định. Nếu <paramref name="count"/> lớn hơn số câu có sẵn, ngân hàng lặp lại tuần
    /// hoàn từ đầu để luôn trả đủ số lượng (vẫn xác định). Trả về danh sách rỗng nếu goal
    /// không có trong ngân hàng.
    /// </summary>
    /// <param name="learningGoal">Learning Goal cần lấy câu hỏi.</param>
    /// <param name="count">Số câu hỏi cần lấy (phải &gt; 0 để trả về câu hỏi).</param>
    public static IReadOnlyList<AssessmentQuestionTemplate> GetQuestions(string? learningGoal, int count)
    {
        if (string.IsNullOrEmpty(learningGoal) ||
            count <= 0 ||
            !Bank.Value.TryGetValue(learningGoal, out var questions) ||
            questions.Count == 0)
        {
            return Array.Empty<AssessmentQuestionTemplate>();
        }

        if (count <= questions.Count)
        {
            return questions.Take(count).ToList();
        }

        // Lặp tuần hoàn để bảo đảm trả đủ count câu hỏi một cách xác định.
        var result = new List<AssessmentQuestionTemplate>(count);
        for (var i = 0; i < count; i++)
        {
            result.Add(questions[i % questions.Count]);
        }

        return result;
    }

    private static AssessmentQuestionTemplate Q(
        string skillArea, string prompt, string a, string b, string c, string d, string correct) =>
        new(skillArea, prompt, new[] { a, b, c, d }, correct);

    /// <summary>
    /// Ngân hàng câu hỏi theo Learning Goal. Khóa khớp với
    /// <see cref="LearningGoalCatalog.Supported"/>. Khởi tạo lười (lazy) để bảo đảm các
    /// trường dữ liệu (Toeic, Ielts...) nằm rải ở các file partial đã được nạp xong trước
    /// khi dựng từ điển — tránh phụ thuộc thứ tự khởi tạo trường static giữa các file.
    /// </summary>
    private static readonly Lazy<IReadOnlyDictionary<string, IReadOnlyList<AssessmentQuestionTemplate>>> Bank =
        new(() => new Dictionary<string, IReadOnlyList<AssessmentQuestionTemplate>>(StringComparer.Ordinal)
        {
            ["TOEIC"] = Toeic,
            ["IELTS"] = Ielts,
            ["UniversitySubject"] = UniversitySubject,
            ["FrontendDevelopment"] = FrontendDevelopment,
            ["BackendDevelopment"] = BackendDevelopment,
            ["DataAnalyst"] = DataAnalyst,
            ["AIEngineer"] = AiEngineer,
        });
}
