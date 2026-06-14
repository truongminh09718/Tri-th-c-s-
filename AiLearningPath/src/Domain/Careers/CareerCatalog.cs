namespace AiLearningPath.Domain.Careers;

/// <summary>
/// Danh mục các nghề nghiệp được Career Path AI hỗ trợ (R10.1). Đây là logic thuần,
/// không phụ thuộc I/O: cùng một truy vấn luôn cho cùng một kết quả.
/// </summary>
public static class CareerCatalog
{
    /// <summary>
    /// Danh sách nghề nghiệp được hỗ trợ: Frontend, Backend, Data Analyst, AI Engineer,
    /// Tester (R10.1). Thứ tự ổn định để hiển thị nhất quán cho client.
    /// </summary>
    public static readonly IReadOnlyList<string> Careers = new[]
    {
        "Frontend",
        "Backend",
        "DataAnalyst",
        "AIEngineer",
        "Tester",
    };

    private static readonly IReadOnlySet<string> Supported =
        new HashSet<string>(Careers, StringComparer.Ordinal);

    /// <summary>
    /// Trả về true nếu <paramref name="career"/> là một nghề nghiệp được hỗ trợ.
    /// Trả về false cho giá trị null/rỗng hoặc không thuộc danh mục.
    /// </summary>
    public static bool IsSupported(string? career)
    {
        if (string.IsNullOrEmpty(career))
        {
            return false;
        }

        return Supported.Contains(career);
    }
}
