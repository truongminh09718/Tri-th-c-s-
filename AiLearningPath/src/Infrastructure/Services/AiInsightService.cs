using AiLearningPath.Application.Ai;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Domain.Common;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

public sealed class AiInsightService : IAiInsightService
{
    private readonly AppDbContext _db;
    private readonly IAiGateway _aiGateway;

    public AiInsightService(AppDbContext db, IAiGateway aiGateway)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _aiGateway = aiGateway ?? throw new ArgumentNullException(nameof(aiGateway));
    }

    public async Task<DashboardInsightResponse> GetDashboardInsightAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        var goal = profile?.LearningGoal ?? "General";
        var latestPath = await _db.LearningPaths.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new { p.TargetDays, p.FeasibilityWarning, TotalTasks = p.Phases.SelectMany(ph => ph.Tasks).Count(), Done = p.Phases.SelectMany(ph => ph.Tasks).Count(t => t.Completed) })
            .FirstOrDefaultAsync(cancellationToken);

        var context = latestPath is null
            ? "No learning path yet."
            : $"targetDays={latestPath.TargetDays}; feasibilityWarning={latestPath.FeasibilityWarning}; completed={latestPath.Done}/{latestPath.TotalTasks}";

        var aiResult = await _aiGateway.GenerateAsync(
            new AiGatewayRequest(
                userId,
                AiOperations.DashboardInsight,
                goal,
                new Dictionary<string, string> { ["context"] = context }),
            cancellationToken);

        var insight = JsonFieldSerializer.Deserialize<GeneratedDashboardInsight>(aiResult.Content)
            ?? new GeneratedDashboardInsight(aiResult.Content, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());

        return new DashboardInsightResponse(
            insight.Summary,
            insight.Risks ?? Array.Empty<string>(),
            insight.Recommendations ?? Array.Empty<string>(),
            insight.NextActions ?? Array.Empty<string>(),
            aiResult.UsedFallback,
            aiResult.FromCache,
            aiResult.Provider);
    }
}
