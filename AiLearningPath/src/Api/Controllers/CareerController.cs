using AiLearningPath.Application.Career;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiLearningPath.Api.Controllers;

/// <summary>
/// Endpoint định hướng nghề nghiệp — Career Path AI (R10): liệt kê danh mục nghề nghiệp
/// được hỗ trợ (R10.1) và sinh lộ trình kỹ năng kèm đề xuất chứng chỉ/dự án (R10.2–R10.4).
/// Mọi route đều gắn <c>{userId}</c> để áp dụng ownership-based authorization (R14):
/// middleware <c>OwnershipAuthorizationMiddleware</c> đảm bảo người gọi chỉ sinh/truy cập
/// lộ trình của chính mình (401 nếu chưa xác thực, 403 nếu truy cập tài khoản khác).
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/careers")]
[Produces("application/json")]
public sealed class CareerController : ControllerBase
{
    private readonly ICareerPathService _careerPathService;

    public CareerController(ICareerPathService careerPathService)
    {
        _careerPathService = careerPathService ?? throw new ArgumentNullException(nameof(careerPathService));
    }

    /// <summary>
    /// Liệt kê danh sách nghề nghiệp được hỗ trợ (R10.1): Frontend, Backend, Data Analyst,
    /// AI Engineer, Tester.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(CareerCatalogResponse), StatusCodes.Status200OK)]
    public IActionResult List(Guid userId)
    {
        var careers = _careerPathService.ListCareers();
        return Ok(new CareerCatalogResponse(careers));
    }

    /// <summary>
    /// Sinh lộ trình nghề nghiệp cho một nghề được chọn (R10.2–R10.4): sinh lộ trình kỹ năng
    /// kèm đề xuất chứng chỉ/dự án và lưu liên kết với hồ sơ sinh viên.
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(CareerPathResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Generate(
        Guid userId, [FromBody] GenerateCareerRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Career))
        {
            return BadRequest(ApiErrorResponse.Create(
                "VALIDATION_ERROR", "Nghề nghiệp không được để trống."));
        }

        var careerPath = await _careerPathService.GenerateAsync(userId, request.Career, cancellationToken);
        var response = CareerPathResponse.From(careerPath);

        return StatusCode(StatusCodes.Status201Created, response);
    }
}

/// <summary>Phản hồi danh mục nghề nghiệp được hỗ trợ (R10.1).</summary>
/// <param name="Careers">Danh sách nghề nghiệp được hỗ trợ.</param>
public sealed record CareerCatalogResponse(IReadOnlyList<string> Careers);

/// <summary>Body yêu cầu sinh lộ trình nghề nghiệp.</summary>
/// <param name="Career">Nghề nghiệp được chọn (phải thuộc danh mục hỗ trợ).</param>
public sealed record GenerateCareerRequest(string Career);

/// <summary>
/// Phản hồi lộ trình nghề nghiệp đã sinh (R10.2–R10.4). Các trường danh sách được
/// deserialize từ JSON lưu trong entity sang dạng mảng thân thiện với client.
/// </summary>
public sealed record CareerPathResponse(
    Guid Id,
    Guid UserId,
    string Career,
    IReadOnlyList<string> Skills,
    IReadOnlyList<string> Certifications,
    IReadOnlyList<string> Projects)
{
    public static CareerPathResponse From(CareerPath careerPath) => new(
        careerPath.Id,
        careerPath.UserId,
        careerPath.Career,
        JsonFieldSerializer.Deserialize<List<string>>(careerPath.SkillsJson) ?? new List<string>(),
        JsonFieldSerializer.Deserialize<List<string>>(careerPath.CertificationsJson) ?? new List<string>(),
        JsonFieldSerializer.Deserialize<List<string>>(careerPath.ProjectsJson) ?? new List<string>());
}
