using AiLearningPath.Domain.Assessments;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Assessments;

/// <summary>
/// Property-based tests cho <see cref="AssessmentGrader.ValidateCompleteness"/>
/// (kiểm tra đầy đủ câu trả lời trước khi nộp bài đánh giá).
/// Validates: Requirements 5.4
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class AssessmentCompletenessProperties
{
    /// <summary>Sinh một lĩnh vực kỹ năng không rỗng từ một tập nhỏ cố định.</summary>
    private static Gen<string> SkillAreaGen =>
        Gen.Elements("Grammar", "Listening", "Reading", "Writing", "Speaking");

    /// <summary>Sinh một câu hỏi với Id duy nhất (Id được gán ngoài generator để đảm bảo duy nhất).</summary>
    private static Gen<AssessmentQuestion> QuestionGen(Guid id) =>
        from skill in SkillAreaGen
        from correct in Gen.Elements("A", "B", "C", "D")
        select new AssessmentQuestion(id, skill, correct);

    /// <summary>
    /// Sinh một bộ câu hỏi (1..10) với các Id duy nhất. ValidateCompleteness chỉ quan tâm
    /// tới Id nên việc đảm bảo Id duy nhất là đủ để xây dựng input space hợp lệ.
    /// </summary>
    private static Gen<IReadOnlyList<AssessmentQuestion>> QuestionsGen =>
        from count in Gen.Choose(1, 10)
        from questions in GenSequence(count)
        select (IReadOnlyList<AssessmentQuestion>)questions;

    private static Gen<List<AssessmentQuestion>> GenSequence(int count)
    {
        var ids = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
        var gens = ids.Select(QuestionGen).ToArray();
        return Gen.Sequence(gens).Select(arr => arr.ToList());
    }

    /// <summary>Sinh một lựa chọn câu trả lời không rỗng.</summary>
    private static Gen<string> NonEmptyOptionGen =>
        Gen.Elements("A", "B", "C", "D");

    /// <summary>
    /// Với một bộ câu hỏi cho trước, sinh ngẫu nhiên tập câu trả lời phủ cả ba trường hợp
    /// cho mỗi câu hỏi: (0) không trả lời, (1) trả lời rỗng/null, (2) trả lời không rỗng.
    /// Nhờ vậy input space bao gồm cả bài đầy đủ lẫn bài còn thiếu câu trả lời.
    /// </summary>
    private static Gen<List<Answer>> BuildAnswersGen(IReadOnlyList<AssessmentQuestion> questions)
    {
        // Với mỗi câu hỏi sinh một mã trạng thái 0..2 quyết định cách trả lời.
        var kindGens = questions.Select(_ => Gen.Choose(0, 2)).ToArray();
        return Gen.Sequence(kindGens).Select(kinds =>
        {
            var answers = new List<Answer>();
            for (int i = 0; i < questions.Count; i++)
            {
                switch (kinds[i])
                {
                    case 0:
                        // Không thêm câu trả lời nào (chưa trả lời).
                        break;
                    case 1:
                        // Trả lời rỗng → vẫn coi là chưa trả lời.
                        answers.Add(new Answer(questions[i].Id, string.Empty));
                        break;
                    default:
                        // Trả lời không rỗng.
                        answers.Add(new Answer(questions[i].Id, "A"));
                        break;
                }
            }

            return answers;
        });
    }

    // Feature: ai-learning-path, Property 7: Nộp bài thiếu câu trả lời bị từ chối
    // Với mọi bài đánh giá và tập câu trả lời: ValidateCompleteness hợp lệ KHI VÀ CHỈ KHI
    // mọi câu hỏi đều đã được trả lời (có SelectedOption không rỗng).
    [Property(MaxTest = 100)]
    public Property ValidateCompleteness_IsValid_IffAllQuestionsAnswered()
    {
        // Với mỗi câu hỏi, sinh ngẫu nhiên xem nó có được trả lời hay không, và nếu có
        // thì lựa chọn là rỗng hay không rỗng. Việc này phủ cả input đầy đủ lẫn thiếu.
        var arb = Arb.From(
            from questions in QuestionsGen
            from answers in BuildAnswersGen(questions)
            select (questions, answers));

        return Prop.ForAll(arb, tuple =>
        {
            var (questions, answers) = tuple;

            bool actual = AssessmentGrader.ValidateCompleteness(questions, answers).IsValid;

            // Tập câu hỏi đã được trả lời với lựa chọn không rỗng.
            var answeredIds = answers
                .Where(a => !string.IsNullOrEmpty(a.SelectedOption))
                .Select(a => a.QuestionId)
                .ToHashSet();
            bool expected = questions.All(q => answeredIds.Contains(q.Id));

            return (actual == expected)
                .Label($"actual={actual} expected={expected} " +
                       $"questions={questions.Count} answered={answeredIds.Count}");
        });
    }

    // Feature: ai-learning-path, Property 7: Nộp bài thiếu câu trả lời bị từ chối
    // (a) Khi mọi câu hỏi đều có câu trả lời không rỗng, kết quả luôn hợp lệ.
    [Property(MaxTest = 100)]
    public Property ValidateCompleteness_AllAnswered_IsValid()
    {
        var arb = Arb.From(
            from questions in QuestionsGen
            from options in Gen.Sequence(
                questions.Select(_ => NonEmptyOptionGen).ToArray())
            select (questions, options));

        return Prop.ForAll(arb, tuple =>
        {
            var (questions, options) = tuple;
            var answers = questions
                .Zip(options, (q, opt) => new Answer(q.Id, opt))
                .ToList();

            return AssessmentGrader.ValidateCompleteness(questions, answers).IsValid
                .Label("Mọi câu hỏi được trả lời nhưng kết quả lại không hợp lệ.");
        });
    }

    // Feature: ai-learning-path, Property 7: Nộp bài thiếu câu trả lời bị từ chối
    // (b) Khi tồn tại ít nhất một câu hỏi thiếu câu trả lời không rỗng, kết quả luôn không hợp lệ.
    [Property(MaxTest = 100)]
    public Property ValidateCompleteness_MissingAnswer_IsInvalid()
    {
        var arb = Arb.From(
            from questions in QuestionsGen
            // Chọn một câu hỏi sẽ bị bỏ trống.
            from skipIndex in Gen.Choose(0, questions.Count - 1)
            // Các câu còn lại được trả lời không rỗng; câu bị bỏ thì rỗng hoặc null hoặc thiếu hẳn.
            from emptyKind in Gen.Choose(0, 2)
            select (questions, skipIndex, emptyKind));

        return Prop.ForAll(arb, tuple =>
        {
            var (questions, skipIndex, emptyKind) = tuple;
            var answers = new List<Answer>();
            for (int i = 0; i < questions.Count; i++)
            {
                if (i == skipIndex)
                {
                    // emptyKind 0: bỏ hẳn câu trả lời; 1: SelectedOption rỗng; 2: SelectedOption null.
                    if (emptyKind == 1)
                    {
                        answers.Add(new Answer(questions[i].Id, string.Empty));
                    }
                    else if (emptyKind == 2)
                    {
                        answers.Add(new Answer(questions[i].Id, null));
                    }
                    // emptyKind == 0: không thêm answer nào cho câu này.
                }
                else
                {
                    answers.Add(new Answer(questions[i].Id, "A"));
                }
            }

            return (!AssessmentGrader.ValidateCompleteness(questions, answers).IsValid)
                .Label($"skipIndex={skipIndex} emptyKind={emptyKind} expected invalid but was valid");
        });
    }
}
