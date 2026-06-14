using AiLearningPath.Domain.Common;

namespace AiLearningPath.Domain.Assessments;

/// <summary>
/// Logic thuần chấm bài đánh giá năng lực (R5.2) và kiểm tra đầy đủ câu trả lời
/// trước khi nộp (R5.4). Không phụ thuộc I/O nên có thể kiểm thử bằng
/// property-based testing.
/// </summary>
public static class AssessmentGrader
{
    /// <summary>Ngưỡng tỷ lệ đúng (>=) để một lĩnh vực kỹ năng được coi là điểm mạnh.</summary>
    public const double StrengthThreshold = 0.7d;

    /// <summary>Ngưỡng tỷ lệ đúng (&lt;) để một lĩnh vực kỹ năng được coi là điểm yếu.</summary>
    public const double WeaknessThreshold = 0.5d;

    /// <summary>Ngưỡng điểm tổng (>=) để suy ra trình độ Advanced.</summary>
    public const double AdvancedScoreThreshold = 80d;

    /// <summary>Ngưỡng điểm tổng (>=) để suy ra trình độ Intermediate.</summary>
    public const double IntermediateScoreThreshold = 50d;

    /// <summary>
    /// Chấm bài đánh giá: tính điểm theo từng lĩnh vực kỹ năng, suy ra trình độ,
    /// xác định điểm mạnh và điểm yếu (R5.2).
    /// </summary>
    /// <remarks>
    /// Hàm này có tính xác định (deterministic): cùng một bộ câu hỏi và cùng tập
    /// câu trả lời luôn cho ra cùng một kết quả. Tập điểm mạnh và tập điểm yếu luôn
    /// tách biệt (disjoint) vì mỗi lĩnh vực kỹ năng được phân loại vào tối đa một nhóm.
    /// </remarks>
    /// <param name="questions">Bộ câu hỏi của bài đánh giá.</param>
    /// <param name="answers">Tập câu trả lời của sinh viên.</param>
    /// <returns>Kết quả chấm điểm gồm trình độ, điểm số, điểm mạnh và điểm yếu.</returns>
    public static AssessmentGradingResult Grade(
        IReadOnlyList<AssessmentQuestion> questions,
        IReadOnlyList<Answer> answers)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(answers);

        // Xây lookup câu trả lời theo QuestionId. Nếu có nhiều câu trả lời cho cùng
        // một câu hỏi, giữ câu trả lời đầu tiên gặp được để đảm bảo tính xác định.
        var answerByQuestion = new Dictionary<Guid, string?>();
        foreach (var answer in answers)
        {
            if (!answerByQuestion.ContainsKey(answer.QuestionId))
            {
                answerByQuestion[answer.QuestionId] = answer.SelectedOption;
            }
        }

        // Gom nhóm theo lĩnh vực kỹ năng, tính số câu đúng / tổng số câu.
        var correctBySkill = new Dictionary<string, int>(StringComparer.Ordinal);
        var totalBySkill = new Dictionary<string, int>(StringComparer.Ordinal);

        var totalCorrect = 0;
        foreach (var question in questions)
        {
            totalBySkill[question.SkillArea] = totalBySkill.GetValueOrDefault(question.SkillArea) + 1;

            var isCorrect = answerByQuestion.TryGetValue(question.Id, out var selected)
                && selected is not null
                && string.Equals(selected, question.CorrectOption, StringComparison.Ordinal);

            if (isCorrect)
            {
                correctBySkill[question.SkillArea] = correctBySkill.GetValueOrDefault(question.SkillArea) + 1;
                totalCorrect++;
            }
        }

        // Sắp xếp các lĩnh vực kỹ năng theo tên (ordinal) để có đầu ra ổn định.
        var skillBreakdown = new List<SkillAreaScore>();
        var strengths = new List<string>();
        var weaknesses = new List<string>();

        foreach (var skill in totalBySkill.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var total = totalBySkill[skill];
            var correct = correctBySkill.GetValueOrDefault(skill);
            var accuracy = total > 0 ? (double)correct / total : 0d;

            skillBreakdown.Add(new SkillAreaScore(skill, correct, total, accuracy));

            // Phân loại tách biệt: điểm mạnh khi accuracy >= StrengthThreshold,
            // điểm yếu khi accuracy < WeaknessThreshold. Khoảng giữa không thuộc nhóm nào.
            if (accuracy >= StrengthThreshold)
            {
                strengths.Add(skill);
            }
            else if (accuracy < WeaknessThreshold)
            {
                weaknesses.Add(skill);
            }
        }

        var totalQuestions = questions.Count;
        var score = totalQuestions > 0 ? (double)totalCorrect / totalQuestions * 100d : 0d;
        var level = InferLevel(score);

        return new AssessmentGradingResult(level, score, strengths, weaknesses, skillBreakdown);
    }

    /// <summary>
    /// Kiểm tra đầy đủ câu trả lời trước khi nộp (R5.4). Trả về kết quả không hợp lệ
    /// nếu còn bất kỳ câu hỏi nào chưa được trả lời; hợp lệ khi mọi câu hỏi đều đã trả lời.
    /// </summary>
    /// <param name="questions">Bộ câu hỏi của bài đánh giá.</param>
    /// <param name="answers">Tập câu trả lời của sinh viên.</param>
    public static ValidationResult ValidateCompleteness(
        IReadOnlyList<AssessmentQuestion> questions,
        IReadOnlyList<Answer> answers)
    {
        ArgumentNullException.ThrowIfNull(questions);
        ArgumentNullException.ThrowIfNull(answers);

        // Tập các câu hỏi đã được trả lời (có lựa chọn không rỗng).
        var answeredQuestionIds = new HashSet<Guid>();
        foreach (var answer in answers)
        {
            if (!string.IsNullOrEmpty(answer.SelectedOption))
            {
                answeredQuestionIds.Add(answer.QuestionId);
            }
        }

        var hasUnanswered = questions.Any(q => !answeredQuestionIds.Contains(q.Id));
        if (hasUnanswered)
        {
            return ValidationResult.Failure(
                "Vui lòng hoàn thành tất cả câu hỏi trước khi nộp bài đánh giá.");
        }

        return ValidationResult.Success();
    }

    private static string InferLevel(double score)
    {
        if (score >= AdvancedScoreThreshold)
        {
            return "Advanced";
        }

        if (score >= IntermediateScoreThreshold)
        {
            return "Intermediate";
        }

        return "Beginner";
    }
}
