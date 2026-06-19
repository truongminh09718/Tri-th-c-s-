using Microsoft.AspNetCore.Mvc;
using AiLearningPath.Application.Assessments;

namespace AiLearningPath.Api.Controllers;

[ApiController]
[Route("api/ai-engineer")]
public class AiEngineerController : ControllerBase
{
    private readonly AiEngineerService _service = new();

    [HttpGet("test")]

    public IActionResult GetTest([FromQuery] string goal = "TOEIC")
    {
        return Ok(_service.GetAssessmentTest(goal));
    }

    [HttpPost("grade")]
public IActionResult Grade([FromBody] AiEngineerGradeRequest request)
{
    return Ok(_service.GradeTest(request));
}
}