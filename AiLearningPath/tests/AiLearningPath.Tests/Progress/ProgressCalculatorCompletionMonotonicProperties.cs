using AiLearningPath.Domain.Progress;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Progress;

/// <summary>
/// Property-based tests xác nhận: đánh dấu một nhiệm vụ hoàn thành không bao giờ làm
/// giảm tỷ lệ hoàn thành kế hoạch (R8.2). Hoàn thành thêm một nhiệm vụ làm tăng
/// <c>completedTasks</c> thêm 1 (tổng số nhiệm vụ không đổi), nên tỷ lệ chỉ giữ nguyên
/// hoặc tăng. Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class ProgressCalculatorCompletionMonotonicProperties
{
    /// <summary>
    /// Sinh trạng thái tiến độ hợp lệ: tổng số nhiệm vụ >= 1 và số nhiệm vụ đã hoàn
    /// thành nằm trong [0, totalTasks]. Đây là không gian đầu vào thực tế của một lộ
    /// trình học có ít nhất một nhiệm vụ.
    /// </summary>
    private static Arbitrary<(int Completed, int Total)> ProgressStateArb()
    {
        var gen =
            from total in Gen.Choose(1, 1000)
            from completed in Gen.Choose(0, total)
            select (completed, total);

        return Arb.From(gen);
    }

    // Feature: ai-learning-path, Property 15: Hoàn thành nhiệm vụ làm tỷ lệ hoàn thành không giảm
    // Với mọi trạng thái tiến độ hợp lệ (0 <= completed <= total, total >= 1), việc đánh
    // dấu thêm một nhiệm vụ hoàn thành (completed + 1, nhưng không vượt quá total) khiến
    // tỷ lệ hoàn thành mới luôn lớn hơn hoặc bằng tỷ lệ cũ — không bao giờ giảm.
    [Property(MaxTest = 100)]
    public Property CompletingTask_NeverDecreasesCompletionRate()
    {
        return Prop.ForAll(ProgressStateArb(), state =>
        {
            var (completed, total) = state;

            var rateBefore = ProgressCalculator.ComputeCompletionRate(completed, total);

            // Hoàn thành thêm một nhiệm vụ: số hoàn thành tăng 1 nhưng không vượt tổng.
            var completedAfter = Math.Min(completed + 1, total);
            var rateAfter = ProgressCalculator.ComputeCompletionRate(completedAfter, total);

            return (rateAfter >= rateBefore)
                .Label($"completed={completed} total={total} before={rateBefore} after={rateAfter}");
        });
    }
}
