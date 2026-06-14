using AiLearningPath.Domain.Progress;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Progress;

/// <summary>
/// Property-based tests cho <see cref="ProgressCalculator.ComputeLearningScore"/> —
/// Learning Score luôn nằm trong [0, 100] (R8.1, R8.2).
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class ProgressCalculatorLearningScoreProperties
{
    /// <summary>
    /// Sinh giá trị double "thử thách" bao phủ cả khoảng kỳ vọng lẫn các trường hợp
    /// bất thường: ngoài khoảng (âm, rất lớn), NaN, ±Infinity. Mục đích là gây áp lực
    /// lên cơ chế kẹp giá trị (clamp) của <see cref="ProgressCalculator.ComputeLearningScore"/>.
    /// </summary>
    private static Gen<double> ChallengingDoubleGen =>
        Gen.Frequency(
            // Giá trị "bình thường" trong khoảng kỳ vọng [-50, 150].
            Tuple.Create(6, Gen.Choose(-50, 150).Select(x => (double)x)),
            // Giá trị cực trị lớn theo cả hai dấu.
            Tuple.Create(2, Gen.Elements(
                double.MaxValue, double.MinValue, -1_000_000d, 1_000_000d, -0.0001d, 100.0001d)),
            // Các giá trị bất thường cần được xử lý phòng vệ.
            Tuple.Create(2, Gen.Elements(
                double.NaN, double.PositiveInfinity, double.NegativeInfinity)));

    /// <summary>Sinh <see cref="LearningScoreInput"/> với mọi thành phần có thể bất thường.</summary>
    private static Gen<LearningScoreInput> InputGen =>
        from completion in ChallengingDoubleGen
        from assessment in ChallengingDoubleGen
        from consistency in ChallengingDoubleGen
        select new LearningScoreInput(completion, assessment, consistency);

    // Feature: ai-learning-path, Property 13: Learning Score luôn nằm trong [0, 100]
    // Với mọi LearningScoreInput (kể cả thành phần ngoài khoảng, NaN, ±Infinity),
    // ComputeLearningScore luôn trả về một giá trị hữu hạn trong khoảng [0, 100].
    [Property(MaxTest = 100)]
    public Property ComputeLearningScore_IsAlwaysWithin_ZeroToHundred()
    {
        return Prop.ForAll(Arb.From(InputGen), input =>
        {
            var score = ProgressCalculator.ComputeLearningScore(input);

            return (!double.IsNaN(score) && !double.IsInfinity(score) && score >= 0d && score <= 100d)
                .Label($"completion={input.CompletionRate} assessment={input.AssessmentScore} " +
                       $"consistency={input.StudyConsistency} score={score}");
        });
    }
}
