using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Scheduling;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

public sealed class StudyScheduleService : IStudyScheduleService
{
    private readonly AppDbContext _db;
    private readonly IAiGateway _aiGateway;

    public StudyScheduleService(AppDbContext db, IAiGateway aiGateway)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _aiGateway = aiGateway ?? throw new ArgumentNullException(nameof(aiGateway));
    }

    public async Task<StudyScheduleResponse> GenerateAsync(
        Guid userId,
        GenerateStudyScheduleRequest request,
        CancellationToken cancellationToken = default)
    {
        var path = await _db.LearningPaths
            .Include(p => p.Phases)
            .ThenInclude(ph => ph.Tasks)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (path is null)
        {
            throw new InvalidOperationException("A learning path is required before generating a study schedule.");
        }

        var openTasks = path.Phases
            .OrderBy(p => p.MonthIndex).ThenBy(p => p.WeekIndex)
            .SelectMany(p => p.Tasks)
            .Where(t => !t.Completed)
            .Take(20)
            .Select(t => $"{t.Id}|{t.Skill}|{t.Description}|{t.EstimatedHours}")
            .ToList();

        var aiResult = await _aiGateway.GenerateAsync(
            new AiGatewayRequest(
                userId,
                AiOperations.SchedulePlan,
                path.LearningGoal,
                new Dictionary<string, string>
                {
                    ["context"] = $"days={request.Days}; deadline={request.Deadline}; windows={string.Join(",", request.PreferredWindows ?? Array.Empty<string>())}; tasks={string.Join(";", openTasks)}"
                }),
            cancellationToken);

        var plan = JsonFieldSerializer.Deserialize<GeneratedSchedulePlan>(aiResult.Content)
            ?? new GeneratedSchedulePlan(Array.Empty<GeneratedScheduleItem>());

        var schedule = new StudySchedule
        {
            UserId = userId,
            UsedFallback = aiResult.UsedFallback,
            FromCache = aiResult.FromCache,
            Provider = aiResult.Provider
        };

        foreach (var item in plan.Items ?? Array.Empty<GeneratedScheduleItem>())
        {
            schedule.Items.Add(new StudyScheduleItem
            {
                LearningTaskId = item.LearningTaskId,
                ScheduledFor = DateTime.UtcNow.Date.AddDays(Math.Max(0, item.DayOffset)),
                Title = string.IsNullOrWhiteSpace(item.Title) ? item.Skill : item.Title,
                Skill = item.Skill,
                EstimatedHours = item.EstimatedHours <= 0 ? 1 : item.EstimatedHours
            });
        }

        _db.StudySchedules.Add(schedule);
        await _db.SaveChangesAsync(cancellationToken);

        return new StudyScheduleResponse(
            schedule.Id,
            schedule.UserId,
            schedule.CreatedAt,
            schedule.Items.Select(i => new StudyScheduleItemResponse(i.Id, i.ScheduledFor, i.Title, i.Skill, i.EstimatedHours, i.LearningTaskId)).ToList(),
            schedule.UsedFallback,
            schedule.FromCache,
            schedule.Provider);
    }
}
