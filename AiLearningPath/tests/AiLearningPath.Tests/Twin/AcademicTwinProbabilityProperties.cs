using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Tests.Twin;

/// <summary>
/// Property-based test cho <see cref="AcademicTwinService.SimulateAsync"/>: dù dịch vụ
/// dự đoán ML (<see cref="IPredictionService"/>) trả về bất kỳ giá trị nào — kể cả
/// ngoài khoảng [0, 1] hay các giá trị bất thường (âm lớn, lớn hơn 1, vô cực) — thì
/// xác suất đạt mục tiêu do Academic Twin trả về luôn là một giá trị xác suất hợp lệ
/// nằm trong khoảng [0, 1] (service kẹp/clamp kết quả về [0, 1]).
///
/// Validates: Requirements 9.1, 9.2
/// Chạy tối thiểu 100 iteration với FsCheck.
///
/// Dùng EF Core In-Memory provider để chạy luồng thật qua DbContext mà không cần SQL
/// Server, và một stub <see cref="IPredictionService"/> trả về giá trị do test điều khiển.
/// </summary>
public class AcademicTwinProbabilityProperties
{
    /// <summary>
    /// Stub dịch vụ dự đoán: trả về một xác suất cố định (có thể ngoài [0, 1] hoặc bất
    /// thường) để kiểm tra rằng service luôn kẹp kết quả về khoảng xác suất hợp lệ.
    /// </summary>
    private sealed class StubPredictionService : IPredictionService
    {
        private readonly double _value;

        public StubPredictionService(double value) => _value = value;

        public Task<double> PredictSuccessProbabilityAsync(
            PredictionFeatures features, CancellationToken cancellationToken = default)
            => Task.FromResult(_value);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Seed các tiên quyết (Learning Goal trong Profile + một AssessmentResult) để
    /// <see cref="AcademicTwinService.SimulateAsync"/> vượt qua kiểm tra tiên quyết (R9.4)
    /// và tiến hành mô phỏng.
    /// </summary>
    private static async Task SeedPrerequisitesAsync(AppDbContext db, Guid userId)
    {
        db.Profiles.Add(new Profile
        {
            UserId = userId,
            FullName = "Test Student",
            LearningGoal = "IELTS",
            StudyHoursPerDay = 3,
        });

        db.AssessmentResults.Add(new AssessmentResult
        {
            AssessmentId = Guid.NewGuid(),
            UserId = userId,
            Level = "Intermediate",
            Score = 60,
            StrengthsJson = "[]",
            WeaknessesJson = "[]",
        });

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Sinh các giá trị xác suất "thô" từ dịch vụ ML, bao phủ cả miền hợp lệ [0, 1] lẫn
    /// các giá trị bất thường ngoài miền (âm, lớn hơn 1, rất lớn, vô cực dương/âm).
    /// </summary>
    private static Gen<double> RawProbabilityGen =>
        Gen.OneOf(
            // Giá trị hợp lệ trong [0, 1].
            Gen.Choose(0, 1000).Select(i => i / 1000.0),
            // Giá trị ngoài khoảng và bất thường.
            Gen.Elements(
                -1.0, -0.0001, -1000.0, -1e18,
                1.0001, 2.0, 1000.0, 1e18,
                double.NegativeInfinity, double.PositiveInfinity));

    /// <summary>
    /// Sinh số giờ học mỗi ngày hợp lý (không âm) cho mô phỏng.
    /// </summary>
    private static Gen<double> HoursPerDayGen =>
        Gen.Choose(0, 240).Select(i => i / 10.0);

    // Feature: ai-learning-path, Property 17: Xác suất đạt mục tiêu là một giá trị xác suất hợp lệ
    // Với mọi giá trị thô do IPredictionService trả về và mọi số giờ học mỗi ngày,
    // SuccessProbability do SimulateAsync trả về SHALL nằm trong [0, 1].
    [Property(MaxTest = 100)]
    public Property SimulateAsync_SuccessProbability_AlwaysInUnitInterval()
    {
        return Prop.ForAll(
            Arb.From(RawProbabilityGen),
            Arb.From(HoursPerDayGen),
            (rawProbability, hoursPerDay) =>
            {
                using var db = CreateDb();
                var userId = Guid.NewGuid();

                SeedPrerequisitesAsync(db, userId).GetAwaiter().GetResult();

                var service = new AcademicTwinService(db, new StubPredictionService(rawProbability));
                var prediction = service.SimulateAsync(userId, hoursPerDay).GetAwaiter().GetResult();

                bool inRange = prediction.SuccessProbability >= 0d
                    && prediction.SuccessProbability <= 1d;

                return inRange.Label(
                    $"raw={rawProbability} hoursPerDay={hoursPerDay} " +
                    $"=> SuccessProbability={prediction.SuccessProbability} (expected within [0, 1])");
            });
    }
}
