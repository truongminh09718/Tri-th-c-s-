using AiLearningPath.Application.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AiLearningPath.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("ai")]
[Route("api/users/{userId:guid}/ai")]
[Produces("application/json")]
public sealed class AiController : ControllerBase
{
    private readonly IAiTutorService _tutorService;
    private readonly IAiInsightService _insightService;
    private readonly IAiFeedbackService _feedbackService;

    public AiController(
        IAiTutorService tutorService,
        IAiInsightService insightService,
        IAiFeedbackService feedbackService)
    {
        _tutorService = tutorService;
        _insightService = insightService;
        _feedbackService = feedbackService;
    }

    [HttpPost("tutor/messages")]
    [ProducesResponseType(typeof(TutorMessageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendTutorMessage(
        Guid userId,
        [FromBody] TutorMessageRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _tutorService.SendMessageAsync(userId, request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("insights/dashboard")]
    [ProducesResponseType(typeof(DashboardInsightResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardInsight(Guid userId, CancellationToken cancellationToken)
    {
        var response = await _insightService.GetDashboardInsightAsync(userId, cancellationToken);
        return Ok(response);
    }

    [HttpPost("feedback")]
    [ProducesResponseType(typeof(AiFeedbackResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> SaveFeedback(
        Guid userId,
        [FromBody] AiFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _feedbackService.SaveFeedbackAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
