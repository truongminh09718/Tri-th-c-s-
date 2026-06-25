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
        // Đọc dữ liệu lộ trình KHÔNG tracking. AiGateway.GenerateAsync gọi SaveChangesAsync nội
        // bộ (ghi cache + log); nếu các entity path/phase/task bị track, việc thêm task mới qua
        // navigation collection đã track sẽ gây DbUpdateConcurrencyException. Do đó chỉ đọc dữ
        // liệu cần thiết (AsNoTracking) rồi thêm entity mới trực tiếp qua DbSet với FK rõ ràng.
        var path = await _db.LearningPaths
            .AsNoTracking()
            .Include(p => p.Phases)
            .ThenInclude(ph => ph.Tasks)
            .FirstOrDefaultAsync(p => p.Id == pathId && p.UserId == userId, cancellationToken);

        if (path is null)
        {
            throw new InvalidOperationException("Learning path was not found.");
        }

        var allTasks = path.Phases.SelectMany(p => p.Tasks).ToList();
        var completedCount = allTasks.Count(t => t.Completed);
        var totalCount = allTasks.Count;

        // Ưu tiên kỹ năng yếu do người dùng cung cấp; nếu trống, lấy từ kết quả đánh giá
        // mới nhất để adaptive patch luôn bám sát điểm yếu thật của sinh viên.
        var weakSkills = (request.WeakSkills ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        if (weakSkills.Count == 0)
        {
            var latestWeaknessesJson = await _db.AssessmentResults.AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.Id)
                .Select(r => r.WeaknessesJson)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(latestWeaknessesJson))
            {
                weakSkills = JsonFieldSerializer.Deserialize<List<string>>(latestWeaknessesJson) ?? new List<string>();
            }
        }

        var progressSignals = (request.ProgressSignals ?? Array.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var contextParts = new List<string>
        {
            $"completed={completedCount}/{totalCount}"
        };
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            contextParts.Add($"reason={request.Reason}");
        }
        if (weakSkills.Count > 0)
        {
            contextParts.Add($"weakSkills={string.Join(", ", weakSkills)}");
        }
        if (progressSignals.Count > 0)
        {
            contextParts.Add($"progressSignals={string.Join("; ", progressSignals)}");
        }

        var context = string.Join("; ", contextParts);

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

        // Kỹ năng cần tập trung: ưu tiên do AI đề xuất, bổ sung kỹ năng yếu thật nếu AI để trống.
        var focusSkills = (patch.FocusSkills is { Count: > 0 } ? patch.FocusSkills : weakSkills)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

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

        // Thêm task mới TRỰC TIẾP vào DbSet với FK PathPhaseId (không mutate navigation đã track).
        var firstPhase = path.Phases.OrderBy(p => p.MonthIndex).ThenBy(p => p.WeekIndex).FirstOrDefault();
        if (firstPhase is not null)
        {
            foreach (var task in addedTasks.Take(3))
            {
                _db.LearningTasks.Add(new LearningTask
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

        // Task ưu tiên: các nhiệm vụ hiện có thuộc nhóm kỹ năng cần tập trung (chưa hoàn thành),
        // giúp sinh viên biết nên làm lại/đẩy mạnh phần nào trong lộ trình hiện tại.
        var prioritizedTasks = focusSkills.Count == 0
            ? new List<string>()
            : allTasks
                .Where(t => !t.Completed && focusSkills.Any(skill =>
                    t.Skill.Contains(skill, StringComparison.OrdinalIgnoreCase) ||
                    skill.Contains(t.Skill, StringComparison.OrdinalIgnoreCase)))
                .Select(t => t.Description)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

        // Rationale: ưu tiên Summary do AI sinh; nếu trống thì dựng thông điệp deterministic
        // dựa trên kỹ năng yếu để người dùng luôn nhận được giải thích có ý nghĩa.
        var rationale = !string.IsNullOrWhiteSpace(patch.Summary)
            ? patch.Summary
            : focusSkills.Count > 0
                ? $"Tăng cường luyện tập cho các kỹ năng yếu: {string.Join(", ", focusSkills)}."
                : "Đã tạo adaptation event dựa trên tiến độ hiện tại.";

        return new AdaptivePathResponse(
            eventEntity.Id,
            pathId,
            patch.Summary,
            rationale,
            addedTasks,
            prioritizedTasks,
            focusSkills,
            aiResult.UsedFallback,
            aiResult.FromCache,
            aiResult.Provider);
    }
}
