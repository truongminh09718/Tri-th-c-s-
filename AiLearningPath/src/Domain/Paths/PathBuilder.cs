using AiLearningPath.Domain.Assessments;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.LearningDna;

namespace AiLearningPath.Domain.Paths;

/// <summary>
/// Logic thuần sinh lộ trình học tập cá nhân hóa (R7). Không phụ thuộc I/O nên có
/// thể kiểm thử bằng property-based testing.
/// </summary>
/// <remarks>
/// <para>Lớp này gồm ba phép toán thuần:</para>
/// <list type="bullet">
/// <item><see cref="CheckPrerequisites"/> — kiểm tra điều kiện tiên quyết trước khi sinh lộ trình (R7.4).</item>
/// <item><see cref="Build"/> — xây cấu trúc lộ trình tháng → tuần → ngày, bao phủ
/// toàn bộ <c>targetDays</c> mà không trùng/hở (R7.1, R7.2).</item>
/// <item><see cref="AssessFeasibility"/> — gắn cảnh báo khi tổng giờ ước lượng vượt
/// giờ khả dụng (R7.5).</item>
/// </list>
/// <para>Mọi hàm đều có tính xác định (deterministic): cùng đầu vào luôn cho cùng kết quả.</para>
/// </remarks>
public static class PathBuilder
{
    /// <summary>Số ngày trong một tháng dùng để phân cấp lộ trình.</summary>
    public const int DaysPerMonth = 30;

    /// <summary>Số ngày trong một tuần dùng để phân cấp lộ trình.</summary>
    public const int DaysPerWeek = 7;

    /// <summary>Lĩnh vực kỹ năng mặc định khi không suy ra được kỹ năng nào từ đầu vào.</summary>
    public const string DefaultSkill = "General";

    /// <summary>
    /// Kiểm tra điều kiện tiên quyết để sinh lộ trình (R7.4): sinh viên phải đã hoàn
    /// thành AI Assessment (tức ngữ cảnh có <see cref="StudentContext.HasAssessmentResult"/>).
    /// </summary>
    /// <param name="context">Ngữ cảnh tiên quyết của sinh viên.</param>
    /// <returns>
    /// Hợp lệ khi và chỉ khi ngữ cảnh đã có một <c>AssessmentResult</c>; ngược lại
    /// trả về kết quả không hợp lệ kèm thông báo yêu cầu hoàn thành đánh giá.
    /// </returns>
    public static ValidationResult CheckPrerequisites(StudentContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.HasAssessmentResult)
        {
            return ValidationResult.Failure(
                "Vui lòng hoàn thành bài đánh giá năng lực trước khi sinh lộ trình.");
        }

