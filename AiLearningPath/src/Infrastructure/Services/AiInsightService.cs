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

        // Lấy điểm yếu THẬT từ kết quả đánh giá mới nhất của sinh viên (deterministic, không
        // phụ thuộc dịch vụ AI ngoài). Đây là nguồn dữ liệu chính cho phần "Kỹ năng yếu".
        var latestResult = await _db.AssessmentResults.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Id)
            .Select(r => new { r.WeaknessesJson, r.Score })
            .FirstOrDefaultAsync(cancellationToken);

        var assessedWeakSkills = latestResult is null
            ? new List<string>()
            : JsonFieldSerializer.Deserialize<List<string>>(latestResult.WeaknessesJson) ?? new List<string>();

        var context = latestPath is null
            ? "No learning path yet."
            : $"targetDays={latestPath.TargetDays}; feasibilityWarning={latestPath.FeasibilityWarning}; completed={latestPath.Done}/{latestPath.TotalTasks}";

        // Bổ sung điểm yếu đã đánh giá vào ngữ cảnh để AI sinh nhận xét sát thực tế hơn.
        if (assessedWeakSkills.Count > 0)
        {
            context += $"; weakSkills={string.Join(", ", assessedWeakSkills)}";
        }

        var aiResult = await _aiGateway.GenerateAsync(
            new AiGatewayRequest(
                userId,
                AiOperations.DashboardInsight,
                goal,
                new Dictionary<string, string> { ["context"] = context }),
            cancellationToken);

        var insight = JsonFieldSerializer.Deserialize<GeneratedDashboardInsight>(aiResult.Content)
            ?? new GeneratedDashboardInsight(aiResult.Content, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());

        // Ưu tiên điểm yếu thật từ bài đánh giá; nếu chưa có bài đánh giá thì để rỗng kèm
        // thông điệp hướng dẫn ở Summary (không bịa kỹ năng yếu khi chưa có dữ liệu).
        var summary = !string.IsNullOrWhiteSpace(insight.Summary)
            ? insight.Summary
            : "Chưa đủ dữ liệu để phân tích. Hãy hoàn thành bài đánh giá năng lực trước.";

        if (assessedWeakSkills.Count == 0 && latestResult is null)
        {
            summary = "Bạn chưa hoàn thành bài đánh giá năng lực. Hãy làm bài đánh giá để hệ thống xác định kỹ năng yếu của bạn.";
        }

        // Độ tin cậy: cao hơn khi đã có dữ liệu đánh giá thật để dựa vào.
        var confidence = latestResult is null ? 0.4d : 0.85d;

        return new DashboardInsightResponse(
            summary,
            insight.Risks ?? Array.Empty<string>(),
            assessedWeakSkills,
            insight.Recommendations ?? Array.Empty<string>(),
            insight.NextActions ?? Array.Empty<string>(),
            confidence,
            aiResult.UsedFallback,
            aiResult.FromCache,
            aiResult.Provider);
    }
}
