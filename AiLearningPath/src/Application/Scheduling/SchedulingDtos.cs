namespace AiLearningPath.Application.Scheduling;

public interface IStudyScheduleService
{
    Task<StudyScheduleResponse> GenerateAsync(
        Guid userId,
        GenerateStudyScheduleRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record GenerateStudyScheduleRequest(
    int Days = 14,
    string? Deadline = null,
    IReadOnlyList<string>? PreferredWindows = null);

public sealed record StudyScheduleResponse(
    Guid Id,
    Guid UserId,
    DateTime CreatedAt,
    IReadOnlyList<StudyScheduleItemResponse> Items,
    bool UsedFallback,
    bool FromCache,
    string Provider);

public sealed record StudyScheduleItemResponse(
    Guid Id,
    DateTime ScheduledFor,
    string Title,
    string Skill,
    double EstimatedHours,
    Guid? LearningTaskId);

public sealed record GeneratedSchedulePlan(IReadOnlyList<GeneratedScheduleItem>? Items);

public sealed record GeneratedScheduleItem(
    string Title,
    string Skill,
    int DayOffset,
    double EstimatedHours,
    Guid? LearningTaskId);
