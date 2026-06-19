using AiLearningPath.Application.Study;
using AiLearningPath.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AiLearningPath.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users/{userId:guid}/study-lessons")]
[Produces("application/json")]
public sealed class StudyLessonController : ControllerBase
{
    private readonly IStudyLessonService _lessons;

    public StudyLessonController(IStudyLessonService lessons)
    {
        _lessons = lessons ?? throw new ArgumentNullException(nameof(lessons));
    }

    [HttpGet("today")]
    [ProducesResponseType(typeof(TodayLessonView), StatusCodes.Status200OK)]
    public async Task<IActionResult> Today(
        Guid userId,
        [FromQuery] Guid? taskId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _lessons.GetTodayAsync(userId, taskId, cancellationToken));
        }
        catch (StudyLessonTaskNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.Create("NOT_FOUND", ex.Message));
        }
    }

    [HttpPost("{taskId:guid}/complete")]
    [ProducesResponseType(typeof(CompleteLessonResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Complete(
        Guid userId,
        Guid taskId,
        [FromBody] CompleteLessonRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _lessons.CompleteAsync(userId, taskId, request, cancellationToken));
        }
        catch (StudyLessonValidationException ex)
        {
            return BadRequest(ApiErrorResponse.Create("VALIDATION_ERROR", ex.Message));
        }
        catch (StudyLessonTaskNotFoundException ex)
        {
            return NotFound(ApiErrorResponse.Create("NOT_FOUND", ex.Message));
        }
    }
}
