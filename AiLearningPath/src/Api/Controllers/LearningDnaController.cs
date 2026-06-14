using AiLearningPath.Application.LearningDna;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiLearningPath.Api.Controllers;

/// <summary>
/// Endpoint xem Learning DNA Profile của sinh viên (R6.4).
/// Mọi route đều gắn <c>{userId}</c> để áp dụng ownership-based authorization (R14):
/// middleware <c>OwnershipAuthorizationMiddleware</c> đảm bảo người gọi chỉ truy cập
/// được hồ sơ của chính mình (401 nếu chưa xác thực, 403 nếu truy cập tài khoản khác).
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/learning-dna")]
[Produces("application/json")]
public sealed class LearningDnaController : ControllerBase
{
    private readonly ILearningDnaEngine _learningDnaEngine;

    public LearningDnaController(ILearningDnaEngine learningDnaEngine)
    {
        _learningDnaEngine = learningDnaEngine ?? throw new ArgumentNullException(nameof(learningDnaEngine));
    }

    /// <summary>Xem Learning DNA Profile hiện tại của sinh viên (R6.4).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(LearningDnaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _learningDnaEngine.GetAsync(userId, cancellationToken);
        return Ok(LearningDnaResponse.From(profile));
    }
}

/// <summary>
/// Biểu diễn Learning DNA Profile trả về cho client. Các trường danh sách được
/// deserialize từ JSON lưu trong entity (R6.2) sang dạng mảng thân thiện với client.
/// </summary>
public sealed record LearningDnaResponse(
    Guid Id,
    Guid UserId,
    string LearningStyle,
    IReadOnlyList<string> EffectiveHours,
    double LearningSpeed,
    double FocusAbility,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Weaknesses)
{
    public static LearningDnaResponse From(LearningDnaProfile profile) => new(
        profile.Id,
        profile.UserId,
        profile.LearningStyle,
        JsonFieldSerializer.Deserialize<List<string>>(profile.EffectiveHoursJson) ?? new List<string>(),
        profile.LearningSpeed,
        profile.FocusAbility,
        JsonFieldSerializer.Deserialize<List<string>>(profile.StrengthsJson) ?? new List<string>(),
        JsonFieldSerializer.Deserialize<List<string>>(profile.WeaknessesJson) ?? new List<string>());
}
