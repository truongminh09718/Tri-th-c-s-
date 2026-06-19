namespace AiLearningPath.Application.ExternalServices;

public static class AiOperations
{
    public const string AssessmentQuestions = "AssessmentQuestions";
    public const string LearningPathContent = "LearningPathContent";
    public const string CareerPath = "AiCareerPath";
    public const string TutorAnswer = "TutorAnswer";
    public const string DashboardInsight = "DashboardInsight";
    public const string SchedulePlan = "SchedulePlan";
    public const string AdaptivePathPatch = "AdaptivePathPatch";
}

public sealed record AiGatewayRequest(
    Guid? UserId,
    string Operation,
    string Subject,
    IReadOnlyDictionary<string, string> Parameters,
    string SchemaVersion = "v1");

public sealed record AiGatewayResult(
    string Content,
    string Provider,
    bool FromCache,
    bool UsedFallback,
    string? FallbackReason,
    long LatencyMs,
    string CorrelationId);

public interface IAiGateway
{
    Task<AiGatewayResult> GenerateAsync(
        AiGatewayRequest request,
        CancellationToken cancellationToken = default);
}
