using AiLearningPath.Application.Ai;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

public sealed class AiTutorService : IAiTutorService
{
    private readonly AppDbContext _db;
    private readonly IAiGateway _aiGateway;

    public AiTutorService(AppDbContext db, IAiGateway aiGateway)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _aiGateway = aiGateway ?? throw new ArgumentNullException(nameof(aiGateway));
    }

    public async Task<TutorMessageResponse> SendMessageAsync(
        Guid userId,
        TutorMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new InvalidOperationException("Tutor message must not be empty.");
        }

        var profile = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        var goal = profile?.LearningGoal ?? "General";
        var context = await BuildContextAsync(userId, cancellationToken);

        var conversation = request.ConversationId is Guid conversationId
            ? await _db.TutorConversations.FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId, cancellationToken)
            : null;

        if (conversation is null)
        {
            conversation = new TutorConversation
            {
                UserId = userId,
                Title = request.Message.Length > 60 ? request.Message[..60] : request.Message
            };
            _db.TutorConversations.Add(conversation);
        }

        _db.TutorMessages.Add(new TutorMessage
        {
            TutorConversationId = conversation.Id,
            UserId = userId,
            Role = "user",
            Content = request.Message
        });

        var aiResult = await _aiGateway.GenerateAsync(
            new AiGatewayRequest(
                userId,
                AiOperations.TutorAnswer,
                goal,
                new Dictionary<string, string>
                {
                    ["question"] = request.Message,
                    ["context"] = context
                }),
            cancellationToken);

        var answer = JsonFieldSerializer.Deserialize<GeneratedTutorAnswer>(aiResult.Content)
            ?? new GeneratedTutorAnswer(aiResult.Content, Array.Empty<string>(), Array.Empty<string>(), 0.5);

        var assistant = new TutorMessage
        {
            TutorConversationId = conversation.Id,
            UserId = userId,
            Role = "assistant",
            Content = answer.Answer,
            Provider = aiResult.Provider,
            UsedFallback = aiResult.UsedFallback
        };
        _db.TutorMessages.Add(assistant);
        await _db.SaveChangesAsync(cancellationToken);

        return new TutorMessageResponse(
            conversation.Id,
            assistant.Id,
            answer.Answer,
            answer.Resources ?? Array.Empty<string>(),
            answer.RelatedTasks ?? Array.Empty<string>(),
            Math.Clamp(answer.Confidence, 0d, 1d),
            aiResult.UsedFallback,
            aiResult.Provider);
    }

    private async Task<string> BuildContextAsync(Guid userId, CancellationToken cancellationToken)
    {
        var assessment = await _db.AssessmentResults.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.Score)
            .FirstOrDefaultAsync(cancellationToken);

        var dna = await _db.LearningDnaProfiles.AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        var tasks = await _db.LearningPaths.AsNoTracking()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .SelectMany(p => p.Phases.SelectMany(ph => ph.Tasks))
            .Take(5)
            .Select(t => t.Skill + ": " + t.Description)
            .ToListAsync(cancellationToken);

        return $"assessmentScore={assessment?.Score}; level={assessment?.Level}; dnaStyle={dna?.LearningStyle}; tasks={string.Join(" | ", tasks)}";
    }
}
