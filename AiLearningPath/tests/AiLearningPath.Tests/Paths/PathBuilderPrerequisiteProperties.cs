using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Paths;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Paths;

/// <summary>
/// Property-based tests cho <see cref="PathBuilder.CheckPrerequisites"/> (điều kiện
/// tiên quyết để sinh lộ trình: sinh viên phải đã hoàn thành AI Assessment — R7.4).
/// Validates: Requirements 7.4
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class PathBuilderPrerequisiteProperties
{
    // Feature: ai-learning-path, Property 9: Sinh lộ trình yêu cầu hoàn thành đánh giá
    // Với mọi ngữ cảnh sinh viên bất kỳ: CheckPrerequisites hợp lệ KHI VÀ CHỈ KHI
    // ngữ cảnh đã có kết quả đánh giá (HasAssessmentResult == true).
    [Property(MaxTest = 100)]
    public Property CheckPrerequisites_IsValid_IffHasAssessmentResult(bool hasLearningGoal, bool hasAssessmentResult)
    {
        var context = new StudentContext(hasLearningGoal, hasAssessmentResult);

        bool actual = PathBuilder.CheckPrerequisites(context).IsValid;
        bool expected = hasAssessmentResult;

        return (actual == expected)
            .Label($"hasLearningGoal={hasLearningGoal} hasAssessmentResult={hasAssessmentResult} actual={actual} expected={expected}");
    }
}
