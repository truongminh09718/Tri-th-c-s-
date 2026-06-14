using AiLearningPath.Domain.Progress;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Progress;

/// <summary>
/// Property-based tests cho <see cref="ProgressCalculator.ComputeTotalStudyHours"/> —
/// tổng số giờ học bằng tổng thời lượng các phiên học và luôn không âm (R8.1).
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class ProgressCalculatorTotalStudyHoursProperties
{
    /// <summary>
    /// Sinh thời lượng phiên học bao phủ cả các giá trị bất thường mà hàm xử lý phòng vệ:
    /// giá trị hữu hạn (âm và không âm) cùng NaN, +Infinity, -Infinity.
    /// </summary>
    private static Gen<double> DurationGen =>
        Gen.Frequency(
            Tuple.Create(8, from whole in Gen.Choose(-100, 1000)
                            from frac in Gen.Choose(0, 4)
                            select whole + (frac * 0.25)),
            Tuple.Create(1, Gen.Elements(double.NaN, double.PositiveInfinity, double.NegativeInfinity)));

    /// <summary>Sinh một danh sách (0..50) phiên học với thời lượng đa dạng.</summary>
    private static Gen<List<StudySessionInput>> SessionsGen =>
        from durations in Gen.ListOf(DurationGen).Select(xs => xs.Take(50).ToList())
        select durations
            .Select(d => new StudySessionInput(new DateTime(2024, 1, 1), d))
            .ToList();

    /// <summary>Thời lượng phòng vệ: NaN/Infinity/âm được coi như 0 (khớp implementation).</summary>
    private static double NonNegative(double value)
        => double.IsNaN(value) || double.IsInfinity(value) || value < 0d ? 0d : value;

    // Feature: ai-learning-path, Property 14: Tổng số giờ học bằng tổng các phiên học
    // Với mọi danh sách phiên học, ComputeTotalStudyHours trả về giá trị bằng tổng thời
    // lượng (đã xử lý phòng vệ: âm/NaN/Infinity coi như 0) của tất cả các phiên, và kết
    // quả luôn không âm.
    [Property(MaxTest = 100)]
    public Property ComputeTotalStudyHours_EqualsSumOfSessions_AndIsNonNegative()
    {
        return Prop.ForAll(Arb.From(SessionsGen), sessions =>
        {
            var total = ProgressCalculator.ComputeTotalStudyHours(sessions);

            // Oracle: tổng thời lượng đã được xử lý phòng vệ của từng phiên.
            var expected = sessions.Sum(s => NonNegative(s.DurationHours));

            var matchesSum = Math.Abs(total - expected) <= 1e-9;
            var isNonNegative = total >= 0d;

            return (matchesSum && isNonNegative)
                .Label($"total={total} expected={expected} count={sessions.Count}");
        });
    }
}
