using AiLearningPath.Application.Profiles;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiLearningPath.Api.Controllers;

/// <summary>
/// Endpoint quản lý hồ sơ cá nhân và lựa chọn Learning Goal (R3, R4).
/// Mọi route đều gắn <c>{userId}</c> để áp dụng ownership-based authorization (R14):
/// middleware <c>OwnershipAuthorizationMiddleware</c> đảm bảo người gọi chỉ truy cập
/// được hồ sơ của chính mình (401 nếu chưa xác thực, 403 nếu truy cập tài khoản khác).
/// </summary>
[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/profile")]
[Produces("application/json")]
public sealed class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
    }

    /// <summary>Xem hồ sơ hiện tại của sinh viên (R3.2).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken)
    {
        var profile = await _profileService.GetAsync(userId, cancellationToken);
        return Ok(ProfileResponse.From(profile));
    }

    /// <summary>Tạo mới hoặc cập nhật toàn bộ hồ sơ (R3.1).</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrUpdate(
        Guid userId, [FromBody] ProfileInput input, CancellationToken cancellationToken)
    {
        var profile = await _profileService.CreateOrUpdateAsync(userId, input, cancellationToken);
        return Ok(ProfileResponse.From(profile));
    }

    /// <summary>Cập nhật một trường hồ sơ với giá trị hợp lệ (R3.3, R3.4).</summary>
    [HttpPatch]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateField(
        Guid userId, [FromBody] UpdateFieldRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrEmpty(request.Field))
        {
            return BadRequest(ApiErrorResponse.Create(
                "VALIDATION_ERROR", "Tên trường cập nhật không được để trống."));
        }

        var profile = await _profileService.UpdateFieldAsync(
            userId, request.Field, request.Value, cancellationToken);
        return Ok(ProfileResponse.From(profile));
    }

    /// <summary>Chọn Learning Goal cho hồ sơ, kèm điểm mục tiêu khi cần (R4.2, R4.3).</summary>
    [HttpPut("goal")]
    [ProducesResponseType(typeof(ProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SelectGoal(
        Guid userId, [FromBody] LearningGoalSelection selection, CancellationToken cancellationToken)
    {
        var profile = await _profileService.SelectGoalAsync(userId, selection, cancellationToken);
        return Ok(ProfileResponse.From(profile));
    }
}

/// <summary>Body cho yêu cầu cập nhật một trường hồ sơ.</summary>
/// <param name="Field">Tên trường cần cập nhật (ví dụ: StudyHoursPerDay, LearningGoal).</param>
/// <param name="Value">Giá trị mới của trường.</param>
public sealed record UpdateFieldRequest(string Field, object? Value);

/// <summary>Biểu diễn hồ sơ trả về cho client (không lộ chi tiết quan hệ điều hướng).</summary>
public sealed record ProfileResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string StudentCode,
    string Major,
    string LearningGoal,
    int? TargetScore,
    string CareerGoal,
    double StudyHoursPerDay)
{
    public static ProfileResponse From(Profile profile) => new(
        profile.Id,
        profile.UserId,
        profile.FullName,
        profile.StudentCode,
        profile.Major,
        profile.LearningGoal,
        profile.TargetScore,
        profile.CareerGoal,
        profile.StudyHoursPerDay);
}
