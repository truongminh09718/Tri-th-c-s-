namespace AiLearningPath.Domain.Profiles;

/// <summary>
/// Danh mục các Learning Goal được hệ thống hỗ trợ (R4.1) và quy tắc xác định
/// goal nào yêu cầu điểm số mục tiêu (R4.3). Đây là logic thuần, không phụ thuộc I/O.
/// </summary>
public static class LearningGoalCatalog
{
    /// <summary>
    /// Tập các Learning Goal được hỗ trợ: IELTS, TOEIC, môn đại học,
    /// Frontend Development, Backend Development, Data Analyst, AI Engineer (R4.1).
    /// </summary>
    public static readonly IReadOnlySet<string> Supported = new HashSet<string>(StringComparer.Ordinal)
    {
        "IELTS",
        "TOEIC",
        "UniversitySubject",
        "FrontendDevelopment",
        "BackendDevelopment",
        "DataAnalyst",
        "AIEngineer",
    };

    /// <summary>
    /// Các Learning Goal yêu cầu nhập điểm số mục tiêu (R4.3): IELTS, TOEIC.
    /// </summary>
    private static readonly IReadOnlySet<string> GoalsRequiringTargetScore = new HashSet<string>(StringComparer.Ordinal)
    {
        "IELTS",
        "TOEIC",
    };

    /// <summary>
    /// Trả về true nếu <paramref name="goal"/> là một goal được hỗ trợ và yêu cầu
    /// điểm số mục tiêu (ví dụ IELTS, TOEIC). Trả về false cho mọi giá trị khác,
    /// bao gồm cả goal không được hỗ trợ.
    /// </summary>
    public static bool RequiresTargetScore(string goal)
    {
        if (string.IsNullOrEmpty(goal))
        {
            return false;
        }

        return GoalsRequiringTargetScore.Contains(goal);
    }
}
