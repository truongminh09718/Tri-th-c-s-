using AiLearningPath.Domain.Profiles;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests.Profiles;

/// <summary>
/// Property-based tests cho <see cref="ProfileValidator.ValidateStudyHours"/>.
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class ProfileValidatorStudyHoursProperties
{
    // Feature: ai-learning-path, Property 4: Validation số giờ học mỗi ngày
    [Property(MaxTest = 100)]
    public Property ValidateStudyHours_IsValid_IFF_WithinRange(double hours)
    {
        bool actual = ProfileValidator.ValidateStudyHours(hours).IsValid;

        // Kỳ vọng: hợp lệ khi và chỉ khi 0 <= h <= 24.
        // Với NaN/Infinity, biểu thức so sánh dưới đây luôn là false nên hai trường hợp
        // này được kỳ vọng bị từ chối — đúng với hành vi mong muốn.
        bool expected = hours >= ProfileValidator.MinStudyHours
                        && hours <= ProfileValidator.MaxStudyHours;

        return (actual == expected)
            .Label($"hours={hours} expected valid={expected} but was {actual}");
    }

    // Feature: ai-learning-path, Property 4: Validation số giờ học mỗi ngày
    // Thuộc tính bổ sung: NaN và vô cực luôn bị từ chối.
    [Property(MaxTest = 100)]
    public Property ValidateStudyHours_Rejects_NaN_And_Infinity()
    {
        var specialValues = new[]
        {
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity,
        };

        return Prop.ForAll(Arb.From(Gen.Elements(specialValues)), value =>
        {
            bool isValid = ProfileValidator.ValidateStudyHours(value).IsValid;
            return (!isValid).Label($"value={value} expected invalid but was valid");
        });
    }
}
