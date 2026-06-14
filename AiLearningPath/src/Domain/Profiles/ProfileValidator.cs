using AiLearningPath.Domain.Common;

namespace AiLearningPath.Domain.Profiles;

/// <summary>
/// Logic thuần kiểm tra hợp lệ cho hồ sơ cá nhân và lựa chọn Learning Goal.
/// Không phụ thuộc I/O nên có thể kiểm thử bằng property-based testing.
/// </summary>
public static class ProfileValidator
{
    /// <summary>Số giờ học mỗi ngày tối thiểu hợp lệ.</summary>
    public const double MinStudyHours = 0d;

    /// <summary>Số giờ học mỗi ngày tối đa hợp lệ.</summary>
    public const double MaxStudyHours = 24d;

    /// <summary>
    /// Kiểm tra số giờ học mỗi ngày (R3.4). Chỉ hợp lệ khi <c>0 &lt;= h &lt;= 24</c>.
    /// Giá trị NaN hoặc vô cực luôn bị từ chối.
    /// </summary>
    public static ValidationResult ValidateStudyHours(double hours)
    {
        if (double.IsNaN(hours) || double.IsInfinity(hours) ||
            hours < MinStudyHours || hours > MaxStudyHours)
        {
            return ValidationResult.Failure(
                $"Số giờ học mỗi ngày phải nằm trong khoảng [{MinStudyHours}, {MaxStudyHours}].");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Kiểm tra Learning Goal (R4.4). Chỉ hợp lệ khi goal thuộc danh mục
    /// <see cref="LearningGoalCatalog.Supported"/>.
    /// </summary>
    public static ValidationResult ValidateGoal(string goal)
    {
        if (string.IsNullOrEmpty(goal) || !LearningGoalCatalog.Supported.Contains(goal))
        {
            return ValidationResult.Failure(
                "Mục tiêu học tập không nằm trong danh sách được hỗ trợ.");
        }

        return ValidationResult.Success();
    }
}
