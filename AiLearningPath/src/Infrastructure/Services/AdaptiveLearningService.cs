using AiLearningPath.Application.Adaptive;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

public sealed class AdaptiveLearningService : IAdaptiveLearningService
{
    private readonly AppDbContext _db;
    private readonly IAiGateway _aiGateway;

    public AdaptiveLearningService(AppDbContext db, IAiGateway aiGateway)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _aiGateway = aiGateway ?? throw new ArgumentNullException(nameof(aiGateway));
    }

    public async Task<AdaptivePathResponse> AdaptAsync(
        Guid userId,
        Guid pathId,
        AdaptivePathRequest request,
        CancellationToken cancellationToken = default)
    {
        var path = await _db.LearningPaths
            .Include(p => p.Phases)
            .ThenInclude(ph => ph.Tasks)
            .FirstOrDefaultAsync(p => p.Id == pathId && p.UserId == userId, cancellationToken);

        if (path is null)
        {
            throw new InvalidOperationException("Learning path was not found.");
        }

        var context = $"completed={path.Phases.SelectMany(p => p.Tasks).Count(t => t.Completed)}/{path.Phases.SelectMany(p => p.Tasks).Count()}; reason={request.Reason}";

        var aiResult = await _aiGateway.GenerateAsync(
            new AiGatewayRequest(
                userId,
                AiOperations.AdaptivePathPatch,
                path.LearningGoal,
                new Dictionary<string, string> { ["context"] = context }),
            cancellationToken);

        var patch = JsonFieldSerializer.Deserialize<GeneratedAdaptivePathPatch>(aiResult.Content)
            ?? new GeneratedAdaptivePathPatch("No adaptive changes were generated.", Array.Empty<string>(), Array.Empty<string>());

        var addedTasks = patch.AddedTasks ?? Array.Empty<string>();
        var focusSkills = patch.FocusSkills ?? Array.Empty<string>();

        var eventEntity = new AdaptationEvent
        {
            UserId = userId,
            LearningPathId = pathId,
            Summary = patch.Summary,
            AddedTasksJson = JsonFieldSerializer.Serialize(addedTasks),
            FocusSkillsJson = JsonFieldSerializer.Serialize(focusSkills),
            UsedFallback = aiResult.UsedFallback,
            FromCache = aiResult.FromCache,
            Provider = aiResult.Provider
        };

        _db.AdaptationEvents.Add(eventEntity);

        var firstPhase = path.Phases.OrderBy(p => p.MonthIndex).ThenBy(p => p.WeekIndex).FirstOrDefault();
        if (firstPhase is not null)
        {
            foreach (var task in addedTasks.Take(3))
            {
                firstPhase.Tasks.Add(new LearningTask
                {
                    PathPhaseId = firstPhase.Id,
                    Skill = focusSkills.FirstOrDefault() ?? path.LearningGoal,
                    Description = task,
                    EstimatedHours = 1,
                    Completed = false
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new AdaptivePathResponse(
            eventEntity.Id,
            pathId,
            patch.Summary,
            addedTasks,
            focusSkills,
            aiResult.UsedFallback,
            aiResult.FromCache,
            aiResult.Provider);
    }
}
