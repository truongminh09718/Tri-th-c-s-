using AiLearningPath.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AiLearningPath.Api.Controllers;

[ApiController]
[Route("api/aiprediction")]
[Produces("application/json")]
public sealed class AiPredictionController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiPredictionController> _logger;

    public AiPredictionController(IHttpClientFactory httpClientFactory, ILogger<AiPredictionController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AiPredictionClient");
        _logger = logger;
    }

    [HttpPost("predict")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Predict([FromBody] AiPredictionRequest request)
    {
        var payload = JsonSerializer.Serialize(new
        {
            study_hours_per_day = request.StudyHoursPerDay,
            attendance_rate = request.AttendanceRate,
            mock_test_score = request.MockTestScore,
            historical_gpa = request.HistoricalGpa
        });

        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/predict", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("AiPrediction backend returned {StatusCode}: {Body}", response.StatusCode, responseBody);
            return StatusCode((int)response.StatusCode, responseBody);
        }

        return Content(responseBody, "application/json");
    }

    [HttpPost("twin-simulation")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> SimulateTwin([FromBody] AiPredictionRequest request)
    {
        var payload = JsonSerializer.Serialize(new
        {
            study_hours_per_day = request.StudyHoursPerDay,
            attendance_rate = request.AttendanceRate,
            mock_test_score = request.MockTestScore,
            historical_gpa = request.HistoricalGpa
        });

        using var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("api/twin-simulation", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("AiPrediction backend returned {StatusCode}: {Body}", response.StatusCode, responseBody);
            return StatusCode((int)response.StatusCode, responseBody);
        }

        return Content(responseBody, "application/json");
    }
}
