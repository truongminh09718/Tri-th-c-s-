namespace AiLearningPath.Domain.Assessments;

/// <summary>
/// Một câu hỏi trong bài đánh giá năng lực. Đây là mô hình đầu vào thuần cho
/// <see cref="AssessmentGrader"/>, độc lập với cách lưu trữ (JSON/EF Core).
/// </summary>
/// <param name="Id">Định danh duy nhất của câu hỏi.</param>
/// <param name="SkillArea">Lĩnh vực kỹ năng mà câu hỏi đo lường (ví dụ: "Grammar", "Listening").</param>
/// <param name="CorrectOption">Đáp án đúng dùng để chấm điểm.</param>
public sealed record AssessmentQuestion(Guid Id, string SkillArea, string CorrectOption);

/// <summary>
/// Câu trả lời của sinh viên cho một câu hỏi. Một câu hỏi được coi là "đã trả lời"
/// khi có một <see cref="Answer"/> tương ứng với <see cref="SelectedOption"/> không rỗng.
/// </summary>
/// <param name="QuestionId">Định danh câu hỏi mà câu trả lời này thuộc về.</param>
/// <param name="SelectedOption">Lựa chọn của sinh viên. Rỗng/null nghĩa là chưa trả lời.</param>
public sealed record Answer(Guid QuestionId, string? SelectedOption);

/// <summary>
/// Kết quả chấm điểm theo một lĩnh vực kỹ năng cụ thể.
/// </summary>
/// <param name="SkillArea">Tên lĩnh vực kỹ năng.</param>
/// <param name="CorrectCount">Số câu trả lời đúng trong lĩnh vực này.</param>
/// <param name="TotalCount">Tổng số câu hỏi trong lĩnh vực này.</param>
/// <param name="Accuracy">Tỷ lệ đúng trong khoảng [0, 1] = CorrectCount / TotalCount.</param>
public sealed record SkillAreaScore(string SkillArea, int CorrectCount, int TotalCount, double Accuracy);

/// <summary>
/// Kết quả chấm bài đánh giá: trình độ suy ra, điểm tổng, điểm mạnh và điểm yếu.
/// Tập <see cref="Strengths"/> và <see cref="Weaknesses"/> luôn tách biệt (disjoint).
/// </summary>
/// <param name="Level">Trình độ suy ra (Beginner/Intermediate/Advanced).</param>
/// <param name="Score">Điểm tổng trong khoảng [0, 100].</param>
/// <param name="Strengths">Danh sách lĩnh vực kỹ năng là điểm mạnh (đã sắp xếp ổn định).</param>
/// <param name="Weaknesses">Danh sách lĩnh vực kỹ năng là điểm yếu (đã sắp xếp ổn định).</param>
/// <param name="SkillBreakdown">Chi tiết điểm theo từng lĩnh vực kỹ năng (đã sắp xếp ổn định).</param>
public sealed record AssessmentGradingResult(
    string Level,
    double Score,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses,
    IReadOnlyList<SkillAreaScore> SkillBreakdown);
