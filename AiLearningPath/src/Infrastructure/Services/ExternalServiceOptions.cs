namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Configuration for Gemini-backed content generation.
/// Empty ApiKey/Endpoint means the real provider is not configured and the app should use deterministic fallback.
/// </summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string? ApiKey { get; set; }

    public string? Endpoint { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    public static bool IsConfigured(GeminiOptions o)
        => o is not null
           && !string.IsNullOrWhiteSpace(o.ApiKey)
           && !string.IsNullOrWhiteSpace(o.Endpoint);
}

/// <summary>
/// Configuration for the Python ML prediction service.
/// Empty Endpoint means the real service is not configured and the app should use deterministic fallback.
/// </summary>
public sealed class MlServiceOptions
{
    public const string SectionName = "MlService";

    public string? Endpoint { get; set; }

    /// <summary>
    /// Optional internal key sent to the ML service as X-ML-Service-Key.
    /// Leave empty in local development; require it in Production when Endpoint is configured.
    /// </summary>
    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 10;

    public static bool IsConfigured(MlServiceOptions o)
        => o is not null
           && !string.IsNullOrWhiteSpace(o.Endpoint);
}
