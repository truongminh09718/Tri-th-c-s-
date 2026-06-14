using AiLearningPath.Domain.Profiles;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Profiles;

/// <summary>
/// Property-based tests cho <see cref="ProfileValidator.ValidateGoal"/> và
/// <see cref="LearningGoalCatalog"/> (chỉ chấp nhận Learning Goal được hỗ trợ).
/// Validates: Requirements 4.1, 4.2, 4.4
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class ProfileValidatorGoalProperties
{
    /// <summary>Sinh một goal được hỗ trợ bất kỳ từ danh mục.</summary>
    private static Gen<string> SupportedGoalGen =>
        Gen.Elements<string>(LearningGoalCatalog.Supported);

    // Feature: ai-learning-path, Property 5: Chỉ chấp nhận Learning Goal được hỗ trợ
    // Với mọi chuỗi goal bất kỳ: ValidateGoal hợp lệ KHI VÀ CHỈ KHI goal thuộc danh mục Supported.
    [Property(MaxTest = 100)]
    public Property ValidateGoal_IsValid_IffGoalIsSupported(NonNull<string> goal)
    {
        string value = goal.Get;
        bool actual = ProfileValidator.ValidateGoal(value).IsValid;
        bool expected = LearningGoalCatalog.Supported.Contains(value);

        return (actual == expected)
            .Label($"goal=\"{value}\" actual={actual} expected={expected}");
    }

    // Feature: ai-learning-path, Property 5: Chỉ chấp nhận Learning Goal được hỗ trợ
    // (a) Mọi goal thuộc danh mục Supported đều được chấp nhận (R4.1, R4.2).
    [Property(MaxTest = 100)]
    public Property ValidateGoal_AcceptsAllSupportedGoals()
    {
        return Prop.ForAll(Arb.From(SupportedGoalGen), goal =>
        {
            bool actual = ProfileValidator.ValidateGoal(goal).IsValid;
            return actual.Label($"supported goal=\"{goal}\" expected valid but was invalid");
        });
    }

    // Feature: ai-learning-path, Property 5: Chỉ chấp nhận Learning Goal được hỗ trợ
    // (b) Goal không thuộc danh mục (bao gồm chuỗi rỗng) luôn bị từ chối (R4.4).
    [Property(MaxTest = 100)]
    public Property ValidateGoal_RejectsUnsupportedGoals(NonNull<string> goal)
    {
        string value = goal.Get;
        return (!ProfileValidator.ValidateGoal(value).IsValid)
            .When(!LearningGoalCatalog.Supported.Contains(value))
            .Label($"unsupported goal=\"{value}\" expected invalid but was valid");
    }

    // Feature: ai-learning-path, Property 5: Chỉ chấp nhận Learning Goal được hỗ trợ
    // (c) Goal null hoặc rỗng luôn bị từ chối (R4.4).
    [Property(MaxTest = 100)]
    public bool ValidateGoal_RejectsNullOrEmpty(bool useNull)
    {
        string? value = useNull ? null : string.Empty;
        return !ProfileValidator.ValidateGoal(value!).IsValid;
    }
}
