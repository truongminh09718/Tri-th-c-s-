using AiLearningPath.Domain.Assessments;
using AiLearningPath.Domain.LearningDna;
using AiLearningPath.Domain.Paths;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Paths;

/// <summary>
/// Property-based test cho <see cref="PathBuilder.Build"/>: lộ trình sinh ra bao phủ
/// đúng và đủ toàn bộ khoảng thời gian mục tiêu — tập số thứ tự ngày
/// (<see cref="PathDayPlan.DayIndex"/>) của toàn lộ trình đúng bằng {1, …, targetDays},
/// không trùng lặp và không bỏ sót (R7.1, R7.2).
/// Validates: Requirements 7.1, 7.2
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class PathBuilderCoverageProperties
{
    /// <summary>Tập lĩnh vực kỹ năng dùng để dựng input điểm mạnh/yếu.</summary>
    private static readonly string[] SkillAreas =
    {
        "Grammar", "Listening", "Reading", "Writing", "Speaking", "Vocabulary"
    };

    /// <summary>
    /// Sinh danh sách kỹ năng không rỗng, không trùng, có thể rỗng để bao phủ cả
    /// trường hợp không có dữ liệu (PathBuilder dùng kỹ năng mặc định).
    /// </summary>
    private static Gen<IReadOnlyList<string>> SkillListGen =>
        from count in Gen.Choose(0, SkillAreas.Length)
        from indices in Gen.Sequence(Enumerable.Range(0, count).Select(_ => Gen.Choose(0, SkillAreas.Length - 1)))
        select (IReadOnlyList<string>)indices.Distinct().Select(i => SkillAreas[i]).ToList();

    /// <summary>Sinh một <see cref="AssessmentGradingResult"/> tối thiểu hợp lệ.</summary>
    private static Gen<AssessmentGradingResult> AssessmentResultGen =>
        from strengths in SkillListGen
        from weaknesses in SkillListGen
        select new AssessmentGradingResult(
            "Intermediate",
            50d,
            strengths,
            weaknesses,
            Array.Empty<SkillAreaScore>());

    /// <summary>Sinh một <see cref="LearningDnaResult"/> tối thiểu hợp lệ.</summary>
    private static Gen<LearningDnaResult> DnaResultGen =>
        from strengths in SkillListGen
        from weaknesses in SkillListGen
        select new LearningDnaResult(
            "Visual",
            new[] { "18:00-20:00" },
            1.0,
            0.5,
            strengths,
            weaknesses);

    /// <summary>
    /// Sinh bộ đầu vào hợp lệ cho <see cref="PathBuilder.Build"/>: targetDays trong [1, 400],
    /// số giờ mỗi ngày không âm, cùng kết quả đánh giá và Learning DNA tối thiểu.
    /// </summary>
    private static Gen<(int TargetDays, double DailyHours, AssessmentGradingResult Result, LearningDnaResult Dna)> InputGen =>
        from targetDays in Gen.Choose(1, 400)
        from dailyHours in Gen.Choose(0, 8)
        from result in AssessmentResultGen
        from dna in DnaResultGen
        select (targetDays, (double)dailyHours, result, dna);

    // Feature: ai-learning-path, Property 10: Lộ trình bao phủ toàn bộ thời gian mục tiêu
    // Với mọi targetDays >= 1, tập số thứ tự ngày của toàn bộ lộ trình đúng bằng
    // {1, 2, …, targetDays}: bao phủ đầy đủ, không trùng lặp và không bỏ sót.
    [Property(MaxTest = 100)]
    public Property Build_CoversEntireTargetTimeSpan()
    {
        return Prop.ForAll(Arb.From(InputGen), input =>
        {
            var (targetDays, dailyHours, result, dna) = input;

            var path = PathBuilder.Build("Test goal", result, dna, targetDays, dailyHours);

            var dayIndices = path.AllDays.Select(d => d.DayIndex).ToList();
            var expected = Enumerable.Range(1, targetDays).ToHashSet();
            var actual = dayIndices.ToHashSet();

            bool noDuplicates = dayIndices.Count == actual.Count;
            bool exactCoverage = actual.SetEquals(expected);

            return (noDuplicates && exactCoverage)
                .Label($"targetDays={targetDays} dayCount={dayIndices.Count} " +
                       $"distinct={actual.Count} noDuplicates={noDuplicates} " +
                       $"exactCoverage={exactCoverage}");
        });
    }
}
