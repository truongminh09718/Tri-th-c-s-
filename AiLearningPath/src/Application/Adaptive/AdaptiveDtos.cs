namespace AiLearningPath.Application.Adaptive;

public interface IAdaptiveLearningService
{
    Task<AdaptivePathResponse> AdaptAsync(
        Guid userId,
        Guid pathId,
        AdaptivePathRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record AdaptivePathRequest(
    string? Reason = null,
    IReadOnlyList<string>? WeakSkills = null,
    IReadOnlyList<string>? ProgressSignals = null);

public sealed record AdaptivePathResponse(
    Guid EventId,
    Guid PathId,
    string Summary,
    string Rationale,
    IReadOnlyList<string> AddedTasks,
    IReadOnlyList<string> PrioritizedTasks,
    IReadOnlyList<string> FocusSkills,
    bool UsedFallback,
    bool FromCache,
    string Provider);

public sealed record GeneratedAdaptivePathPatch(
    string Summary,
    IReadOnlyList<string>? AddedTasks,
    IReadOnlyList<string>? FocusSkills);
