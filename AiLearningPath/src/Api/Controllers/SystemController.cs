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
            : new ExternalDependencyStatus(false, "fallback", "ML service chưa được cấu hình, hệ thống đang dùng dự đoán dự phòng.");

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
        // Dùng timeout từ cấu hình (MlService:TimeoutSeconds); mặc định 10 giây nếu không hợp lệ.
        var timeoutSeconds = _mlOptions.TimeoutSeconds > 0 ? _mlOptions.TimeoutSeconds : 10;
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var endpoint = _mlOptions.Endpoint!.TrimEnd('/');
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{endpoint}/ready");
            if (!string.IsNullOrWhiteSpace(_mlOptions.ApiKey))
            {
                request.Headers.Add("X-ML-Service-Key", _mlOptions.ApiKey);
            }

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, timeoutCts.Token);
            return response.IsSuccessStatusCode
                ? new ExternalDependencyStatus(true, "ready", "ML service đã sẵn sàng (readiness check passed).")
                : new ExternalDependencyStatus(true, "fallback", $"ML service trả HTTP {(int)response.StatusCode}. Hệ thống đang dùng dự đoán dự phòng.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ExternalDependencyStatus(true, "fallback", $"ML service chưa sẵn sàng (quá {timeoutSeconds}s), hệ thống đang dùng dự đoán dự phòng.");
        }
        catch (Exception ex)
        {
            return new ExternalDependencyStatus(true, "fallback", $"ML service chưa sẵn sàng, hệ thống đang dùng dự đoán dự phòng. ({ex.GetType().Name})");
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
