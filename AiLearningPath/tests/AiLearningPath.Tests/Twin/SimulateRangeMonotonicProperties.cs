using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Twin;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Tests.Twin;

/// <summary>
/// Property-based test cho <see cref="AcademicTwinService.SimulateRangeAsync"/> (R9.1, R9.2):
/// <list type="bullet">
///   <item><b>Đầy đủ (completeness)</b>: trả về đúng một dự đoán cho mỗi mức thời lượng đầu vào
///   (số lượng kết quả bằng số lượng đầu vào).</item>
///   <item><b>Đơn điệu không giảm</b>: khi tăng thời lượng học mỗi ngày, xác suất đạt mục tiêu
///   không giảm — dù dịch vụ dự đoán nền (mock) trả về giá trị thô dao động không đơn điệu.</item>
/// </list>
///
/// Dùng EF Core In-Memory provider và một <see cref="IPredictionService"/> stub theo quy ước
/// kiểm thử hiện có. Stub trả về xác suất thô <i>dao động không đơn điệu</i> theo thời lượng để
/// đảm bảo chính lớp dịch vụ (sắp xếp theo thời lượng + running-max) là thứ tạo ra tính đơn điệu,
/// chứ không phải dữ liệu mock vô tình đã đơn điệu.
///
/// Validates: Requirements 9.1, 9.2
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public sealed class SimulateRangeMonotonicProperties
{
    /// <summary>
    /// Stub <see cref="IPredictionService"/> trả về xác suất thô dao động (không đơn điệu) theo
    /// thời lượng học, nằm trong [0, 1]. Dùng hàm sin để cố tình phá vỡ tính đơn điệu của dữ liệu thô.
    /// </summary>
    private sealed class OscillatingPredictionService : IPredictionService
    {
        public Task<double> PredictSuccessProbabilityAsync(
            PredictionFeatures features, CancellationToken cancellationToken = default)
        {
            // (sin(x) + 1) / 2 ∈ [0, 1], dao động lên xuống theo HoursPerDay -> KHÔNG đơn điệu.
            var raw = (Math.Sin(features.HoursPerDay * 1.7) + 1d) / 2d;
            return Task.FromResult(raw);
        }
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Seed các tiên quyết cần thiết để mô phỏng (R9.4): Learning Goal trên Profile và
    /// một AssessmentResult của cùng người dùng. Trả về userId.
    /// </summary>
    private static async Task<Guid> SeedPrerequisitesAsync(AppDbContext db)
    {
        var userId = Guid.NewGuid();

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
        return userId;
    }

    /// <summary>
    /// Sinh một mức thời lượng học mỗi ngày trong [0, 24] với bước 0.1 (giá trị hữu hạn, hợp lý),
    /// cho phép trùng lặp để kiểm tra cả trường hợp các mức bằng nhau.
    /// </summary>
    private static Gen<double> HoursGen =>
        Gen.Choose(0, 240).Select(i => i / 10.0);

    /// <summary>Sinh danh sách các mức thời lượng (0..30 phần tử), có thể rỗng và có thể trùng.</summary>
    private static Gen<double[]> HoursListGen =>
        Gen.Choose(0, 30).SelectMany(n => Gen.ArrayOf(n, HoursGen));

    // Feature: ai-learning-path, Property 18: Mô phỏng nhiều mức thời lượng đầy đủ và đơn điệu
    // Với mọi danh sách mức thời lượng: SimulateRangeAsync trả về đúng một dự đoán cho mỗi mức
    // (completeness) và xác suất đạt mục tiêu không giảm khi thời lượng tăng (đơn điệu không giảm).
    [Property(MaxTest = 100)]
    public Property SimulateRange_IsCompleteAndMonotonicNonDecreasing()
    {
        return Prop.ForAll(Arb.From(HoursListGen), hoursOptions =>
        {
            using var db = CreateDb();
            var userId = SeedPrerequisitesAsync(db).GetAwaiter().GetResult();
            var service = new AcademicTwinService(db, new OscillatingPredictionService());

            var predictions = service
                .SimulateRangeAsync(userId, hoursOptions)
                .GetAwaiter().GetResult();

            // (a) Completeness: đúng một dự đoán cho mỗi mức đầu vào, giữ nguyên thứ tự/giá trị thời lượng.
            bool completeCount = predictions.Count == hoursOptions.Length;
            bool hoursPreserved = predictions
                .Select(p => p.HoursPerDay)
                .SequenceEqual(hoursOptions);

            // (b) Đơn điệu không giảm: sắp theo thời lượng tăng dần thì xác suất không được giảm.
            var byHours = predictions.OrderBy(p => p.HoursPerDay).ToArray();
            bool monotonic = true;
            for (var i = 1; i < byHours.Length; i++)
            {
                if (byHours[i].SuccessProbability < byHours[i - 1].SuccessProbability)
                {
                    monotonic = false;
                    break;
                }
            }

            // Xác suất luôn hợp lệ trong [0, 1].
            bool valid = predictions.All(p => p.SuccessProbability is >= 0d and <= 1d);

            return (completeCount && hoursPreserved && monotonic && valid)
                .Label($"count={predictions.Count} expected={hoursOptions.Length} " +
                       $"hoursPreserved={hoursPreserved} monotonic={monotonic} valid={valid} " +
                       $"hours=[{string.Join(",", hoursOptions)}]");
        });
    }
}
