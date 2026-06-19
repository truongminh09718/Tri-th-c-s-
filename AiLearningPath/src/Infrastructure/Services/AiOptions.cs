namespace AiLearningPath.Infrastructure.Services;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public bool EnableCache { get; set; } = true;
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int MaxPromptChars { get; set; } = 8000;
    public int MaxResponseChars { get; set; } = 20000;
    public bool EnableFallbackInProduction { get; set; } = true;
    public string Provider { get; set; } = "Gemini";
    public int CacheTtlMinutes { get; set; } = 30;
}
