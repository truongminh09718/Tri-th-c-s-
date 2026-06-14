using System.Globalization;
using AiLearningPath.Domain.Assessments;

namespace AiLearningPath.Domain.LearningDna;

/// <summary>
/// Logic thuần xây dựng và cập nhật Learning DNA Profile (R6.1, R6.3).
/// Không phụ thuộc I/O nên có thể kiểm thử bằng property-based testing.
/// </summary>
/// <remarks>
/// Mọi hàm trong lớp này đều có tính xác định (deterministic): cùng một đầu vào
/// luôn cho ra cùng một kết quả. Tập điểm mạnh và tập điểm yếu của kết quả luôn
/// tách biệt (disjoint).
/// </remarks>
public static class DnaBuilder
{
    /// <summary>Hệ số tốc độ tiếp thu tối thiểu.</summary>
    public const double MinLearningSpeed = 0.5d;

    /// <summary>Hệ số tốc độ tiếp thu tối đa.</summary>
    public const double MaxLearningSpeed = 2.0d;

    /// <summary>Độ dài mỗi khối giờ học hiệu quả (giờ).</summary>
    private const int BlockHours = 2;

    /// <summary>Trọng số áp cho dữ liệu tiến độ mới khi hợp nhất (exponential smoothing).</summary>
    private const double NewDataWeight = 0.3d;

    /// <summary>
    /// Tạo Learning DNA Profile từ kết quả đánh giá và sở thích hồ sơ (R6.1).
    /// Suy ra phong cách học, khung giờ học hiệu quả, tốc độ tiếp thu, khả năng
    /// tập trung và điểm mạnh/yếu một cách xác định.
    /// </summary>
    /// <param name="result">Kết quả chấm bài đánh giá năng lực.</param>
    /// <param name="profile">Sở thích/thiết lập hồ sơ của sinh viên.</param>
    /// <returns>Hồ sơ Learning DNA suy ra.</returns>
    public static LearningDnaResult Build(AssessmentGradingResult result, DnaProfileInput profile)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(profile);

        var hours = ClampStudyHours(profile.StudyHoursPerDay);
        var effectiveHours = BuildEffectiveHours(profile.PreferredStudyPeriod, hours);
        var learningStyle = InferLearningStyle(result.Score);
        var learningSpeed = ScoreToLearningSpeed(result.Score);
        var focusAbility = ScoreToFocusAbility(result.Score);

        // Điểm mạnh/yếu lấy trực tiếp từ kết quả đánh giá, đảm bảo tách biệt và sắp xếp ổn định.
        var (strengths, weaknesses) = Reconcile(result.Strengths, result.Weaknesses, preferStrengthOnConflict: true);

