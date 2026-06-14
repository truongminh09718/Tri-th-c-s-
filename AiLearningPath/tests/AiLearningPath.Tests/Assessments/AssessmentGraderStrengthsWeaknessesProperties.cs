using AiLearningPath.Domain.Assessments;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Assessments;

/// <summary>
/// Property-based test cho <see cref="AssessmentGrader.Grade"/>: tập điểm mạnh và
/// tập điểm yếu luôn tách biệt (disjoint) — không một lĩnh vực kỹ năng nào vừa là
/// điểm mạnh vừa là điểm yếu.
/// Validates: Requirements 5.2
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class AssessmentGraderStrengthsWeaknessesProperties
{
    /// <summary>Tập tên lĩnh vực kỹ năng dùng để sinh câu hỏi (giới hạn để các câu hỏi gom nhóm).</summary>
    private static readonly string[] SkillAreas =
    {
        "Grammar", "Listening", "Reading", "Writing", "Speaking", "Vocabulary"
    };

    /// <summary>Tập đáp án có thể chọn cho mỗi câu hỏi.</summary>
    private static readonly string[] Options = { "A", "B", "C", "D" };

    /// <summary>
    /// Sinh một cặp (questions, answers) hợp lệ: mỗi câu hỏi có một skill area và một
    /// đáp án đúng; mỗi câu hỏi có thể được trả lời (đúng/sai) hoặc bỏ trống. Việc sinh
    /// trải đều các tỷ lệ đúng để bao phủ cả vùng điểm mạnh, điểm yếu và vùng giữa.
    /// </summary>
    private static Gen<(AssessmentQuestion[] Questions, Answer[] Answers)> QuestionsAndAnswersGen =>
        from count in Gen.Choose(0, 30)
        from skillIndices in Gen.ArrayOf(count, Gen.Choose(0, SkillAreas.Length - 1))
        from correctIndices in Gen.ArrayOf(count, Gen.Choose(0, Options.Length - 1))
        // 0 = bỏ trống, 1 = trả lời đúng, 2 = trả lời sai (chọn đáp án kế tiếp)
        from answerModes in Gen.ArrayOf(count, Gen.Choose(0, 2))
        select BuildSample(skillIndices, correctIndices, answerModes);

    private static (AssessmentQuestion[], Answer[]) BuildSample(
        int[] skillIndices, int[] correctIndices, int[] answerModes)
    {
        var questions = new AssessmentQuestion[skillIndices.Length];
        var answers = new List<Answer>(skillIndices.Length);

        for (int i = 0; i < skillIndices.Length; i++)
        {
            var skill = SkillAreas[skillIndices[i]];
            var correctOption = Options[correctIndices[i]];
            var question = new AssessmentQuestion(Guid.NewGuid(), skill, correctOption);
            questions[i] = question;

            switch (answerModes[i])
            {
                case 1: // trả lời đúng
                    answers.Add(new Answer(question.Id, correctOption));
                    break;
                case 2: // trả lời sai (chọn đáp án khác)
                    var wrongOption = Options[(correctIndices[i] + 1) % Options.Length];
                    answers.Add(new Answer(question.Id, wrongOption));
                    break;
                default: // bỏ trống
                    break;
            }
        }

        return (questions, answers.ToArray());
    }

    // Feature: ai-learning-path, Property 8: Điểm mạnh và điểm yếu tách biệt
    [Property(MaxTest = 100)]
    public Property StrengthsAndWeaknesses_AreAlwaysDisjoint()
    {
        return Prop.ForAll(Arb.From(QuestionsAndAnswersGen), sample =>
        {
            var (questions, answers) = sample;

            var result = AssessmentGrader.Grade(questions, answers);

            var intersection = result.Strengths.Intersect(result.Weaknesses).ToArray();

            return (intersection.Length == 0)
                .Label($"overlap=[{string.Join(", ", intersection)}] " +
                       $"strengths=[{string.Join(", ", result.Strengths)}] " +
                       $"weaknesses=[{string.Join(", ", result.Weaknesses)}]");
        });
    }
}