        return ValidationResult.Success();
    }

    /// <summary>
    /// Xây cấu trúc lộ trình học tập theo phân cấp tháng → tuần → ngày, bao phủ đúng
    /// và đủ <paramref name="targetDays"/> ngày mà không trùng lặp hay bỏ sót (R7.1, R7.2).
    /// </summary>
    /// <remarks>
    /// Mỗi ngày được gán một số thứ tự tuyệt đối (1-based) duy nhất trong khoảng
    /// <c>[1, targetDays]</c>; các ngày được gom vào giai đoạn theo công thức xác định
    /// dựa trên <see cref="DaysPerMonth"/> và <see cref="DaysPerWeek"/>, nên tập số thứ
    /// tự ngày của toàn bộ lộ trình luôn đúng bằng <c>{1, …, targetDays}</c>.
    /// Nhiệm vụ mỗi ngày luân phiên qua danh sách kỹ năng ưu tiên (điểm yếu trước,
    /// rồi điểm mạnh), đảm bảo tính cá nhân hóa và xác định.
    /// </remarks>
    /// <param name="learningGoal">Mục tiêu học tập của lộ trình.</param>
    /// <param name="result">Kết quả chấm bài đánh giá (cung cấp điểm mạnh/yếu).</param>
    /// <param name="dna">Hồ sơ Learning DNA (cung cấp thêm điểm mạnh/yếu cá nhân hóa).</param>
    /// <param name="targetDays">Số ngày mục tiêu mà lộ trình phải bao phủ (phải >= 1).</param>
    /// <param name="dailyEstimatedHours">Số giờ học ước lượng cho nhiệm vụ mỗi ngày (không âm).</param>
    /// <param name="aiContent">
    /// Nội dung do AI sinh ra dùng làm mô tả nhiệm vụ. Có thể null/rỗng; khi đó mô tả
    /// được sinh tự động theo kỹ năng.
    /// </param>
    /// <returns>Lộ trình thuần (<see cref="PathPlan"/>) bao phủ toàn bộ targetDays.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Khi <paramref name="targetDays"/> nhỏ hơn 1.</exception>
    public static PathPlan Build(
        string learningGoal,
        AssessmentGradingResult result,
        LearningDnaResult dna,
        int targetDays,
        double dailyEstimatedHours,
        string? aiContent = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(dna);
        ArgumentOutOfRangeException.ThrowIfLessThan(targetDays, 1);

        var goal = learningGoal ?? string.Empty;
        var estimatedHours = NonNegative(dailyEstimatedHours);
        var skills = BuildSkillRotation(result, dna);
        var description = string.IsNullOrWhiteSpace(aiContent) ? null : aiContent;

        // Gom các ngày liên tiếp cùng (tháng, tuần) thành một giai đoạn, giữ thứ tự tăng dần.
        var phases = new List<PathPhasePlan>();
        var currentDays = new List<PathDayPlan>();
        var currentMonth = 0;
        var currentWeek = 0;

        for (var day = 1; day <= targetDays; day++)
        {
            var monthIndex = ((day - 1) / DaysPerMonth) + 1;
            var dayWithinMonth = (day - 1) % DaysPerMonth;
            var weekIndex = (dayWithinMonth / DaysPerWeek) + 1;

            if (currentDays.Count > 0 && (monthIndex != currentMonth || weekIndex != currentWeek))
            {
                phases.Add(CreatePhase(currentMonth, currentWeek, currentDays));
                currentDays = new List<PathDayPlan>();
            }

            currentMonth = monthIndex;
            currentWeek = weekIndex;

            var skill = skills[(day - 1) % skills.Count];
            var taskDescription = description ?? $"Luyện tập {skill} (ngày {day}).";
            currentDays.Add(new PathDayPlan(day, new PathTaskPlan(skill, taskDescription, estimatedHours)));
        }

        if (currentDays.Count > 0)
        {
            phases.Add(CreatePhase(currentMonth, currentWeek, currentDays));
        }

        return new PathPlan(goal, targetDays, phases);
    }

    /// <summary>
    /// Ước lượng tính khả thi của lộ trình theo khoảng thời gian mục tiêu (R7.5).
    /// Gắn cảnh báo khi và chỉ khi tổng số giờ học ước lượng của lộ trình vượt quá
    /// tổng số giờ khả dụng trong <c>targetDays</c> ngày.
    /// </summary>
    /// <remarks>
    /// Tổng số giờ khả dụng = <c>path.TargetDays × studyHoursPerDay</c>. Cảnh báo được
    /// bật khi <c>EstimatedHours &gt; AvailableHours</c>.
    /// </remarks>
    /// <param name="path">Lộ trình cần đánh giá.</param>
    /// <param name="studyHoursPerDay">Số giờ học khả dụng mỗi ngày của sinh viên (không âm).</param>
    /// <returns>Kết quả ước lượng kèm cờ cảnh báo, tổng giờ ước lượng và tổng giờ khả dụng.</returns>
    public static FeasibilityResult AssessFeasibility(PathPlan path, double studyHoursPerDay)
    {
        ArgumentNullException.ThrowIfNull(path);

        var availableHours = path.TargetDays * NonNegative(studyHoursPerDay);
        var estimatedHours = path.TotalEstimatedHours;
        var hasWarning = estimatedHours > availableHours;

        return new FeasibilityResult(hasWarning, estimatedHours, availableHours);
    }

    private static PathPhasePlan CreatePhase(int monthIndex, int weekIndex, List<PathDayPlan> days)
        => new(
            monthIndex,
            weekIndex,
            $"Tháng {monthIndex} - Tuần {weekIndex}",
            days);

    /// <summary>
    /// Dựng danh sách kỹ năng luân phiên cho nhiệm vụ hằng ngày: ưu tiên điểm yếu
    /// (cần luyện tập nhiều hơn) rồi đến điểm mạnh, loại trùng theo thứ tự ordinal.
    /// Luôn trả về ít nhất một phần tử (<see cref="DefaultSkill"/> khi không có dữ liệu).
    /// </summary>
    private static IReadOnlyList<string> BuildSkillRotation(
        AssessmentGradingResult result,
        LearningDnaResult dna)
    {
        var ordered = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(IEnumerable<string>? skills)
        {
            if (skills is null)
            {
                return;
            }

            foreach (var skill in skills)
            {
                if (!string.IsNullOrWhiteSpace(skill) && seen.Add(skill))
                {
                    ordered.Add(skill);
                }
            }
        }

        Add(result.Weaknesses);
        Add(dna.Weaknesses);
        Add(result.Strengths);
        Add(dna.Strengths);

        if (ordered.Count == 0)
        {
            ordered.Add(DefaultSkill);
        }

        return ordered;
    }

    private static double NonNegative(double value)
        => double.IsNaN(value) || value < 0d ? 0d : value;
}
