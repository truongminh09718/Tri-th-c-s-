using AiLearningPath.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AiLearningPath.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/system")]
[Produces("application/json")]
public sealed class SystemController : ControllerBase
{
    private readonly GeminiOptions _geminiOptions;
    private readonly MlServiceOptions _mlOptions;
    private readonly AiOptions _aiOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public SystemController(
        IOptions<GeminiOptions> geminiOptions,
        IOptions<MlServiceOptions> mlOptions,
        IOptions<AiOptions> aiOptions,
        IHttpClientFactory httpClientFactory)
    {
        _geminiOptions = geminiOptions.Value;
        _mlOptions = mlOptions.Value;
        _aiOptions = aiOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("ai-status")]
    [ProducesResponseType(typeof(AiStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAiStatus(CancellationToken cancellationToken)
    {
        var geminiConfigured = GeminiOptions.IsConfigured(_geminiOptions);
        var mlConfigured = MlServiceOptions.IsConfigured(_mlOptions);
        var mlReady = mlConfigured
            ? await CheckMlReadyAsync(cancellationToken)
            : new ExternalDependencyStatus(false, "fallback", "ML service endpoint is not configured.");

        var response = new AiStatusResponse(
            Gemini: new ExternalDependencyStatus(
                geminiConfigured,
                geminiConfigured ? "configured" : "fallback",
                geminiConfigured ? "Gemini endpoint and API key are configured." : "Gemini is not configured; deterministic fallback is active."),
            MlService: mlReady,
            FallbackMode: new FallbackModeStatus(
                ContentFallbackActive: !geminiConfigured,
                PredictionFallbackActive: !mlConfigured),
            Cache: new AiCacheStatus(_aiOptions.EnableCache, _aiOptions.CacheTtlMinutes));

        return Ok(response);
    }

    private async Task<ExternalDependencyStatus> CheckMlReadyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(2));

            var endpoint = _mlOptions.Endpoint!.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/ready");
            if (!string.IsNullOrWhiteSpace(_mlOptions.ApiKey))
            {
                request.Headers.Add("X-ML-Service-Key", _mlOptions.ApiKey);
            }

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, timeoutCts.Token);
            return response.IsSuccessStatusCode
                ? new ExternalDependencyStatus(true, "ready", "ML service readiness check passed.")
                : new ExternalDependencyStatus(true, "unready", $"ML service returned HTTP {(int)response.StatusCode}.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ExternalDependencyStatus(true, "timeout", "ML service readiness check timed out.");
        }
        catch (Exception ex)
        {
            return new ExternalDependencyStatus(true, "unreachable", ex.Message);
        }
    }
}

public sealed record AiStatusResponse(
    ExternalDependencyStatus Gemini,
    ExternalDependencyStatus MlService,
    FallbackModeStatus FallbackMode,
    AiCacheStatus Cache);

public sealed record ExternalDependencyStatus(bool Configured, string State, string Detail);

public sealed record FallbackModeStatus(bool ContentFallbackActive, bool PredictionFallbackActive);

public sealed record AiCacheStatus(bool Enabled, int TtlMinutes);
