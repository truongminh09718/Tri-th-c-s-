using System.Globalization;

namespace AiLearningPath.Domain.Progress;

/// <summary>
/// Logic thuần tổng hợp tiến độ học tập cho Progress Dashboard (R8.1, R8.2).
/// Không phụ thuộc I/O nên có thể kiểm thử bằng property-based testing.
/// </summary>
/// <remarks>
/// <para>Lớp này gồm các phép toán thuần:</para>
/// <list type="bullet">
/// <item><see cref="ComputeCompletionRate"/> — tỷ lệ hoàn thành luôn trong [0, 1].</item>
/// <item><see cref="ComputeLearningScore"/> — Learning Score luôn trong [0, 100].</item>
/// <item><see cref="ComputeTotalStudyHours"/> — tổng số giờ học, luôn không âm.</item>
/// <item><see cref="BuildChartData"/> — dựng dữ liệu biểu đồ tổng hợp theo tuần/tháng.</item>
/// </list>
/// <para>Mọi hàm đều có tính xác định (deterministic) và xử lý phòng vệ với các giá
/// trị bất thường (NaN, Infinity, âm, chia cho 0).</para>
/// </remarks>
public static class ProgressCalculator
{
    /// <summary>Trọng số của tỷ lệ hoàn thành trong công thức Learning Score.</summary>
    public const double CompletionWeight = 0.5d;

    /// <summary>Trọng số của điểm đánh giá trong công thức Learning Score.</summary>
    public const double AssessmentWeight = 0.3d;

    /// <summary>Trọng số của mức độ đều đặn khi học trong công thức Learning Score.</summary>
    public const double ConsistencyWeight = 0.2d;

    /// <summary>
    /// Tính tỷ lệ hoàn thành kế hoạch = số nhiệm vụ đã hoàn thành / tổng số nhiệm vụ
    /// (R8.1, R8.2). Luôn trả về giá trị trong khoảng [0, 1].
    /// </summary>
    /// <remarks>
    /// Khi <paramref name="totalTasks"/> bằng 0 (không có nhiệm vụ nào), trả về 0 để
    /// tránh chia cho 0. Số nhiệm vụ hoàn thành được kẹp vào [0, totalTasks] để kết
    /// quả luôn nằm trong [0, 1] kể cả với đầu vào bất thường.
    /// </remarks>
    /// <param name="completedTasks">Số nhiệm vụ đã hoàn thành (không âm).</param>
    /// <param name="totalTasks">Tổng số nhiệm vụ (không âm).</param>
    /// <returns>Tỷ lệ hoàn thành trong [0, 1].</returns>
    public static double ComputeCompletionRate(int completedTasks, int totalTasks)
    {
        if (totalTasks <= 0)
        {
            return 0d;
        }

        var completed = Math.Clamp(completedTasks, 0, totalTasks);
        return (double)completed / totalTasks;
    }

    /// <summary>
    /// Tính Learning Score tổng hợp từ các tín hiệu tiến độ (R8.1, R8.2). Kết hợp tỷ lệ
    /// hoàn thành, điểm đánh giá và mức độ đều đặn theo trọng số xác định, rồi quy về
    /// thang [0, 100]. Luôn trả về giá trị trong khoảng [0, 100].
    /// </summary>
    /// <remarks>
    /// Công thức: <c>(CompletionRate × 100 × <see cref="CompletionWeight"/>) +
    /// (AssessmentScore × <see cref="AssessmentWeight"/>) +
    /// (StudyConsistency × 100 × <see cref="ConsistencyWeight"/>)</c>.
    /// Các đầu vào được kẹp về khoảng kỳ vọng trước khi tính, và kết quả cuối được kẹp
    /// về [0, 100] nên luôn hợp lệ kể cả với đầu vào bất thường (NaN, Infinity, ngoài khoảng).
    /// </remarks>
    /// <param name="input">Tập tín hiệu tiến độ đầu vào.</param>
    /// <returns>Learning Score trong [0, 100].</returns>
    public static double ComputeLearningScore(LearningScoreInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var completion = Clamp01(input.CompletionRate);
        var assessment = Clamp(input.AssessmentScore, 0d, 100d);
        var consistency = Clamp01(input.StudyConsistency);

        var score = (completion * 100d * CompletionWeight)
            + (assessment * AssessmentWeight)
            + (consistency * 100d * ConsistencyWeight);

        return Clamp(score, 0d, 100d);
    }

