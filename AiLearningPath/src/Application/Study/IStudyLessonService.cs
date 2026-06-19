namespace AiLearningPath.Application.Study;

public interface IStudyLessonService
{
    Task<TodayLessonView> GetTodayAsync(
        Guid userId,
        Guid? taskId = null,
        CancellationToken cancellationToken = default);

    Task<CompleteLessonResult> CompleteAsync(
        Guid userId,
        Guid taskId,
        CompleteLessonRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record TodayLessonView(
    string Status,
    string Message,
    LessonDetail? Lesson);

public sealed record LessonDetail(
    Guid TaskId,
    string LearningGoal,
    string PhaseTitle,
    string Skill,
    string Description,
    double EstimatedHours,
    IReadOnlyList<LessonTaskItem> Tasks,
    IReadOnlyList<LessonMaterial> Materials,
    IReadOnlyList<LessonQuiz> Quizzes);

public sealed record LessonTaskItem(Guid Id, string Skill, string Description, bool IsToday);

public sealed record LessonMaterial(string Type, string Title, string Description);

public sealed record LessonQuiz(
    string Question,
    IReadOnlyList<string> Options,
    int CorrectOption,
    string Explanation);

public sealed record CompleteLessonRequest(
    int DurationMinutes,
    int Rating,
    string? Reflection,
    bool QuizCorrect);

public sealed record CompleteLessonResult(
    Guid TaskId,
    int DurationMinutes,
    int Rating,
    string? Reflection,
    bool QuizCorrect,
    double CompletionRate,
    double LearningScore,
    string Message);

public static class TodayLessonStatuses
{
    public const string Ready = "ready";
    public const string RequiresAssessment = "requiresAssessment";
    public const string RequiresPath = "requiresPath";
    public const string AllCompleted = "allCompleted";
}

public sealed class StudyLessonValidationException : Exception
{
    public StudyLessonValidationException(string message) : base(message) { }
}

public sealed class StudyLessonTaskNotFoundException : Exception
{
    public StudyLessonTaskNotFoundException(Guid taskId)
        : base($"Không tìm thấy nhiệm vụ học tập {taskId}.") { }
}
