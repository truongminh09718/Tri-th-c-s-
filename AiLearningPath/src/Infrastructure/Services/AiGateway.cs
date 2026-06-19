using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AiLearningPath.Application.Ai;
using AiLearningPath.Application.Adaptive;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Scheduling;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiLearningPath.Infrastructure.Services;

public sealed class AiGateway : IAiGateway
{
    private readonly AppDbContext _db;
    private readonly GeminiContentGenerator _gemini;
    private readonly PlaceholderContentGenerator _placeholder;
    private readonly GeminiOptions _geminiOptions;
    private readonly AiOptions _aiOptions;
    private readonly ILogger<AiGateway> _logger;

    public AiGateway(
        AppDbContext db,
        GeminiContentGenerator gemini,
        PlaceholderContentGenerator placeholder,
        IOptions<GeminiOptions> geminiOptions,
        IOptions<AiOptions> aiOptions,
        ILogger<AiGateway> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _gemini = gemini ?? throw new ArgumentNullException(nameof(gemini));
        _placeholder = placeholder ?? throw new ArgumentNullException(nameof(placeholder));
        _geminiOptions = geminiOptions.Value;
        _aiOptions = aiOptions.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AiGatewayResult> GenerateAsync(
        AiGatewayRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var correlationId = Guid.NewGuid().ToString("N");
        var sw = Stopwatch.StartNew();
        var cacheKey = BuildCacheKey(request);

        if (_aiOptions.EnableCache)
        {
            var cached = await _db.AiCacheEntries
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CacheKey == cacheKey && c.ExpiresAt > DateTime.UtcNow, cancellationToken);

            if (cached is not null)
            {
                sw.Stop();
                await LogAsync(request, "cache", "cache-hit", null, sw.ElapsedMilliseconds, correlationId, cancellationToken);
                return new AiGatewayResult(cached.Content, "cache", true, false, null, sw.ElapsedMilliseconds, correlationId);
            }
        }

        var contentRequest = new ContentGenerationRequest(
            request.Operation,
            request.Subject,
            request.Parameters);

        string provider;
        string content;
        bool usedFallback;
        string? fallbackReason;

        try
        {
            if (!GeminiOptions.IsConfigured(_geminiOptions))
            {
                provider = "placeholder";
                usedFallback = true;
                fallbackReason = "gemini-unconfigured";
                content = await GenerateFallbackAsync(contentRequest, cancellationToken);
            }
            else
            {
                var generated = await _gemini.GenerateAsync(contentRequest, cancellationToken);
                provider = "gemini";
                usedFallback = false;
                fallbackReason = null;
                content = TrimResponse(generated.Content);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI gateway fallback for operation {Operation}.", request.Operation);
            provider = "placeholder";
            usedFallback = true;
            fallbackReason = ex.GetType().Name;
            content = await GenerateFallbackAsync(contentRequest, cancellationToken);
        }

        sw.Stop();

        if (_aiOptions.EnableCache)
        {
            await UpsertCacheAsync(cacheKey, request, content, cancellationToken);
        }

        await LogAsync(
            request,
            provider,
            usedFallback ? "fallback" : "success",
            fallbackReason,
            sw.ElapsedMilliseconds,
            correlationId,
            cancellationToken);

        return new AiGatewayResult(
            content,
            provider,
            FromCache: false,
            UsedFallback: usedFallback,
            FallbackReason: fallbackReason,
            LatencyMs: sw.ElapsedMilliseconds,
            CorrelationId: correlationId);
    }

    private async Task<string> GenerateFallbackAsync(
        ContentGenerationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Kind is AiOperations.AssessmentQuestions or AiOperations.LearningPathContent or AiOperations.CareerPath)
        {
            var legacyKind = request.Kind switch
            {
                AiOperations.AssessmentQuestions => "Assessment",
                AiOperations.LearningPathContent => "LearningPath",
                AiOperations.CareerPath => "CareerPath",
                _ => request.Kind
            };
            return (await _placeholder.GenerateAsync(request with { Kind = legacyKind }, cancellationToken)).Content;
        }

        var subject = string.IsNullOrWhiteSpace(request.LearningGoal) ? "learning goal" : request.LearningGoal;
        return request.Kind switch
        {
            AiOperations.TutorAnswer => JsonFieldSerializer.Serialize(new GeneratedTutorAnswer(
                $"Fallback tutor answer for {subject}: review your current weak skills, finish today's task, then ask a narrower question.",
                new[] { "Current learning path", "Assessment review", "Practice tasks" },
                new[] { "Review weak skill", "Complete next task" },
                0.62)),
            AiOperations.DashboardInsight => JsonFieldSerializer.Serialize(new GeneratedDashboardInsight(
                $"You are progressing toward {subject}. Focus on consistency and weak skills this week.",
                new[] { "Completion may slow down if study sessions are skipped." },
                new[] { "Complete at least one task today.", "Review the weakest skill from your assessment." },
                new[] { "Open Learning Path", "Simulate Academic Twin" })),
            AiOperations.SchedulePlan => JsonFieldSerializer.Serialize(new GeneratedSchedulePlan(
                Enumerable.Range(0, 7)
                    .Select(i => new GeneratedScheduleItem($"Study block {i + 1}", subject, i, 1.5, null))
                    .ToList())),
            AiOperations.AdaptivePathPatch => JsonFieldSerializer.Serialize(new GeneratedAdaptivePathPatch(
                "Fallback adaptation keeps the existing path and adds focused review tasks.",
                new[] { "Focused review session", "Short practice quiz", "Reflection checkpoint" },
                new[] { "Weak skill review", subject })),
            _ => $"Fallback content for {subject}."
        };
    }

    private string TrimResponse(string content)
    {
        if (content.Length <= _aiOptions.MaxResponseChars)
        {
            return content;
        }

        return content[.._aiOptions.MaxResponseChars];
    }

    private async Task UpsertCacheAsync(
        string cacheKey,
        AiGatewayRequest request,
        string content,
        CancellationToken cancellationToken)
    {
        var existing = await _db.AiCacheEntries.FirstOrDefaultAsync(c => c.CacheKey == cacheKey, cancellationToken);
        if (existing is null)
        {
            existing = new AiCacheEntry { CacheKey = cacheKey };
            _db.AiCacheEntries.Add(existing);
        }

        existing.Operation = request.Operation;
        existing.SchemaVersion = request.SchemaVersion;
        existing.Content = content;
        existing.CreatedAt = DateTime.UtcNow;
        existing.ExpiresAt = DateTime.UtcNow.AddMinutes(Math.Max(1, _aiOptions.CacheTtlMinutes));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task LogAsync(
        AiGatewayRequest request,
        string provider,
        string status,
        string? fallbackReason,
        long latencyMs,
        string correlationId,
        CancellationToken cancellationToken)
    {
        _db.AiInteractionLogs.Add(new AiInteractionLog
        {
            UserId = request.UserId,
            Operation = request.Operation,
            Provider = provider,
            Status = status,
            FallbackReason = fallbackReason,
            LatencyMs = latencyMs,
            CorrelationId = correlationId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string BuildCacheKey(AiGatewayRequest request)
    {
        var payload = JsonSerializer.Serialize(new
        {
            request.Operation,
            request.Subject,
            request.Parameters,
            request.SchemaVersion
        });
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash);
    }
}
