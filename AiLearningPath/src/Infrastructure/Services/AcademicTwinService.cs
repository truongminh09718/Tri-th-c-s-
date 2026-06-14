using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Twin;
using AiLearningPath.Domain.Twin;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai <see cref="IAcademicTwinService"/> trên nền EF Core (<see cref="AppDbContext"/>).
/// Điều phối nghiệp vụ mô phỏng Academic Twin (R9):
/// <list type="bullet">
///   <item>Kiểm tra tiên quyết bằng domain logic thuần
///   <see cref="TwinValidator.CheckPrerequisites"/> (R9.4): phải có Learning Goal và AssessmentResult.</item>
///   <item><see cref="SimulateAsync"/> dựng đặc trưng từ dữ liệu mới nhất của sinh viên (R9.3),
///   gọi <see cref="IPredictionService"/> (bọc ML Service) và kẹp kết quả về [0, 1] (R9.1).</item>
///   <item><see cref="SimulateRangeAsync"/> trả về đúng một dự đoán cho mỗi mức thời lượng và
///   đảm bảo xác suất đơn điệu không giảm theo thời lượng (R9.2).</item>
/// </list>
/// Dữ liệu dự đoán luôn được tính lại từ trạng thái mới nhất trong cơ sở dữ liệu (R9.3),
/// nên khi dữ liệu tiến độ cập nhật, lần mô phỏng kế tiếp tự động phản ánh dữ liệu mới.
/// Mọi thao tác đều gắn với <c>userId</c> để hỗ trợ ownership-based authorization (R14).
/// </summary>
public sealed class AcademicTwinService : IAcademicTwinService
{
    /// <summary>Số ngày mục tiêu mặc định khi sinh viên chưa có lộ trình học.</summary>
    private const int DefaultTargetDays = 90;

    private readonly AppDbContext _db;
    private readonly IPredictionService _predictionService;

    public AcademicTwinService(AppDbContext db, IPredictionService predictionService)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _predictionService = predictionService ?? throw new ArgumentNullException(nameof(predictionService));
    }

    /// <inheritdoc />
    public async Task<Prediction> SimulateAsync(
        Guid userId, double hoursPerDay, CancellationToken cancellationToken = default)
    {
        // R9.3: dựng ngữ cảnh dự đoán từ dữ liệu mới nhất của sinh viên.
        var context = await BuildPredictionContextAsync(userId, cancellationToken);

        var probability = await PredictAsync(context, hoursPerDay, cancellationToken);
        return new Prediction(hoursPerDay, probability);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Prediction>> SimulateRangeAsync(
        Guid userId, IReadOnlyList<double> hoursOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hoursOptions);

        // R9.3: dựng ngữ cảnh dự đoán một lần từ dữ liệu mới nhất, dùng chung cho mọi mức thời lượng.
        var context = await BuildPredictionContextAsync(userId, cancellationToken);

        if (hoursOptions.Count == 0)
        {
            return Array.Empty<Prediction>();
        }

        // Dự đoán xác suất thô cho từng mức thời lượng (giữ chỉ số gốc để trả về đúng thứ tự đầu vào).
        var raw = new double[hoursOptions.Count];
        for (var i = 0; i < hoursOptions.Count; i++)
        {
            raw[i] = await PredictAsync(context, hoursOptions[i], cancellationToken);
        }

        // R9.2 / Property 18: đảm bảo xác suất đơn điệu không giảm theo thời lượng học.
        // Sắp xếp theo thời lượng tăng dần, lấy running max rồi ánh xạ trở lại vị trí gốc.
        var order = Enumerable.Range(0, hoursOptions.Count)
            .OrderBy(i => hoursOptions[i])
            .ToArray();

        var adjusted = new double[hoursOptions.Count];
        var runningMax = double.NegativeInfinity;
        foreach (var i in order)
        {
            runningMax = Math.Max(runningMax, raw[i]);
            adjusted[i] = runningMax;
        }

        var predictions = new Prediction[hoursOptions.Count];
        for (var i = 0; i < hoursOptions.Count; i++)
        {
            predictions[i] = new Prediction(hoursOptions[i], adjusted[i]);
        }

        return predictions;
    }

    /// <summary>
    /// Dựng ngữ cảnh dự đoán từ dữ liệu mới nhất của sinh viên (R9.3) sau khi kiểm tra
    /// tiên quyết (R9.4). Bao gồm Learning Goal, điểm trình độ hiện tại, số ngày mục tiêu
    /// và các đặc trưng bổ sung (tốc độ học, khả năng tập trung) từ Learning DNA.
    /// </summary>
    /// <exception cref="TwinPrerequisitesException">Khi thiếu Learning Goal hoặc AssessmentResult (R9.4).</exception>
    private async Task<PredictionContext> BuildPredictionContextAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        // Kết quả đánh giá mới nhất của sinh viên (suy ra điểm trình độ hiện tại).
        var assessmentResult = await _db.AssessmentResults
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Score)
            .FirstOrDefaultAsync(cancellationToken);

        var learningGoal = profile?.LearningGoal;

        // R9.4: kiểm tra tiên quyết bằng domain logic thuần (cần Learning Goal + AssessmentResult).
        var ctx = new StudentContext(learningGoal, assessmentResult is not null);
        var prerequisites = TwinValidator.CheckPrerequisites(ctx);
        if (!prerequisites.IsValid)
        {
            throw new TwinPrerequisitesException(prerequisites);
        }

        // Số ngày mục tiêu lấy từ lộ trình mới nhất (nếu có), ngược lại dùng mặc định.
        var targetDays = await _db.LearningPaths
            .AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => (int?)p.TargetDays)
            .FirstOrDefaultAsync(cancellationToken) ?? DefaultTargetDays;

        // Đặc trưng bổ sung từ Learning DNA (nếu có): tốc độ học, khả năng tập trung.
        var dna = await _db.LearningDnaProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        var features = new Dictionary<string, double>();
        if (dna is not null)
        {
            features["learningSpeed"] = dna.LearningSpeed;
            features["focusAbility"] = dna.FocusAbility;
        }

        return new PredictionContext(
            learningGoal!,
            assessmentResult!.Score,
            targetDays,
            features);
    }

    /// <summary>
    /// Gọi dịch vụ dự đoán cho một mức thời lượng và kẹp kết quả về khoảng xác suất hợp lệ
    /// [0, 1] (R9.1, R9.2) để bảo vệ trước các giá trị bất thường từ dịch vụ ngoài.
    /// </summary>
    private async Task<double> PredictAsync(
        PredictionContext context, double hoursPerDay, CancellationToken cancellationToken)
    {
        var featureCopy = new Dictionary<string, double>(context.Features);
        var requestFeatures = new PredictionFeatures(
            context.LearningGoal,
            context.CurrentLevelScore,
            hoursPerDay,
            context.TargetDays,
            featureCopy);

        var probability = await _predictionService.PredictSuccessProbabilityAsync(
            requestFeatures, cancellationToken);

        return Math.Clamp(probability, 0d, 1d);
    }

    /// <summary>
    /// Ngữ cảnh dự đoán dựng từ dữ liệu mới nhất của sinh viên, dùng chung cho mọi mức thời lượng.
    /// </summary>
    private sealed record PredictionContext(
        string LearningGoal,
        double CurrentLevelScore,
        int TargetDays,
        IReadOnlyDictionary<string, double> Features);
}
