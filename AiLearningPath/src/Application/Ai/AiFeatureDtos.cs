namespace AiLearningPath.Application.Ai;

public interface IAiTutorService
{
    Task<TutorMessageResponse> SendMessageAsync(
        Guid userId,
        TutorMessageRequest request,
        CancellationToken cancellationToken = default);
}

public interface IAiInsightService
{
    Task<DashboardInsightResponse> GetDashboardInsightAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public interface IAiFeedbackService
{
    Task<AiFeedbackResponse> SaveFeedbackAsync(
        Guid userId,
        AiFeedbackRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record TutorMessageRequest(string Message, Guid? ConversationId = null);

public sealed record TutorMessageResponse(
    Guid ConversationId,
    Guid MessageId,
    string Answer,
    IReadOnlyList<string> Resources,
    IReadOnlyList<string> RelatedTasks,
    double Confidence,
    bool UsedFallback,
    string Provider);

public sealed record DashboardInsightResponse(
    string Summary,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> WeakSkills,
    IReadOnlyList<string> Recommendations,
    IReadOnlyList<string> NextActions,
    double Confidence,
    bool UsedFallback,
    bool FromCache,
    string Provider);

public sealed record AiFeedbackRequest(
    string Operation,
    string TargetId,
    int Rating,
    string? Comment);

public sealed record AiFeedbackResponse(Guid Id, DateTime CreatedAt);

public sealed record GeneratedTutorAnswer(
    string Answer,
    IReadOnlyList<string>? Resources,
    IReadOnlyList<string>? RelatedTasks,
    double Confidence);

public sealed record GeneratedDashboardInsight(
    string Summary,
    IReadOnlyList<string>? Risks,
    IReadOnlyList<string>? Recommendations,
    IReadOnlyList<string>? NextActions);
