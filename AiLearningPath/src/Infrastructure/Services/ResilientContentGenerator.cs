using System.Text.Json;
using AiLearningPath.Application.ExternalServices;
using Microsoft.Extensions.Logging;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Decorator dự phòng an toàn cho <see cref="IContentGenerator"/> (R17.2, R17.3): thử gọi
/// adapter Gemini thật (<see cref="GeminiContentGenerator"/>) trước; nếu dịch vụ ngoài lỗi
/// (HTTP lỗi, timeout, hoặc phản hồi không phân tích được), log cảnh báo và TRẢ VỀ kết quả
/// từ <see cref="PlaceholderContentGenerator"/> cho cùng yêu cầu — KHÔNG ném lỗi ra ngoài.
///
/// <para>
/// Nhờ vậy các service lớp Application (assessment / path / career) luôn nhận được payload
/// đúng schema kỳ vọng dù Gemini gặp sự cố, giữ luồng end-to-end hoạt động (R17.4, R17.5).
/// </para>
///
/// <para>
/// Phân biệt hủy: nếu <paramref name="cancellationToken"/> gốc do caller chủ động hủy thì
/// tôn trọng và ném <see cref="OperationCanceledException"/> ra ngoài. Còn timeout NỘI BỘ
/// của adapter Gemini (token liên kết bị hủy do <c>CancelAfter</c>, trong khi token gốc
/// CHƯA bị hủy) được coi là lỗi dịch vụ ngoài → fallback an toàn (Property 24).
/// </para>
/// </summary>
public sealed class ResilientContentGenerator : IContentGenerator
{
    private readonly GeminiContentGenerator _primary;
    private readonly PlaceholderContentGenerator _fallback;
    private readonly ILogger<ResilientContentGenerator> _logger;

    public ResilientContentGenerator(
        GeminiContentGenerator primary,
        PlaceholderContentGenerator fallback,
        ILogger<ResilientContentGenerator> logger)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ContentGenerationResult> GenerateAsync(
        ContentGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return await _primary.GenerateAsync(request, cancellationToken);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            // Caller chủ động hủy: tôn trọng yêu cầu hủy, không fallback.
            _logger.LogDebug(ex, "Yêu cầu sinh nội dung bị hủy bởi caller cho Kind={Kind}.", request.Kind);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            // Timeout nội bộ của adapter Gemini (token gốc chưa hủy) → fallback an toàn.
            _logger.LogWarning(ex,
                "Gemini hết thời gian chờ khi sinh nội dung cho Kind={Kind}; dùng placeholder dự phòng.",
                request.Kind);
            return await _fallback.GenerateAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Gemini trả lỗi HTTP khi sinh nội dung cho Kind={Kind}; dùng placeholder dự phòng.",
                request.Kind);
            return await _fallback.GenerateAsync(request, cancellationToken);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex,
                "Không phân tích được phản hồi Gemini khi sinh nội dung cho Kind={Kind}; dùng placeholder dự phòng.",
                request.Kind);
            return await _fallback.GenerateAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            // Mọi lỗi không lường trước khác từ adapter ngoài cũng phải fallback an toàn,
            // không để rò rỉ lỗi ra các service nghiệp vụ (Property 24).
            _logger.LogWarning(ex,
                "Lỗi không xác định khi gọi Gemini cho Kind={Kind}; dùng placeholder dự phòng.",
                request.Kind);
            return await _fallback.GenerateAsync(request, cancellationToken);
        }
    }
}
