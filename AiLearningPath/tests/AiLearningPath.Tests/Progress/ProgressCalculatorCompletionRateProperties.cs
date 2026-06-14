using AiLearningPath.Domain.Progress;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Progress;

/// <summary>
/// Property-based tests cho <see cref="ProgressCalculator.ComputeCompletionRate"/> —
/// tỷ lệ hoàn thành kế hoạch luôn nằm trong [0, 1] (R8.1, R8.2).
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class ProgressCalculatorCompletionRateProperties
{
    // Feature: ai-learning-path, Property 12: Tỷ lệ hoàn thành luôn nằm trong [0, 1]
    // Với mọi số nhiệm vụ hoàn thành (completedTasks) và tổng số nhiệm vụ (totalTasks),
    // ComputeCompletionRate luôn trả về giá trị trong khoảng [0, 1], kể cả với đầu vào
    // bất thường (âm, completedTasks > totalTasks, totalTasks = 0).
    [Property(MaxTest = 100)]
    public Property ComputeCompletionRate_IsAlwaysWithin_ZeroToOne(int completedTasks, int totalTasks)
    {
        var rate = ProgressCalculator.ComputeCompletionRate(completedTasks, totalTasks);

        return (rate >= 0d && rate <= 1d)
            .Label($"completed={completedTasks} total={totalTasks} rate={rate}");
    }
}