        return new LearningDnaResult(
            learningStyle,
            effectiveHours,
            learningSpeed,
            focusAbility,
            strengths,
            weaknesses);
    }

    /// <summary>
    /// Cập nhật Learning DNA Profile hiện tại từ dữ liệu tiến độ mới (R6.3).
    /// Hợp nhất khả năng tập trung và tốc độ tiếp thu bằng làm trơn (smoothing),
    /// đồng thời chuyển kỹ năng tiến bộ thành điểm mạnh và kỹ năng còn yếu thành điểm yếu.
    /// </summary>
    /// <param name="current">Hồ sơ Learning DNA hiện tại.</param>
    /// <param name="newData">Dữ liệu tiến độ mới được ghi nhận.</param>
    /// <returns>Hồ sơ Learning DNA đã cập nhật.</returns>
    public static LearningDnaResult Merge(LearningDnaResult current, ProgressData newData)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(newData);

        // Cập nhật khả năng tập trung: chuẩn hóa thời lượng tập trung trung bình theo
        // mốc 60 phút rồi làm trơn với giá trị hiện tại.
        var normalizedFocus = Clamp01(NonNegative(newData.AverageFocusMinutes) / 60d);
        var focusAbility = Clamp01(Smooth(current.FocusAbility, normalizedFocus));

        // Cập nhật tốc độ tiếp thu theo tỷ lệ hoàn thành gần đây (ánh xạ [0,1] -> [0.5,2.0]).
        var speedFromProgress = RatioToLearningSpeed(Clamp01(newData.CompletionRate));
        var learningSpeed = ClampSpeed(Smooth(current.LearningSpeed, speedFromProgress));

        // Hợp nhất điểm mạnh/yếu: thêm kỹ năng tiến bộ vào điểm mạnh, kỹ năng còn yếu vào điểm yếu.
        var improved = newData.ImprovedSkills ?? Array.Empty<string>();
        var struggled = newData.StruggledSkills ?? Array.Empty<string>();

        var mergedStrengths = current.Strengths.Concat(improved);
        var mergedWeaknesses = current.Weaknesses.Concat(struggled);

        // Khi xung đột (một kỹ năng vừa tiến bộ vừa còn yếu) thì loại khỏi cả hai để giữ tách biệt.
        var conflicts = new HashSet<string>(improved, StringComparer.Ordinal);
        conflicts.IntersectWith(new HashSet<string>(struggled, StringComparer.Ordinal));

        var (strengths, weaknesses) = Reconcile(
            mergedStrengths,
            mergedWeaknesses,
            preferStrengthOnConflict: true,
            drop: conflicts);

        return new LearningDnaResult(
            current.LearningStyle,
            current.EffectiveHours,
            learningSpeed,
            focusAbility,
            strengths,
            weaknesses);
    }

    private static double Smooth(double currentValue, double newValue)
        => (currentValue * (1d - NewDataWeight)) + (newValue * NewDataWeight);

    /// <summary>
    /// Chuẩn hóa điểm mạnh/yếu: loại trùng lặp, đảm bảo hai tập tách biệt và sắp xếp
    /// ổn định theo thứ tự ordinal. Khi một kỹ năng xuất hiện ở cả hai tập, ưu tiên
    /// theo <paramref name="preferStrengthOnConflict"/>. Các kỹ năng trong
    /// <paramref name="drop"/> bị loại khỏi cả hai tập.
    /// </summary>
    private static (IReadOnlyList<string> Strengths, IReadOnlyList<string> Weaknesses) Reconcile(
        IEnumerable<string> rawStrengths,
        IEnumerable<string> rawWeaknesses,
        bool preferStrengthOnConflict,
        ISet<string>? drop = null)
    {
        var strengthSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var s in rawStrengths)
        {
            if (!string.IsNullOrEmpty(s) && (drop is null || !drop.Contains(s)))
            {
                strengthSet.Add(s);
            }
        }

        var weaknessSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var w in rawWeaknesses)
        {
            if (!string.IsNullOrEmpty(w) && (drop is null || !drop.Contains(w)))
            {
                weaknessSet.Add(w);
            }
        }

        // Giải quyết phần giao để hai tập tách biệt.
        if (preferStrengthOnConflict)
        {
            weaknessSet.ExceptWith(strengthSet);
        }
        else
        {
            strengthSet.ExceptWith(weaknessSet);
        }

        var strengths = strengthSet.OrderBy(x => x, StringComparer.Ordinal).ToList();
        var weaknesses = weaknessSet.OrderBy(x => x, StringComparer.Ordinal).ToList();
        return (strengths, weaknesses);
    }

    private static IReadOnlyList<string> BuildEffectiveHours(string? preferredPeriod, double hours)
    {
        var startHour = PeriodStartHour(preferredPeriod);
        var blockCount = (int)Math.Ceiling(hours / BlockHours);
        if (blockCount <= 0)
        {
            return Array.Empty<string>();
        }

        var blocks = new List<string>(blockCount);
        for (var i = 0; i < blockCount; i++)
        {
            var start = (startHour + (i * BlockHours)) % 24;
            var end = (start + BlockHours) % 24;
            blocks.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"{start:D2}:00-{end:D2}:00"));
        }

        return blocks;
    }

    private static int PeriodStartHour(string? preferredPeriod)
        => preferredPeriod switch
        {
            "Morning" => 6,
            "Afternoon" => 13,
            "Night" => 21,
            _ => 18, // Evening là mặc định.
        };

    private static string InferLearningStyle(double score)
    {
        // Ánh xạ xác định từ điểm tổng sang phong cách học chủ đạo.
        if (score >= 80d)
        {
            return "ReadingWriting";
        }

        if (score >= 60d)
        {
            return "Visual";
        }

        if (score >= 40d)
        {
            return "Auditory";
        }

        return "Kinesthetic";
    }

    private static double ScoreToLearningSpeed(double score)
        => RatioToLearningSpeed(Clamp01(score / 100d));

    private static double RatioToLearningSpeed(double ratio)
        => ClampSpeed(MinLearningSpeed + (ratio * (MaxLearningSpeed - MinLearningSpeed)));

    private static double ScoreToFocusAbility(double score)
        => Clamp01(score / 100d);

    private static double ClampStudyHours(double hours)
    {
        if (double.IsNaN(hours))
        {
            return 0d;
        }

        return Math.Clamp(hours, 0d, 24d);
    }

    private static double ClampSpeed(double value)
        => double.IsNaN(value) ? MinLearningSpeed : Math.Clamp(value, MinLearningSpeed, MaxLearningSpeed);

    private static double Clamp01(double value)
        => double.IsNaN(value) ? 0d : Math.Clamp(value, 0d, 1d);

    private static double NonNegative(double value)
        => double.IsNaN(value) || value < 0d ? 0d : value;
}
