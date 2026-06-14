using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Domain.LearningDna;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Tests.LearningDna;

/// <summary>
/// Unit test cho <see cref="LearningDnaEngine"/>:
/// <list type="bullet">
///   <item>
///     <c>BuildAsync</c> tạo và lưu một <see cref="LearningDnaProfile"/> từ một
///     <see cref="AssessmentResult"/> đã có, liên kết với tài khoản sinh viên (R6.1, R6.2).
///   </item>
///   <item>
///     <c>UpdateAsync</c> hợp nhất dữ liệu tiến độ mới (<see cref="ProgressData"/>) vào
///     hồ sơ DNA hiện tại (R6.3).
///   </item>
/// </list>
///
/// Dùng EF Core In-Memory provider để kiểm thử thao tác lưu/đọc thật qua DbContext mà
/// không cần SQL Server đang chạy. Mỗi test dùng một database riêng (tên ngẫu nhiên) để
/// cô lập trạng thái.
/// </summary>
public sealed class LearningDnaEngineTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Tạo một AssessmentResult mẫu (đã serialize điểm mạnh/yếu sang JSON) và lưu vào DB.</summary>
    private static async Task<AssessmentResult> SeedAssessmentResultAsync(
        AppDbContext db,
        Guid userId,
        double score,
        IReadOnlyList<string> strengths,
        IReadOnlyList<string> weaknesses)
    {
        var result = new AssessmentResult
        {
            AssessmentId = Guid.NewGuid(),
            UserId = userId,
            Level = "Intermediate",
            Score = score,
            StrengthsJson = JsonFieldSerializer.Serialize(strengths),
            WeaknessesJson = JsonFieldSerializer.Serialize(weaknesses),
        };
        db.AssessmentResults.Add(result);
        await db.SaveChangesAsync();
        return result;
    }

    // ---- R6.1 / R6.2: Build DNA từ kết quả đánh giá và lưu liên kết với tài khoản ----

    [Fact]
    public async Task BuildAsync_CreatesAndPersistsDnaProfileFromAssessmentResult()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();

        // Hồ sơ có số giờ học/ngày để DnaBuilder suy ra khung giờ hiệu quả.
        db.Profiles.Add(new Profile
        {
            UserId = userId,
            FullName = "Nguyễn Văn A",
            StudyHoursPerDay = 4,
        });
        var assessment = await SeedAssessmentResultAsync(
            db, userId,
            score: 72,
            strengths: new[] { "Grammar", "Reading" },
            weaknesses: new[] { "Listening" });

        var engine = new LearningDnaEngine(db);

        var dna = await engine.BuildAsync(userId, assessment);

        // Trả về hồ sơ gắn đúng chủ sở hữu, suy ra các trường DNA.
        Assert.Equal(userId, dna.UserId);
        Assert.False(string.IsNullOrWhiteSpace(dna.LearningStyle));
        Assert.True(dna.LearningSpeed >= DnaBuilder.MinLearningSpeed
            && dna.LearningSpeed <= DnaBuilder.MaxLearningSpeed);
        Assert.InRange(dna.FocusAbility, 0d, 1d);

        // Điểm mạnh/yếu được serialize từ kết quả đánh giá và tách biệt.
        var strengths = JsonFieldSerializer.Deserialize<List<string>>(dna.StrengthsJson)!;
        var weaknesses = JsonFieldSerializer.Deserialize<List<string>>(dna.WeaknessesJson)!;
        Assert.Contains("Grammar", strengths);
        Assert.Contains("Reading", strengths);
        Assert.Contains("Listening", weaknesses);
        Assert.Empty(strengths.Intersect(weaknesses));

        // Có khung giờ hiệu quả vì StudyHoursPerDay > 0.
        var effectiveHours = JsonFieldSerializer.Deserialize<List<string>>(dna.EffectiveHoursJson)!;
        Assert.NotEmpty(effectiveHours);

        // Đọc lại trực tiếp từ DB để xác nhận đã thực sự persist (R6.2).
        var persisted = await db.LearningDnaProfiles.AsNoTracking()
            .SingleAsync(d => d.UserId == userId);
        Assert.Equal(dna.LearningStyle, persisted.LearningStyle);
        Assert.Equal(dna.LearningSpeed, persisted.LearningSpeed);
    }

    [Fact]
    public async Task BuildAsync_CalledTwice_OverwritesExistingProfileWithoutDuplicating()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Profiles.Add(new Profile { UserId = userId, StudyHoursPerDay = 2 });

        var first = await SeedAssessmentResultAsync(
            db, userId, score: 30,
            strengths: new[] { "Vocabulary" },
            weaknesses: new[] { "Grammar" });

        var engine = new LearningDnaEngine(db);
        var built1 = await engine.BuildAsync(userId, first);
        var firstId = built1.Id;
        var firstStyle = built1.LearningStyle;

        // Kết quả đánh giá mới với điểm cao hơn -> phong cách học suy ra khác.
        var second = await SeedAssessmentResultAsync(
            db, userId, score: 90,
            strengths: new[] { "Grammar", "Reading" },
            weaknesses: Array.Empty<string>());

        var built2 = await engine.BuildAsync(userId, second);

        // Quan hệ 1-1: ghi đè cùng một bản ghi, không tạo mới.
        Assert.Equal(firstId, built2.Id);
        Assert.Single(await db.LearningDnaProfiles.Where(d => d.UserId == userId).ToListAsync());
        Assert.NotEqual(firstStyle, built2.LearningStyle);
    }

    // ---- R6.3: Cập nhật DNA từ dữ liệu tiến độ mới ----

    [Fact]
    public async Task UpdateAsync_MergesNewProgressDataIntoExistingProfile()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Profiles.Add(new Profile { UserId = userId, StudyHoursPerDay = 3 });

        var assessment = await SeedAssessmentResultAsync(
            db, userId, score: 50,
            strengths: new[] { "Reading" },
            weaknesses: new[] { "Listening", "Writing" });

        var engine = new LearningDnaEngine(db);
        var built = await engine.BuildAsync(userId, assessment);
        var styleBefore = built.LearningStyle;

        // Dữ liệu tiến độ mới: "Listening" đã tiến bộ (thành điểm mạnh),
        // "Speaking" còn yếu (thành điểm yếu mới).
        var progress = new ProgressData(
            CompletionRate: 0.9,
            AverageFocusMinutes: 50,
            ImprovedSkills: new[] { "Listening" },
            StruggledSkills: new[] { "Speaking" });

        var updated = await engine.UpdateAsync(userId, progress);

        var strengths = JsonFieldSerializer.Deserialize<List<string>>(updated.StrengthsJson)!;
        var weaknesses = JsonFieldSerializer.Deserialize<List<string>>(updated.WeaknessesJson)!;

        // Kỹ năng tiến bộ chuyển sang điểm mạnh và bị loại khỏi điểm yếu.
        Assert.Contains("Listening", strengths);
        Assert.DoesNotContain("Listening", weaknesses);
        // Kỹ năng còn yếu được thêm vào điểm yếu.
        Assert.Contains("Speaking", weaknesses);
        // Hai tập vẫn tách biệt sau khi hợp nhất.
        Assert.Empty(strengths.Intersect(weaknesses));

        // Merge giữ nguyên phong cách học (chỉ làm trơn focus/speed + hợp nhất điểm mạnh/yếu).
        Assert.Equal(styleBefore, updated.LearningStyle);
        Assert.InRange(updated.FocusAbility, 0d, 1d);
        Assert.True(updated.LearningSpeed >= DnaBuilder.MinLearningSpeed
            && updated.LearningSpeed <= DnaBuilder.MaxLearningSpeed);

        // Thay đổi đã được persist (đọc lại từ DB).
        var persisted = await db.LearningDnaProfiles.AsNoTracking()
            .SingleAsync(d => d.UserId == userId);
        Assert.Contains("Speaking",
            JsonFieldSerializer.Deserialize<List<string>>(persisted.WeaknessesJson)!);
        // Vẫn chỉ có một hồ sơ DNA cho người dùng.
        Assert.Single(await db.LearningDnaProfiles.Where(d => d.UserId == userId).ToListAsync());
    }

    [Fact]
    public async Task UpdateAsync_WhenNoProfileExists_ThrowsNotFound()
    {
        using var db = CreateDb();
        var engine = new LearningDnaEngine(db);

        var progress = new ProgressData(
            CompletionRate: 0.5,
            AverageFocusMinutes: 30,
            ImprovedSkills: Array.Empty<string>(),
            StruggledSkills: Array.Empty<string>());

        await Assert.ThrowsAsync<AiLearningPath.Application.LearningDna.LearningDnaNotFoundException>(
            () => engine.UpdateAsync(Guid.NewGuid(), progress));
    }
}
