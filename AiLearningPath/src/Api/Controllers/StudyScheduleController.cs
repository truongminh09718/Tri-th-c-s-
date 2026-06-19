using AiLearningPath.Application.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AiLearningPath.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("ai")]
[Route("api/users/{userId:guid}/study-schedule")]
[Produces("application/json")]
public sealed class StudyScheduleController : ControllerBase
{
    private readonly IStudyScheduleService _scheduleService;

    public StudyScheduleController(IStudyScheduleService scheduleService)
    {
        _scheduleService = scheduleService;
    }

    [HttpPost("generate")]
    [ProducesResponseType(typeof(StudyScheduleResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Generate(
        Guid userId,
        [FromBody] GenerateStudyScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _scheduleService.GenerateAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
