using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Paths;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Tests.Paths;

/// <summary>
/// Integration test cho <see cref="PathGenerator.GenerateAsync"/> (R7.1): sinh nội dung
/// lộ trình theo Learning Goal qua <see cref="IContentGenerator"/> (bọc Gemini), xây cấu
/// trúc tháng → tuần → ngày và lưu <see cref="LearningPath"/> liên kết với hồ sơ sinh viên.
///
/// <see cref="IContentGenerator"/> được thay bằng một stub xác định (deterministic) trả về
/// nội dung lộ trình gắn với Learning Goal yêu cầu, nên không cần gọi Gemini thật.
/// EF Core In-Memory provider được dùng để kiểm thử thao tác lưu/đọc thật qua DbContext
/// mà không cần SQL Server đang chạy; mỗi test dùng một database riêng để cô lập trạng thái.
/// </summary>
public sealed class PathGeneratorGenerateTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Stub xác định của <see cref="IContentGenerator"/>: trả về nội dung lộ trình gắn với
    /// Learning Goal nhận được, đồng thời ghi lại yêu cầu cuối cùng để kiểm chứng.
    /// </summary>
    private sealed class StubContentGenerator : IContentGenerator
    {
        public ContentGenerationRequest? LastRequest { get; private set; }

        public Task<ContentGenerationResult> GenerateAsync(
            ContentGenerationRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            var content = $"[{request.LearningGoal}] Nội dung lộ trình do AI sinh ra.";
            return Task.FromResult(new ContentGenerationResult(content));
        }
    }

    /// <summary>Stub trả về nội dung rỗng để kiểm thử nhánh thất bại của sinh nội dung AI.</summary>
    private sealed class EmptyContentGenerator : IContentGenerator
    {
        public bool WasCalled { get; private set; }

        public Task<ContentGenerationResult> GenerateAsync(
            ContentGenerationRequest request, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(new ContentGenerationResult(string.Empty));
        }
    }

    /// <summary>Tạo Profile (cung cấp Learning Goal + số giờ học) và lưu vào DB.</summary>
    private static async Task SeedProfileAsync(
        AppDbContext db, Guid userId, string learningGoal, double studyHoursPerDay)
    {
        db.Profiles.Add(new Profile
        {
            UserId = userId,
            FullName = "Nguyễn Văn A",
            LearningGoal = learningGoal,
            StudyHoursPerDay = studyHoursPerDay,
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Tạo AssessmentResult (điều kiện tiên quyết R7.4) và lưu vào DB.</summary>
    private static async Task SeedAssessmentResultAsync(
        AppDbContext db, Guid userId, IReadOnlyList<string> strengths, IReadOnlyList<string> weaknesses)
    {
        db.AssessmentResults.Add(new AssessmentResult
        {
            AssessmentId = Guid.NewGuid(),
            UserId = userId,
            Level = "Intermediate",
            Score = 60,
            StrengthsJson = JsonFieldSerializer.Serialize(strengths),
            WeaknessesJson = JsonFieldSerializer.Serialize(weaknesses),
        });
        await db.SaveChangesAsync();
    }

    // R7.1/R7.2/R7.3: GenerateAsync gọi generator, xây lộ trình tháng→tuần→ngày và persist.
    [Fact]
    public async Task GenerateAsync_BuildsAndPersistsLearningPathFromGeneratedContent()
    {
        using var db = CreateDb();
        var generator = new StubContentGenerator();
        var userId = Guid.NewGuid();
        const string goal = "FrontendDevelopment";
        const int targetDays = 45;

        await SeedProfileAsync(db, userId, goal, studyHoursPerDay: 4);
        await SeedAssessmentResultAsync(
            db, userId, strengths: new[] { "HTML", "CSS" }, weaknesses: new[] { "JavaScript" });

        var generated = await new PathGenerator(db, generator).GenerateAsync(userId, targetDays);

        // Lộ trình được gắn với đúng sinh viên và Learning Goal đã chọn.
        Assert.Equal(userId, generated.UserId);
        Assert.Equal(goal, generated.LearningGoal);
        Assert.Equal(targetDays, generated.TargetDays);

        // Generator được gọi với đúng Learning Goal và loại nội dung LearningPath (R7.1).
        Assert.NotNull(generator.LastRequest);
        Assert.Equal(goal, generator.LastRequest!.LearningGoal);
        Assert.Equal(PathContentKind.LearningPath, generator.LastRequest.Kind);
        Assert.Equal(targetDays.ToString(), generator.LastRequest.Parameters["targetDays"]);

        // Lộ trình được persist với phân cấp tháng → tuần → ngày (R7.2, R7.3).
        var persisted = await db.LearningPaths
            .Include(p => p.Phases)
            .ThenInclude(ph => ph.Tasks)
            .AsNoTracking()
            .SingleAsync(p => p.Id == generated.Id);

        Assert.Equal(userId, persisted.UserId);
        Assert.NotEmpty(persisted.Phases);

        // Tổng số nhiệm vụ (ngày) bao phủ đúng toàn bộ targetDays, không trùng/hở (R7.1, R7.2).
        var totalTasks = persisted.Phases.Sum(ph => ph.Tasks.Count);
        Assert.Equal(targetDays, totalTasks);

        // Nội dung do AI sinh ra được dùng làm mô tả nhiệm vụ.
        var allTasks = persisted.Phases.SelectMany(ph => ph.Tasks).ToList();
        Assert.All(allTasks, t =>
        {
            Assert.Contains(goal, t.Description);
            Assert.False(t.Completed);
            Assert.False(string.IsNullOrWhiteSpace(t.Skill));
        });

        // Giai đoạn phản ánh phân cấp tháng/tuần (1-based).
        Assert.All(persisted.Phases, ph =>
        {
            Assert.True(ph.MonthIndex >= 1);
            Assert.True(ph.WeekIndex >= 1);
        });
    }

    // R7.5: thời gian mục tiêu không đủ -> lộ trình kèm cảnh báo tính khả thi.
    [Fact]
    public async Task GenerateAsync_WhenTimeInsufficient_PersistsPathWithFeasibilityWarning()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        const string goal = "IELTS";

        // Số giờ học mỗi ngày (0.5) nhỏ hơn số giờ ước lượng mỗi ngày của lộ trình (2.0)
        // => tổng giờ ước lượng vượt giờ khả dụng => cảnh báo bật.
        await SeedProfileAsync(db, userId, goal, studyHoursPerDay: 0.5);
        await SeedAssessmentResultAsync(
            db, userId, strengths: new[] { "Reading" }, weaknesses: new[] { "Listening" });

        var generated = await new PathGenerator(db, new StubContentGenerator())
            .GenerateAsync(userId, targetDays: 30);

        Assert.True(generated.FeasibilityWarning);

        var persisted = await db.LearningPaths.AsNoTracking().SingleAsync(p => p.Id == generated.Id);
        Assert.True(persisted.FeasibilityWarning);
    }

    // R7.4: sinh lộ trình khi chưa hoàn thành assessment bị từ chối và không lưu gì.
    [Fact]
    public async Task GenerateAsync_WithoutAssessmentResult_ThrowsAndDoesNotPersist()
    {
        using var db = CreateDb();
        var generator = new StubContentGenerator();
        var userId = Guid.NewGuid();

        // Có hồ sơ + Learning Goal nhưng chưa có AssessmentResult.
        await SeedProfileAsync(db, userId, "BackendDevelopment", studyHoursPerDay: 3);

        await Assert.ThrowsAsync<PathValidationException>(
            () => new PathGenerator(db, generator).GenerateAsync(userId, targetDays: 30));

        // Generator không được gọi và không có lộ trình nào được lưu (R7.4).
        Assert.Null(generator.LastRequest);
        Assert.Empty(await db.LearningPaths.ToListAsync());
    }

    // R7.1: nội dung lộ trình do AI trả về rỗng -> ném PathGenerationException, không lưu.
    [Fact]
    public async Task GenerateAsync_WhenGeneratedContentEmpty_ThrowsAndDoesNotPersist()
    {
        using var db = CreateDb();
        var generator = new EmptyContentGenerator();
        var userId = Guid.NewGuid();

        await SeedProfileAsync(db, userId, "DataAnalyst", studyHoursPerDay: 3);
        await SeedAssessmentResultAsync(
            db, userId, strengths: new[] { "SQL" }, weaknesses: new[] { "Statistics" });

        await Assert.ThrowsAsync<PathGenerationException>(
            () => new PathGenerator(db, generator).GenerateAsync(userId, targetDays: 30));

        Assert.True(generator.WasCalled);
        Assert.Empty(await db.LearningPaths.ToListAsync());
    }
}
