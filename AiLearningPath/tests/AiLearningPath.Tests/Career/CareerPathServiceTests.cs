using AiLearningPath.Application.Career;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Domain.Careers;
using AiLearningPath.Domain.Common;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Tests.Career;

/// <summary>
/// Unit test cho <see cref="CareerPathService"/> — Career Path AI (R10):
/// <list type="bullet">
///   <item>Danh mục nghề nghiệp được hỗ trợ: Frontend, Backend, Data Analyst, AI Engineer,
///   Tester (R10.1).</item>
///   <item><see cref="CareerPathService.GenerateAsync"/> sinh lộ trình kỹ năng kèm đề xuất
///   chứng chỉ/dự án (R10.2, R10.3) và lưu <see cref="Domain.Entities.CareerPath"/> liên kết
///   với hồ sơ sinh viên (R10.4).</item>
/// </list>
///
/// <see cref="IContentGenerator"/> được thay bằng một stub xác định (deterministic) trả về
/// payload JSON lộ trình kỹ năng tương ứng với nghề nghiệp yêu cầu, nên không cần gọi Gemini
/// thật. EF Core In-Memory provider được dùng để kiểm thử thao tác lưu/đọc thật qua DbContext
/// mà không cần SQL Server đang chạy; mỗi test dùng một database riêng để cô lập trạng thái.
/// </summary>
public sealed class CareerPathServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Stub xác định của <see cref="IContentGenerator"/>: sinh lộ trình kỹ năng kèm chứng chỉ
    /// và dự án gắn với nghề nghiệp nhận được, đồng thời ghi lại yêu cầu cuối cùng để kiểm chứng.
    /// </summary>
    private sealed class StubContentGenerator : IContentGenerator
    {
        public ContentGenerationRequest? LastRequest { get; private set; }

        public Task<ContentGenerationResult> GenerateAsync(
            ContentGenerationRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;

            var career = request.LearningGoal;
            var content = new GeneratedCareerContent(
                Skills: new[] { $"{career}-Skill-1", $"{career}-Skill-2", $"{career}-Skill-3" },
                Certifications: new[] { $"{career}-Cert-1", $"{career}-Cert-2" },
                Projects: new[] { $"{career}-Project-1", $"{career}-Project-2" });

            var json = JsonFieldSerializer.Serialize(content);
            return Task.FromResult(new ContentGenerationResult(json));
        }
    }

    /// <summary>Stub trả về payload trống (không có kỹ năng) để mô phỏng lỗi sinh nội dung.</summary>
    private sealed class EmptyContentGenerator : IContentGenerator
    {
        public Task<ContentGenerationResult> GenerateAsync(
            ContentGenerationRequest request, CancellationToken cancellationToken = default)
        {
            var content = new GeneratedCareerContent(
                Skills: Array.Empty<string>(),
                Certifications: Array.Empty<string>(),
                Projects: Array.Empty<string>());
            return Task.FromResult(new ContentGenerationResult(JsonFieldSerializer.Serialize(content)));
        }
    }

    // ---- R10.1: Danh mục nghề nghiệp ----

    // R10.1: ListCareers trả về đúng danh mục nghề nghiệp được hỗ trợ.
    [Fact]
    public void ListCareers_ReturnsSupportedCatalog()
    {
        using var db = CreateDb();
        var service = new CareerPathService(db, new StubContentGenerator());

        var careers = service.ListCareers();

        Assert.Equal(
            new[] { "Frontend", "Backend", "DataAnalyst", "AIEngineer", "Tester" },
            careers);
    }

    // R10.1: Mỗi nghề nghiệp trong danh mục đều được coi là hợp lệ và sinh lộ trình thành công.
    [Theory]
    [InlineData("Frontend")]
    [InlineData("Backend")]
    [InlineData("DataAnalyst")]
    [InlineData("AIEngineer")]
    [InlineData("Tester")]
    public async Task GenerateAsync_ForEverySupportedCareer_Succeeds(string career)
    {
        using var db = CreateDb();
        var service = new CareerPathService(db, new StubContentGenerator());
        var userId = Guid.NewGuid();

        var path = await service.GenerateAsync(userId, career);

        Assert.Equal(career, path.Career);
        Assert.Equal(userId, path.UserId);
    }

    // ---- R10.2, R10.3: Sinh lộ trình kỹ năng kèm chứng chỉ/dự án ----

    // R10.2/R10.3: GenerateAsync sinh lộ trình kỹ năng kèm chứng chỉ và dự án qua dịch vụ AI.
    [Fact]
    public async Task GenerateAsync_GeneratesSkillPathWithCertificationsAndProjects()
    {
        using var db = CreateDb();
        var generator = new StubContentGenerator();
        var service = new CareerPathService(db, generator);
        var userId = Guid.NewGuid();
        const string career = "Frontend";

        var path = await service.GenerateAsync(userId, career);

        // Generator được gọi với đúng nghề nghiệp và loại nội dung CareerPath (R10.2).
        Assert.NotNull(generator.LastRequest);
        Assert.Equal(career, generator.LastRequest!.LearningGoal);
        Assert.Equal(CareerContentKind.CareerPath, generator.LastRequest.Kind);

        // Lộ trình kỹ năng (R10.2).
        var skills = JsonFieldSerializer.Deserialize<List<string>>(path.SkillsJson);
        Assert.NotNull(skills);
        Assert.NotEmpty(skills!);
        Assert.All(skills!, s => Assert.StartsWith(career, s));

        // Đề xuất chứng chỉ và dự án (R10.3).
        var certs = JsonFieldSerializer.Deserialize<List<string>>(path.CertificationsJson);
        Assert.NotNull(certs);
        Assert.NotEmpty(certs!);
        Assert.All(certs!, c => Assert.StartsWith(career, c));

        var projects = JsonFieldSerializer.Deserialize<List<string>>(path.ProjectsJson);
        Assert.NotNull(projects);
        Assert.NotEmpty(projects!);
        Assert.All(projects!, p => Assert.StartsWith(career, p));
    }

    // ---- R10.4: Lưu lộ trình liên kết với hồ sơ sinh viên ----

    // R10.4: Lộ trình nghề nghiệp được persist và liên kết đúng userId.
    [Fact]
    public async Task GenerateAsync_PersistsCareerPathLinkedToUser()
    {
        using var db = CreateDb();
        var service = new CareerPathService(db, new StubContentGenerator());
        var userId = Guid.NewGuid();
        const string career = "DataAnalyst";

        var path = await service.GenerateAsync(userId, career);

        // Đọc lại trực tiếp từ DB để xác nhận đã thực sự persist liên kết với sinh viên.
        var persisted = await db.CareerPaths.AsNoTracking().SingleAsync(c => c.Id == path.Id);
        Assert.Equal(userId, persisted.UserId);
        Assert.Equal(career, persisted.Career);
        Assert.False(string.IsNullOrWhiteSpace(persisted.SkillsJson));
        Assert.False(string.IsNullOrWhiteSpace(persisted.CertificationsJson));
        Assert.False(string.IsNullOrWhiteSpace(persisted.ProjectsJson));

        var skills = JsonFieldSerializer.Deserialize<List<string>>(persisted.SkillsJson);
        Assert.Equal(new[] { "DataAnalyst-Skill-1", "DataAnalyst-Skill-2", "DataAnalyst-Skill-3" }, skills);
    }

    // ---- R10.1: Nghề nghiệp không hợp lệ ----

    // R10.1: Nghề nghiệp không thuộc danh mục bị từ chối trước khi gọi generator và không lưu gì.
    [Fact]
    public async Task GenerateAsync_WithUnsupportedCareer_ThrowsAndDoesNotPersist()
    {
        using var db = CreateDb();
        var service = new CareerPathService(db, new StubContentGenerator());

        await Assert.ThrowsAsync<CareerValidationException>(
            () => service.GenerateAsync(Guid.NewGuid(), "Astronaut"));

        Assert.Empty(await db.CareerPaths.ToListAsync());
    }

    // Nội dung AI rỗng (không có kỹ năng) bị từ chối và không lưu lộ trình (bảo vệ R10.2).
    [Fact]
    public async Task GenerateAsync_WithEmptyGeneratedContent_ThrowsAndDoesNotPersist()
    {
        using var db = CreateDb();
        var service = new CareerPathService(db, new EmptyContentGenerator());

        await Assert.ThrowsAsync<CareerGenerationException>(
            () => service.GenerateAsync(Guid.NewGuid(), "Backend"));

        Assert.Empty(await db.CareerPaths.ToListAsync());
    }
}