    /// <summary>
    /// Tính tổng số giờ học từ danh sách phiên học (R8.1). Trả về tổng thời lượng của
    /// tất cả các phiên và luôn không âm.
    /// </summary>
    /// <remarks>
    /// Thời lượng âm hoặc không hợp lệ (NaN, Infinity) được xử lý phòng vệ như 0 nên
    /// kết quả không bao giờ âm.
    /// </remarks>
    /// <param name="sessions">Danh sách phiên học (có thể rỗng).</param>
    /// <returns>Tổng số giờ học, không âm.</returns>
    public static double ComputeTotalStudyHours(IReadOnlyList<StudySessionInput> sessions)
    {
        ArgumentNullException.ThrowIfNull(sessions);

        var total = 0d;
        foreach (var session in sessions)
        {
            if (session is null)
            {
                continue;
            }

            total += NonNegative(session.DurationHours);
        }

        return total;
    }

    /// <summary>
    /// Dựng dữ liệu biểu đồ tiến độ (R8.1, R8.3) bằng cách tổng hợp các mốc tiến độ
    /// theo tuần (ISO 8601) và theo tháng. Mỗi điểm biểu đồ là giá trị trung bình của
    /// Learning Score và tỷ lệ hoàn thành trong khoảng thời gian tương ứng.
    /// </summary>
    /// <remarks>
    /// Hàm có tính xác định: các điểm được sắp xếp tăng dần theo nhãn (ordinal). Mốc
    /// rỗng cho ra <see cref="ChartData.Empty"/>. Giá trị Learning Score và tỷ lệ hoàn
    /// thành được kẹp về khoảng hợp lệ trước khi lấy trung bình.
    /// </remarks>
    /// <param name="history">Danh sách mốc tiến độ (có thể rỗng).</param>
    /// <returns>Dữ liệu biểu đồ tổng hợp theo tuần và theo tháng.</returns>
    public static ChartData BuildChartData(IReadOnlyList<ProgressPoint> history)
    {
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count == 0)
        {
            return ChartData.Empty;
        }

        var weekly = Aggregate(history, WeeklyLabel);
        var monthly = Aggregate(history, MonthlyLabel);
        return new ChartData(weekly, monthly);
    }

    private static IReadOnlyList<ChartPoint> Aggregate(
        IReadOnlyList<ProgressPoint> history,
        Func<DateTime, string> labelSelector)
    {
        var scoreSums = new Dictionary<string, double>(StringComparer.Ordinal);
        var rateSums = new Dictionary<string, double>(StringComparer.Ordinal);
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var point in history)
        {
            if (point is null)
            {
                continue;
            }

            var label = labelSelector(point.RecordedAt);
            scoreSums[label] = scoreSums.GetValueOrDefault(label) + Clamp(point.LearningScore, 0d, 100d);
            rateSums[label] = rateSums.GetValueOrDefault(label) + Clamp01(point.CompletionRate);
            counts[label] = counts.GetValueOrDefault(label) + 1;
        }

        var points = new List<ChartPoint>(counts.Count);
        foreach (var label in counts.Keys.OrderBy(k => k, StringComparer.Ordinal))
        {
            var count = counts[label];
            points.Add(new ChartPoint(
                label,
                scoreSums[label] / count,
                rateSums[label] / count));
        }

        return points;
    }

    private static string WeeklyLabel(DateTime date)
    {
        var week = ISOWeek.GetWeekOfYear(date);
        var year = ISOWeek.GetYear(date);
        return string.Create(CultureInfo.InvariantCulture, $"{year:D4}-W{week:D2}");
    }

    private static string MonthlyLabel(DateTime date)
        => string.Create(CultureInfo.InvariantCulture, $"{date.Year:D4}-{date.Month:D2}");

    private static double NonNegative(double value)
        => double.IsNaN(value) || double.IsInfinity(value) || value < 0d ? 0d : value;

    private static double Clamp01(double value)
        => Clamp(value, 0d, 1d);

    private static double Clamp(double value, double min, double max)
    {
        if (double.IsNaN(value))
        {
            return min;
        }

        if (double.IsPositiveInfinity(value))
        {
            return max;
        }

        if (double.IsNegativeInfinity(value))
        {
            return min;
        }

        return Math.Clamp(value, min, max);
    }
}
