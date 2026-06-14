using AiLearningPath.Application.Profiles;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Tests.Profiles;

/// <summary>
/// Unit test cho <see cref="ProfileService"/>: lưu/đọc/cập nhật hồ sơ (R3.1–3.3),
/// lưu lựa chọn Learning Goal và nhập điểm số mục tiêu cho goal yêu cầu điểm (R4.2–4.3).
///
/// Dùng EF Core In-Memory provider để kiểm thử thao tác lưu/đọc thật qua DbContext
/// mà không cần SQL Server đang chạy. Mỗi test dùng một database riêng (tên ngẫu nhiên)
/// để cô lập trạng thái.
/// </summary>
public sealed class ProfileServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ProfileInput SampleInput(
        string? learningGoal = null,
        int? targetScore = null,
        double studyHours = 3.5) =>
        new(
            FullName: "Nguyễn Văn A",
            StudentCode: "SV001",
            Major: "Khoa học máy tính",
            LearningGoal: learningGoal,
            TargetScore: targetScore,
            CareerGoal: "Backend Developer",
            StudyHoursPerDay: studyHours);

    // ---- R3.1: Lưu hồ sơ ----

    [Fact]
    public async Task CreateOrUpdateAsync_SavesProfileLinkedToUser()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();

        var saved = await service.CreateOrUpdateAsync(userId, SampleInput());

        Assert.Equal(userId, saved.UserId);
        Assert.Equal("Nguyễn Văn A", saved.FullName);

        // Đọc lại trực tiếp từ DB để xác nhận đã thực sự persist.
        var persisted = await db.Profiles.AsNoTracking().SingleAsync(p => p.UserId == userId);
        Assert.Equal("SV001", persisted.StudentCode);
        Assert.Equal("Khoa học máy tính", persisted.Major);
        Assert.Equal("Backend Developer", persisted.CareerGoal);
        Assert.Equal(3.5, persisted.StudyHoursPerDay);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_CalledTwice_UpdatesExistingProfileWithoutDuplicating()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();

        var first = await service.CreateOrUpdateAsync(userId, SampleInput());
        var updatedInput = SampleInput() with { FullName = "Trần Thị B", StudyHoursPerDay = 5 };
        var second = await service.CreateOrUpdateAsync(userId, updatedInput);

        Assert.Equal(first.Id, second.Id); // cùng một bản ghi, không tạo mới
        Assert.Single(await db.Profiles.Where(p => p.UserId == userId).ToListAsync());
        Assert.Equal("Trần Thị B", second.FullName);
        Assert.Equal(5, second.StudyHoursPerDay);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_WithStudyHoursOutOfRange_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);

        await Assert.ThrowsAsync<ProfileValidationException>(
            () => service.CreateOrUpdateAsync(Guid.NewGuid(), SampleInput(studyHours: 25)));

        // Không có hồ sơ nào được lưu khi đầu vào không hợp lệ.
        Assert.Empty(await db.Profiles.ToListAsync());
    }

    // ---- R3.2: Đọc hồ sơ ----

    [Fact]
    public async Task GetAsync_ReturnsSavedProfile()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(userId, SampleInput());

        var fetched = await service.GetAsync(userId);

        Assert.Equal(userId, fetched.UserId);
        Assert.Equal("Nguyễn Văn A", fetched.FullName);
    }

    [Fact]
    public async Task GetAsync_WhenNoProfile_ThrowsNotFound()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);

        await Assert.ThrowsAsync<ProfileNotFoundException>(
            () => service.GetAsync(Guid.NewGuid()));
    }

    // ---- R3.3: Cập nhật một trường hồ sơ ----

    [Fact]
    public async Task UpdateFieldAsync_WithValidStudyHours_SavesNewValue()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(userId, SampleInput(studyHours: 2));

        var updated = await service.UpdateFieldAsync(userId, ProfileService.StudyHoursField, 6.0);

        Assert.Equal(6.0, updated.StudyHoursPerDay);
        var persisted = await db.Profiles.AsNoTracking().SingleAsync(p => p.UserId == userId);
        Assert.Equal(6.0, persisted.StudyHoursPerDay);
    }

    [Fact]
    public async Task UpdateFieldAsync_WithInvalidStudyHours_ThrowsAndDoesNotPersist()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(userId, SampleInput(studyHours: 2));

        await Assert.ThrowsAsync<ProfileValidationException>(
            () => service.UpdateFieldAsync(userId, ProfileService.StudyHoursField, -1.0));

        var persisted = await db.Profiles.AsNoTracking().SingleAsync(p => p.UserId == userId);
        Assert.Equal(2, persisted.StudyHoursPerDay); // giá trị cũ giữ nguyên
    }

    [Fact]
    public async Task UpdateFieldAsync_UpdatesSimpleField()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(userId, SampleInput());

        var updated = await service.UpdateFieldAsync(userId, nameof(Profile.Major), "Kỹ thuật phần mềm");

        Assert.Equal("Kỹ thuật phần mềm", updated.Major);
    }

    [Fact]
    public async Task UpdateFieldAsync_WithUnsupportedGoal_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(userId, SampleInput());

        await Assert.ThrowsAsync<ProfileValidationException>(
            () => service.UpdateFieldAsync(userId, ProfileService.LearningGoalField, "NotARealGoal"));
    }

    [Fact]
    public async Task UpdateFieldAsync_WhenNoProfile_ThrowsNotFound()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);

        await Assert.ThrowsAsync<ProfileNotFoundException>(
            () => service.UpdateFieldAsync(Guid.NewGuid(), nameof(Profile.Major), "X"));
    }

    // ---- R4.2: Lưu lựa chọn Learning Goal ----

    [Fact]
    public async Task SelectGoalAsync_SavesGoalLinkedToProfile()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();

        var result = await service.SelectGoalAsync(userId, new LearningGoalSelection("FrontendDevelopment", null));

        Assert.Equal("FrontendDevelopment", result.LearningGoal);
        Assert.Null(result.TargetScore); // goal không yêu cầu điểm
        var persisted = await db.Profiles.AsNoTracking().SingleAsync(p => p.UserId == userId);
        Assert.Equal("FrontendDevelopment", persisted.LearningGoal);
    }

    [Fact]
    public async Task SelectGoalAsync_OnExistingProfile_PreservesOtherFields()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();
        await service.CreateOrUpdateAsync(userId, SampleInput());

        var result = await service.SelectGoalAsync(userId, new LearningGoalSelection("DataAnalyst", null));

        Assert.Equal("DataAnalyst", result.LearningGoal);
        Assert.Equal("Nguyễn Văn A", result.FullName); // các trường khác giữ nguyên
    }

    [Fact]
    public async Task SelectGoalAsync_WithUnsupportedGoal_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);

        await Assert.ThrowsAsync<ProfileValidationException>(
            () => service.SelectGoalAsync(Guid.NewGuid(), new LearningGoalSelection("Klingon", null)));
    }

    // ---- R4.3: Nhập điểm số mục tiêu cho goal yêu cầu điểm (IELTS/TOEIC) ----

    [Theory]
    [InlineData("IELTS", 7)]
    [InlineData("TOEIC", 800)]
    public async Task SelectGoalAsync_GoalRequiringScore_SavesTargetScore(string goal, int score)
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();

        var result = await service.SelectGoalAsync(userId, new LearningGoalSelection(goal, score));

        Assert.Equal(goal, result.LearningGoal);
        Assert.Equal(score, result.TargetScore);
        var persisted = await db.Profiles.AsNoTracking().SingleAsync(p => p.UserId == userId);
        Assert.Equal(score, persisted.TargetScore);
    }

    [Fact]
    public async Task SelectGoalAsync_GoalRequiringScore_WithoutScore_ThrowsValidationException()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);

        await Assert.ThrowsAsync<ProfileValidationException>(
            () => service.SelectGoalAsync(Guid.NewGuid(), new LearningGoalSelection("IELTS", null)));
    }

    [Fact]
    public async Task SelectGoalAsync_GoalNotRequiringScore_IgnoresProvidedScore()
    {
        using var db = CreateDb();
        var service = new ProfileService(db);
        var userId = Guid.NewGuid();

        // Goal không yêu cầu điểm: dù có truyền điểm vẫn không lưu (R4.3).
        var result = await service.SelectGoalAsync(userId, new LearningGoalSelection("BackendDevelopment", 999));

        Assert.Equal("BackendDevelopment", result.LearningGoal);
        Assert.Null(result.TargetScore);
    }
}
