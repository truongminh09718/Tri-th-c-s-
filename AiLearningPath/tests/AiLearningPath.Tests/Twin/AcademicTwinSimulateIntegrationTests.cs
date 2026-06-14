using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Twin;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AiLearningPath.Tests.Twin;

/// <summary>
/// Integration test (ví dụ cụ thể) cho <see cref="AcademicTwinService.SimulateAsync"/> chạy
/// end-to-end qua EF Core (<see cref="AppDbContext"/>) với một <see cref="IPredictionService"/>
/// được mock/stub (R9.1). Kiểm chứng rằng service:
/// <list type="bullet">
///   <item>dựng đặc trưng từ dữ liệu mới nhất của sinh viên trong cơ sở dữ liệu (R9.3),</item>
///   <item>gọi dịch vụ dự đoán ML đúng một lần với các đặc trưng tương ứng,</item>
///   <item>trả về đúng xác suất do dịch vụ dự đoán cung cấp cho thời lượng học đã cho.</item>
/// </list>
/// Cũng kiểm chứng <see cref="TwinPrerequisitesException"/> được ném khi thiếu tiên quyết (R9.4).
///
/// Mỗi test dùng một cơ sở dữ liệu In-Memory mới (cô lập) và một stub xác định
/// (deterministic) cho dịch vụ dự đoán — không gọi ML Service thật.
/// </summary>
public sealed class AcademicTwinSimulateIntegrationTests
{
    /// <summary>
    /// Stub dịch vụ dự đoán: trả về một xác suất cố định và ghi lại đặc trưng cùng số lần
    /// được gọi để test kiểm chứng luồng end-to-end (service có thực sự gọi ML Service không,
    /// và gọi với đặc trưng nào).
    /// </summary>
    private sealed class RecordingPredictionService : IPredictionService
    {
        private readonly double _value;

        public RecordingPredictionService(double value) => _value = value;

        public int CallCount { get; private set; }

        public PredictionFeatures? LastFeatures { get; private set; }

        public Task<double> PredictSuccessProbabilityAsync(
            PredictionFeatures features, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastFeatures = features;
            return Task.FromResult(_value);
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
    /// Seed các tiên quyết (Profile có Learning Goal + một AssessmentResult) để
    /// <see cref="AcademicTwinService.SimulateAsync"/> vượt qua kiểm tra tiên quyết (R9.4).
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
            Score = 72,
            StrengthsJson = "[]",
            WeaknessesJson = "[]",
        });

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task SimulateAsync_CallsPredictionService_AndReturnsItsProbability_ForGivenHours()
    {
        // Arrange — cơ sở dữ liệu mới + tiên quyết hợp lệ + stub dự đoán xác định.
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedPrerequisitesAsync(db, userId);

        const double expectedProbability = 0.83;
        const double hoursPerDay = 4.0;
        var prediction = new RecordingPredictionService(expectedProbability);
        var service = new AcademicTwinService(db, prediction);

        // Act — chạy mô phỏng end-to-end qua EF Core.
        var result = await service.SimulateAsync(userId, hoursPerDay);

        // Assert — dịch vụ dự đoán được gọi đúng một lần với đặc trưng dựng từ dữ liệu DB.
        Assert.Equal(1, prediction.CallCount);
        Assert.NotNull(prediction.LastFeatures);
        Assert.Equal("IELTS", prediction.LastFeatures!.LearningGoal);
        Assert.Equal(72, prediction.LastFeatures.CurrentLevelScore);
        Assert.Equal(hoursPerDay, prediction.LastFeatures.HoursPerDay);

        // Kết quả trả về phản ánh đúng thời lượng đầu vào và xác suất từ dịch vụ dự đoán (R9.1).
        Assert.Equal(hoursPerDay, result.HoursPerDay);
        Assert.Equal(expectedProbability, result.SuccessProbability);
    }

    [Fact]
    public async Task SimulateAsync_ClampsProbabilityFromPredictionService_IntoUnitInterval()
    {
        // Arrange — dịch vụ dự đoán trả về giá trị ngoài [0, 1]; service phải kẹp về [0, 1] (R9.1).
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        await SeedPrerequisitesAsync(db, userId);

        var service = new AcademicTwinService(db, new RecordingPredictionService(1.5));

        // Act
        var result = await service.SimulateAsync(userId, 5.0);

        // Assert
        Assert.Equal(1.0, result.SuccessProbability);
    }

    [Fact]
    public async Task SimulateAsync_Throws_WhenPrerequisitesMissing()
    {
        // Arrange — không seed Profile/AssessmentResult => thiếu tiên quyết (R9.4).
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        var prediction = new RecordingPredictionService(0.5);
        var service = new AcademicTwinService(db, prediction);

        // Act + Assert — ném TwinPrerequisitesException và KHÔNG gọi dịch vụ dự đoán.
        await Assert.ThrowsAsync<TwinPrerequisitesException>(
            () => service.SimulateAsync(userId, 4.0));
        Assert.Equal(0, prediction.CallCount);
    }
}
