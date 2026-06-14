using System.Linq;
using AiLearningPath.Domain.Assessments;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Assessments;

/// <summary>
/// Property-based tests cho tính xác định (determinism) của
/// <see cref="AssessmentGrader.Grade"/>.
/// Validates: Requirements 5.2
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class AssessmentGraderDeterminismProperties
{
    /// <summary>Các lĩnh vực kỹ năng mẫu để câu hỏi gom nhóm có ý nghĩa.</summary>
    private static readonly string[] SkillAreas =
    {
        "Grammar", "Listening", "Reading", "Writing", "Speaking", "Vocabulary"
    };

    /// <summary>Các lựa chọn đáp án khả dĩ.</summary>
    private static readonly string[] Options = { "A", "B", "C", "D" };

    /// <summary>Sinh một câu hỏi với skill area và đáp án đúng từ tập mẫu.</summary>
    private static Gen<AssessmentQuestion> QuestionGen =>
        from id in Arb.Generate<Guid>()
        from skill in Gen.Elements(SkillAreas)
        from correct in Gen.Elements(Options)
        select new AssessmentQuestion(id, skill, correct);

    /// <summary>
    /// Sinh một bộ câu hỏi (có thể rỗng) cùng tập câu trả lời tương ứng.
    /// Một số câu trả lời trỏ tới câu hỏi thật (đúng hoặc sai), một số có thể bị bỏ trống.
    /// </summary>
    private static Gen<(AssessmentQuestion[] Questions, Answer[] Answers)> QuestionsAndAnswersGen =>
        from questions in Gen.ListOf(QuestionGen).Select(q => q.ToArray())
        from answers in GenAnswers(questions)
        select (questions, answers);

    private static Gen<Answer[]> GenAnswers(AssessmentQuestion[] questions)
    {
        if (questions.Length == 0)
        {
            return Gen.Constant(System.Array.Empty<Answer>());
        }

        // Với mỗi câu hỏi sinh 0..2 câu trả lời, lựa chọn có thể null (chưa trả lời)
        // hoặc một trong các option, nhằm phủ cả trường hợp đúng/sai/trùng lặp.
        var perQuestion =
            from q in Gen.Elements(questions)
            from option in Gen.Elements<string?>("A", "B", "C", "D", null)
            select new Answer(q.Id, option);

        return Gen.ListOf(perQuestion).Select(a => a.ToArray());
    }

    // Feature: ai-learning-path, Property 6: Chấm bài đánh giá có tính xác định
    // Với mọi bộ câu hỏi + tập câu trả lời, gọi Grade nhiều lần với cùng đầu vào
    // luôn cho ra cùng một kết quả (Level, Score, Strengths, Weaknesses, SkillBreakdown).
    [Property(MaxTest = 100)]
    public Property Grade_IsDeterministic_ForSameInput()
    {
        return Prop.ForAll(Arb.From(QuestionsAndAnswersGen), input =>
        {
            var (questions, answers) = input;

            var first = AssessmentGrader.Grade(questions, answers);
            var second = AssessmentGrader.Grade(questions, answers);

            bool levelEqual = string.Equals(first.Level, second.Level, System.StringComparison.Ordinal);
            bool scoreEqual = first.Score == second.Score;
            bool strengthsEqual = first.Strengths.SequenceEqual(second.Strengths, System.StringComparer.Ordinal);
            bool weaknessesEqual = first.Weaknesses.SequenceEqual(second.Weaknesses, System.StringComparer.Ordinal);
            bool breakdownEqual = first.SkillBreakdown.SequenceEqual(second.SkillBreakdown);

            return (levelEqual && scoreEqual && strengthsEqual && weaknessesEqual && breakdownEqual)
                .Label(
                    $"level={levelEqual}, score={scoreEqual}, strengths={strengthsEqual}, " +
                    $"weaknesses={weaknessesEqual}, breakdown={breakdownEqual} " +
                    $"(questions={questions.Length}, answers={answers.Length})");
        });
    }
}
