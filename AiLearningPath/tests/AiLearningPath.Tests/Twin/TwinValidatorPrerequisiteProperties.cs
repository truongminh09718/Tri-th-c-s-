using AiLearningPath.Domain.Twin;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Twin;

/// <summary>
/// Property-based tests cho <see cref="TwinValidator.CheckPrerequisites"/> (điều kiện
/// tiên quyết để mô phỏng AI Academic Twin: sinh viên phải có cả Learning Goal lẫn
/// kết quả đánh giá năng lực — R9.4).
/// Validates: Requirements 9.4
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class TwinValidatorPrerequisiteProperties
{
    /// <summary>
    /// Sinh một Learning Goal: hoặc null/rỗng (chưa chọn), hoặc một chuỗi không rỗng (đã chọn).
    /// Bao phủ đầy đủ không gian đầu vào liên quan đến trạng thái có/không có Learning Goal.
    /// </summary>
    private static Gen<string?> LearningGoalGen =>
        Gen.OneOf(
            Gen.Constant<string?>(null),
            Gen.Constant<string?>(string.Empty),
            Arb.Generate<NonEmptyString>().Select(s => (string?)s.Get));

    // Feature: ai-learning-path, Property 16: Mô phỏng Academic Twin yêu cầu tiên quyết
    // Với mọi ngữ cảnh sinh viên bất kỳ: CheckPrerequisites hợp lệ KHI VÀ CHỈ KHI
    // ngữ cảnh có cả Learning Goal (HasLearningGoal == true) lẫn kết quả đánh giá
    // (HasAssessmentResult == true).
    [Property(MaxTest = 100)]
    public Property CheckPrerequisites_IsValid_IffHasLearningGoalAndAssessmentResult(bool hasAssessmentResult)
    {
        return Prop.ForAll(Arb.From(LearningGoalGen), learningGoal =>
        {
            var context = new StudentContext(learningGoal, hasAssessmentResult);

            bool actual = TwinValidator.CheckPrerequisites(context).IsValid;
            bool expected = context.HasLearningGoal && hasAssessmentResult;

            return (actual == expected)
                .Label($"learningGoal=\"{learningGoal ?? "<null>"}\" hasAssessmentResult={hasAssessmentResult} actual={actual} expected={expected}");
        });
    }
}
