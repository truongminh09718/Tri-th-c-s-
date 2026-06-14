using AiLearningPath.Domain.Paths;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Paths;

/// <summary>
/// Property-based tests cho <see cref="PathBuilder.AssessFeasibility"/> — cảnh báo
/// tính khả thi của lộ trình khi thời gian mục tiêu không đủ (R7.5).
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class PathBuilderFeasibilityProperties
{
    /// <summary>Sinh số giờ hữu hạn, không âm trong khoảng [0, 1000].</summary>
    private static Gen<double> NonNegativeHoursGen =>
        from whole in Gen.Choose(0, 1000)
        from frac in Gen.Choose(0, 4)
        select whole + (frac * 0.25);

    /// <summary>
    /// Sinh một <see cref="PathPlan"/> với <c>TargetDays</c> trong [1, 365] và một danh
    /// sách nhiệm vụ (0..30) có số giờ ước lượng không âm. Tổng giờ ước lượng được điều
    /// khiển độc lập với <c>TargetDays</c> để bao phủ cả hai phía của ngưỡng cảnh báo.
    /// </summary>
    private static Gen<(PathPlan path, double studyHoursPerDay)> PlanGen =>
        from targetDays in Gen.Choose(1, 365)
        from taskHours in Gen.ListOf(NonNegativeHoursGen).Select(xs => xs.Take(30).ToList())
        from studyHoursPerDay in NonNegativeHoursGen
        select (BuildPlan(targetDays, taskHours), studyHoursPerDay);

    private static PathPlan BuildPlan(int targetDays, IReadOnlyList<double> taskHours)
    {
        var days = taskHours
            .Select((hours, i) => new PathDayPlan(i + 1, new PathTaskPlan("Skill", "Task", hours)))
            .ToList();
        var phases = new List<PathPhasePlan>
        {
            new(1, 1, "Tháng 1 - Tuần 1", days),
        };
        return new PathPlan("Goal", targetDays, phases);
    }

    // Feature: ai-learning-path, Property 11: Cảnh báo khả thi khi thời gian không đủ
    // Với mọi lộ trình và số giờ học mỗi ngày: AssessFeasibility bật cảnh báo (HasWarning)
    // KHI VÀ CHỈ KHI tổng giờ ước lượng vượt quá tổng giờ khả dụng (TargetDays × studyHoursPerDay).
    [Property(MaxTest = 100)]
    public Property AssessFeasibility_HasWarning_IFF_EstimatedExceedsAvailable()
    {
        return Prop.ForAll(Arb.From(PlanGen), tuple =>
        {
            var (path, studyHoursPerDay) = tuple;

            var result = PathBuilder.AssessFeasibility(path, studyHoursPerDay);

            // Oracle: số giờ khả dụng được clamp không âm; cảnh báo bật khi ước lượng > khả dụng.
            var availableHours = path.TargetDays * studyHoursPerDay;
            var expected = path.TotalEstimatedHours > availableHours;

            return (result.HasWarning == expected)
                .Label($"estimated={result.EstimatedHours} available={result.AvailableHours} " +
                       $"hasWarning={result.HasWarning} expected={expected}");
        });
    }
}
