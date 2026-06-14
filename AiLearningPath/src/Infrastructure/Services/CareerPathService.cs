using AiLearningPath.Application.Career;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Domain.Careers;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai <see cref="ICareerPathService"/> trên nền EF Core (<see cref="AppDbContext"/>).
/// Điều phối nghiệp vụ định hướng nghề nghiệp — Career Path AI (R10):
/// <list type="bullet">
///   <item><see cref="ListCareers"/> trả về danh mục nghề nghiệp tĩnh từ
///   <see cref="CareerCatalog.Careers"/> (R10.1).</item>
///   <item><see cref="GenerateAsync"/> kiểm tra nghề nghiệp thuộc danh mục hỗ trợ (R10.1),
///   gọi <see cref="IContentGenerator"/> (bọc Gemini) sinh lộ trình kỹ năng cùng đề xuất
///   chứng chỉ/dự án (R10.2, R10.3), rồi ánh xạ sang entity <see cref="CareerPath"/> và lưu
///   liên kết với hồ sơ sinh viên (R10.4).</item>
/// </list>
/// Các trường danh sách (kỹ năng, chứng chỉ, dự án) được lưu dạng JSON qua
/// <see cref="JsonFieldSerializer"/>. Mọi thao tác đều gắn với <c>userId</c> để hỗ trợ
/// ownership-based authorization (R14).
/// </summary>
public sealed class CareerPathService : ICareerPathService
{
    private readonly AppDbContext _db;
    private readonly IContentGenerator _contentGenerator;

    public CareerPathService(AppDbContext db, IContentGenerator contentGenerator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _contentGenerator = contentGenerator ?? throw new ArgumentNullException(nameof(contentGenerator));
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListCareers() => CareerCatalog.Careers;

    /// <inheritdoc />
    public async Task<CareerPath> GenerateAsync(
        Guid userId, string career, CancellationToken cancellationToken = default)
    {
        // R10.1: nghề nghiệp phải thuộc danh mục hỗ trợ trước khi sinh lộ trình.
        if (!CareerCatalog.IsSupported(career))
        {
            throw new CareerValidationException(
                ValidationResult.Failure(
                    $"Nghề nghiệp '{career}' không nằm trong danh mục được hỗ trợ."));
        }

        // R10.2, R10.3: sinh lộ trình kỹ năng + đề xuất chứng chỉ/dự án qua dịch vụ AI (Gemini).
        var request = new ContentGenerationRequest(
            CareerContentKind.CareerPath,
            career,
            new Dictionary<string, string>());

        var generated = await _contentGenerator.GenerateAsync(request, cancellationToken);
        var content = ParseContent(generated?.Content);

        // R10.4: ánh xạ sang entity và lưu liên kết với hồ sơ sinh viên.
        var entity = new CareerPath
        {
            UserId = userId,
            Career = career,
            SkillsJson = JsonFieldSerializer.Serialize(content.Skills ?? Array.Empty<string>()),
            CertificationsJson = JsonFieldSerializer.Serialize(content.Certifications ?? Array.Empty<string>()),
            ProjectsJson = JsonFieldSerializer.Serialize(content.Projects ?? Array.Empty<string>())
        };

        _db.CareerPaths.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity;
    }

    /// <summary>
    /// Phân tích payload JSON do dịch vụ sinh nội dung AI trả về thành lộ trình kỹ năng
    /// kèm chứng chỉ/dự án. Yêu cầu nội dung không rỗng và có ít nhất một kỹ năng (R10.2).
    /// </summary>
    /// <exception cref="CareerGenerationException">Khi payload rỗng hoặc không hợp lệ.</exception>
    private static GeneratedCareerContent ParseContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new CareerGenerationException(
                "Dịch vụ AI không sinh được nội dung lộ trình nghề nghiệp cho nghề đã chọn.");
        }

        GeneratedCareerContent? parsed;
        try
        {
            parsed = JsonFieldSerializer.Deserialize<GeneratedCareerContent>(content);
        }
        catch (Exception ex) when (ex is not CareerGenerationException)
        {
            throw new CareerGenerationException(
                "Nội dung lộ trình nghề nghiệp do dịch vụ AI trả về không hợp lệ (không phân tích được JSON).");
        }

        if (parsed?.Skills is null || parsed.Skills.Count == 0)
        {
            throw new CareerGenerationException(
                "Dịch vụ AI không sinh được lộ trình kỹ năng nào cho nghề đã chọn.");
        }

        return parsed;
    }
}
