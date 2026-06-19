using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AiLearningPath.Application.Ai;
using AiLearningPath.Application.Adaptive;
using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.Career;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Paths;
using AiLearningPath.Application.Scheduling;
using AiLearningPath.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Adapter HTTP thật gọi Google Gemini REST API (<c>generateContent</c>) để hiện thực
/// <see cref="IContentGenerator"/> (R17.1). Dùng khi section <c>"Gemini"</c> đã được cấu
/// hình đầy đủ (ApiKey + Endpoint — xem <see cref="GeminiOptions.IsConfigured"/>).
///
/// <para>
/// Quy trình (R17.4, R17.5): dựng prompt theo <see cref="ContentGenerationRequest.Kind"/>
/// và <see cref="ContentGenerationRequest.LearningGoal"/>, gọi Gemini, trích text trả về
/// (<c>candidates[0].content.parts[0].text</c>), rồi CHUẨN HÓA về đúng schema mà
/// <see cref="PlaceholderContentGenerator"/> trả ra để các service lớp Application
/// (<see cref="AssessmentEngine"/>, <see cref="PathGenerator"/>, <see cref="CareerPathService"/>)
/// phân tích đồng nhất:
/// <list type="bullet">
///   <item>Assessment → JSON <see cref="GeneratedAssessmentContent"/>.</item>
///   <item>CareerPath → JSON <see cref="GeneratedCareerContent"/>.</item>
///   <item>LearningPath → plain text.</item>
/// </list>
/// </para>
///
/// <para>
/// Áp timeout cấu hình (<see cref="GeminiOptions.TimeoutSeconds"/>) qua một
/// <see cref="CancellationTokenSource"/> liên kết với token gọi vào. Khi HTTP trả lỗi,
/// <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/> sẽ ném ngoại lệ — adapter này
/// KHÔNG tự nuốt lỗi; lớp decorator dự phòng (<c>ResilientContentGenerator</c>, task 17.3)
/// chịu trách nhiệm fallback sang placeholder (R17.2, R17.3).
/// </para>
/// </summary>
public sealed class GeminiContentGenerator : IContentGenerator
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiContentGenerator> _logger;

    /// <summary>
    /// Tùy chọn đọc text từ payload Gemini: không phân biệt hoa thường để bền với khác biệt
    /// quy ước đặt tên (camelCase của API Gemini vs PascalCase của bản ghi C#).
    /// </summary>
    private static readonly JsonSerializerOptions ResponseReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiContentGenerator(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiContentGenerator> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ContentGenerationResult> GenerateAsync(
        ContentGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var prompt = BuildPrompt(request);
        var rawText = await CallGeminiAsync(prompt, cancellationToken);

        var content = request.Kind switch
        {
            AssessmentContentKind.Assessment => NormalizeAssessment(rawText, request),
            AiOperations.AssessmentQuestions => NormalizeAssessment(rawText, request),
            CareerContentKind.CareerPath => NormalizeCareer(rawText, request),
            AiOperations.CareerPath => NormalizeCareer(rawText, request),
            AiOperations.TutorAnswer => NormalizeTutor(rawText, request),
            AiOperations.DashboardInsight => NormalizeDashboardInsight(rawText, request),
            AiOperations.SchedulePlan => NormalizeSchedule(rawText, request),
            AiOperations.AdaptivePathPatch => NormalizeAdaptive(rawText, request),
            PathContentKind.LearningPath => NormalizeLearningPath(rawText, request),
            AiOperations.LearningPathContent => NormalizeLearningPath(rawText, request),
            _ => NormalizeLearningPath(rawText, request),
        };

        return new ContentGenerationResult(content);
    }

    // ----------------------------------------------------------------------------------
    // Gọi HTTP tới Gemini
    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Gọi Gemini <c>generateContent</c> với prompt cho trước và trích text từ candidate đầu.
    /// Áp timeout cấu hình bằng <see cref="CancellationTokenSource"/> liên kết với
    /// <paramref name="cancellationToken"/>; ném khi HTTP lỗi (R17.4).
    /// </summary>
    private async Task<string> CallGeminiAsync(string prompt, CancellationToken cancellationToken)
    {
        // Body chuẩn Gemini: { "contents": [{ "parts": [{ "text": prompt }] }] }.
        var body = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            }
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, BuildRequestUri())
        {
            Content = JsonContent.Create(body)
        };

        // ApiKey gắn vào header x-goog-api-key (cách khuyến nghị của Google), bổ sung cho
        // trường hợp Endpoint chưa kèm "?key=".
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            requestMessage.Headers.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
        }

        // Liên kết token gọi vào với timeout cấu hình để hủy sau TimeoutSeconds giây.
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        using var response = await _httpClient.SendAsync(requestMessage, timeoutCts.Token);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadAsStringAsync(timeoutCts.Token);
        return ExtractText(payload);
    }

    /// <summary>
    /// Dựng URI yêu cầu từ <see cref="GeminiOptions.Endpoint"/> đã cấu hình (dùng nguyên như
    /// base). Nếu Endpoint chưa kèm tham số <c>key</c>, gắn <c>?key={ApiKey}</c> để tương
    /// thích cả khi máy chủ ưu tiên query string.
    /// </summary>
    private Uri BuildRequestUri()
    {
        return new Uri(_options.Endpoint!.Trim(), UriKind.RelativeOrAbsolute);
    }

    /// <summary>
    /// Trích text trả về từ response Gemini: <c>candidates[0].content.parts[0].text</c>
    /// (nối tất cả parts nếu có nhiều). Trả về chuỗi rỗng khi không tìm thấy text.
    /// </summary>
    private string ExtractText(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.TryGetProperty("candidates", out var candidates) &&
                candidates.ValueKind == JsonValueKind.Array &&
                candidates.GetArrayLength() > 0)
            {
                var first = candidates[0];
                if (first.TryGetProperty("content", out var contentEl) &&
                    contentEl.TryGetProperty("parts", out var parts) &&
                    parts.ValueKind == JsonValueKind.Array)
                {
                    var sb = new StringBuilder();
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var textEl) &&
                            textEl.ValueKind == JsonValueKind.String)
                        {
                            sb.Append(textEl.GetString());
                        }
                    }

                    return sb.ToString();
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Không phân tích được payload phản hồi từ Gemini.");
        }

        return string.Empty;
    }

    // ----------------------------------------------------------------------------------
    // Dựng prompt theo Kind
    // ----------------------------------------------------------------------------------

    private static string BuildPrompt(ContentGenerationRequest request) => request.Kind switch
    {
        AssessmentContentKind.Assessment => BuildAssessmentPrompt(request),
        AiOperations.AssessmentQuestions => BuildAssessmentPrompt(request),
        CareerContentKind.CareerPath => BuildCareerPrompt(request),
        AiOperations.CareerPath => BuildCareerPrompt(request),
        AiOperations.TutorAnswer => BuildTutorPrompt(request),
        AiOperations.DashboardInsight => BuildDashboardInsightPrompt(request),
        AiOperations.SchedulePlan => BuildSchedulePrompt(request),
        AiOperations.AdaptivePathPatch => BuildAdaptivePrompt(request),
        PathContentKind.LearningPath => BuildLearningPathPrompt(request),
        AiOperations.LearningPathContent => BuildLearningPathPrompt(request),
        _ => BuildLearningPathPrompt(request),
    };

    private static string BuildAssessmentPrompt(ContentGenerationRequest request)
    {
        var count = ReadQuestionCount(request);
        var goal = request.LearningGoal;

        return
            $"Bạn là trợ lý tạo bài đánh giá năng lực cho mục tiêu học tập \"{goal}\".\n" +
            $"Hãy tạo CHÍNH XÁC {count} câu hỏi trắc nghiệm, mỗi câu có 4 lựa chọn và đúng một đáp án.\n" +
            "Chỉ trả về JSON hợp lệ (không kèm giải thích, không kèm markdown) theo schema:\n" +
            "{\"Questions\":[{\"Id\":null,\"SkillArea\":\"...\",\"Prompt\":\"...\"," +
            "\"Options\":[\"A\",\"B\",\"C\",\"D\"],\"CorrectOption\":\"A\"}]}\n" +
            "Trường CorrectOption phải là một trong các giá trị thuộc Options.";
    }

    private static string BuildCareerPrompt(ContentGenerationRequest request)
    {
        var career = request.LearningGoal;

        return
            $"Bạn là cố vấn nghề nghiệp cho nghề \"{career}\".\n" +
            "Hãy đề xuất lộ trình kỹ năng, các chứng chỉ liên quan và các dự án thực tế.\n" +
            "Chỉ trả về JSON hợp lệ (không kèm giải thích, không kèm markdown) theo schema:\n" +
            "{\"Skills\":[\"...\"],\"Certifications\":[\"...\"],\"Projects\":[\"...\"]}\n" +
            "Mỗi danh sách cần có ít nhất một phần tử; Skills là bắt buộc.";
    }

    private static string BuildLearningPathPrompt(ContentGenerationRequest request)
    {
        var goal = request.LearningGoal;
        var details = new StringBuilder();

        if (request.Parameters.TryGetValue("targetDays", out var targetDays) &&
            !string.IsNullOrWhiteSpace(targetDays))
        {
            details.Append($" trong khoảng {targetDays} ngày");
        }

        if (request.Parameters.TryGetValue("level", out var level) &&
            !string.IsNullOrWhiteSpace(level))
        {
            details.Append($" cho trình độ {level}");
        }

        return
            $"Bạn là chuyên gia thiết kế lộ trình học tập cho mục tiêu \"{goal}\"{details}.\n" +
            "Hãy mô tả nội dung lộ trình học tập rõ ràng, súc tích bằng tiếng Việt. " +
            "Trả về văn bản thuần (không kèm markdown).";
    }

    private static string BuildTutorPrompt(ContentGenerationRequest request)
    {
        var question = request.Parameters.TryGetValue("question", out var q) ? q : string.Empty;
        var context = request.Parameters.TryGetValue("context", out var c) ? c : string.Empty;
        return
            $"You are an AI tutor for goal \"{request.LearningGoal}\".\nQuestion: {question}\nContext: {context}\n" +
            "Return valid JSON only: {\"Answer\":\"...\",\"Resources\":[\"...\"],\"RelatedTasks\":[\"...\"],\"Confidence\":0.8}";
    }

    private static string BuildDashboardInsightPrompt(ContentGenerationRequest request)
    {
        var context = request.Parameters.TryGetValue("context", out var c) ? c : string.Empty;
        return
            $"Analyze this learning dashboard for \"{request.LearningGoal}\".\n{context}\n" +
            "Return valid JSON only: {\"Summary\":\"...\",\"Risks\":[\"...\"],\"Recommendations\":[\"...\"],\"NextActions\":[\"...\"]}";
    }

    private static string BuildSchedulePrompt(ContentGenerationRequest request)
    {
        var context = request.Parameters.TryGetValue("context", out var c) ? c : string.Empty;
        return
            $"Create a study schedule for \"{request.LearningGoal}\".\n{context}\n" +
            "Return valid JSON only: {\"Items\":[{\"Title\":\"...\",\"Skill\":\"...\",\"DayOffset\":0,\"EstimatedHours\":1.5,\"LearningTaskId\":null}]}";
    }

    private static string BuildAdaptivePrompt(ContentGenerationRequest request)
    {
        var context = request.Parameters.TryGetValue("context", out var c) ? c : string.Empty;
        return
            $"Suggest an adaptive learning path patch for \"{request.LearningGoal}\".\n{context}\n" +
            "Return valid JSON only: {\"Summary\":\"...\",\"AddedTasks\":[\"...\"],\"FocusSkills\":[\"...\"]}";
    }

    private static int ReadQuestionCount(ContentGenerationRequest request)
    {
        if (request.Parameters.TryGetValue("questionCount", out var raw) &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
            parsed > 0)
        {
            return parsed;
        }

        return AssessmentDefaults.QuestionCount;
    }

    // ----------------------------------------------------------------------------------
    // Chuẩn hóa text trả về về schema mà lớp Application kỳ vọng
    // ----------------------------------------------------------------------------------

    /// <summary>
    /// Chuẩn hóa text Gemini về JSON <see cref="GeneratedAssessmentContent"/>. Ưu tiên dùng
    /// JSON do Gemini trả nếu khớp schema; nếu không, sinh cấu trúc tối thiểu từ số câu hỏi
    /// yêu cầu để giữ luồng hoạt động (R17.5).
    /// </summary>
    private string NormalizeAssessment(string rawText, ContentGenerationRequest request)
    {
        var json = ExtractJsonBlock(rawText);
        if (json is not null)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<GeneratedAssessmentContent>(json, ResponseReadOptions);
                if (parsed?.Questions is { Count: > 0 })
                {
                    // Tái serialize bằng JsonFieldSerializer để đảm bảo cùng quy ước với placeholder.
                    return JsonFieldSerializer.Serialize(parsed);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Gemini trả JSON đánh giá không khớp schema, dùng fallback tối thiểu.");
            }
        }

        // Fallback: sinh cấu trúc tối thiểu từ Learning Goal + số câu hỏi yêu cầu.
        var count = ReadQuestionCount(request);
        var goal = request.LearningGoal;
        var questions = Enumerable.Range(1, count)
            .Select(i => new GeneratedQuestion(
                Id: Guid.NewGuid(),
                SkillArea: $"{goal}-Skill-{i}",
                Prompt: $"[{goal}] Câu hỏi số {i}?",
                Options: new[] { "A", "B", "C", "D" },
                CorrectOption: "A"))
            .ToList();

        return JsonFieldSerializer.Serialize(new GeneratedAssessmentContent(questions));
    }

    /// <summary>
    /// Chuẩn hóa text Gemini về JSON <see cref="GeneratedCareerContent"/>. Ưu tiên dùng JSON
    /// khớp schema; nếu không có kỹ năng nào, sinh cấu trúc tối thiểu từ nghề nghiệp (R17.5).
    /// </summary>
    private string NormalizeCareer(string rawText, ContentGenerationRequest request)
    {
        var json = ExtractJsonBlock(rawText);
        if (json is not null)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<GeneratedCareerContent>(json, ResponseReadOptions);
                if (parsed?.Skills is { Count: > 0 })
                {
                    return JsonFieldSerializer.Serialize(parsed);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Gemini trả JSON nghề nghiệp không khớp schema, dùng fallback tối thiểu.");
            }
        }

        // Fallback: cấu trúc tối thiểu gắn với nghề nghiệp để bảo đảm Skills không rỗng.
        var career = request.LearningGoal;
        var content = new GeneratedCareerContent(
            Skills: new[] { $"{career}-Skill-1", $"{career}-Skill-2", $"{career}-Skill-3" },
            Certifications: new[] { $"{career}-Cert-1", $"{career}-Cert-2" },
            Projects: new[] { $"{career}-Project-1", $"{career}-Project-2" });

        return JsonFieldSerializer.Serialize(content);
    }

    /// <summary>
    /// Chuẩn hóa text Gemini cho lộ trình học tập: trả plain text (R17.5). Nếu Gemini trả
    /// rỗng, fallback nội dung không rỗng gắn với Learning Goal để service không ném lỗi.
    /// </summary>
    private static string NormalizeLearningPath(string rawText, ContentGenerationRequest request)
    {
        var text = rawText?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return $"[{request.LearningGoal}] Nội dung lộ trình học tập.";
    }

    /// <summary>
    /// Trích khối JSON từ text tự do của Gemini. Hỗ trợ trường hợp text được bọc trong
    /// hàng rào markdown (```json ... ```), hoặc JSON nằm xen lẫn văn bản. Trả về null khi
    /// không tìm thấy khối JSON hợp lý.
    /// </summary>
    private string NormalizeTutor(string rawText, ContentGenerationRequest request)
    {
        var parsed = TryDeserializeJson<GeneratedTutorAnswer>(rawText);
        if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Answer))
        {
            return JsonFieldSerializer.Serialize(parsed);
        }

        return JsonFieldSerializer.Serialize(new GeneratedTutorAnswer(
            $"Answer for {request.LearningGoal}: review your weakest skill, complete one focused practice task, then ask a narrower question.",
            new[] { "Learning path", "Assessment result" },
            new[] { "Review weak skill" },
            0.6));
    }

    private string NormalizeDashboardInsight(string rawText, ContentGenerationRequest request)
    {
        var parsed = TryDeserializeJson<GeneratedDashboardInsight>(rawText);
        if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Summary))
        {
            return JsonFieldSerializer.Serialize(parsed);
        }

        return JsonFieldSerializer.Serialize(new GeneratedDashboardInsight(
            $"You are progressing toward {request.LearningGoal}; keep a steady study rhythm this week.",
            new[] { "Progress may slow down if key tasks are skipped." },
            new[] { "Complete one task today.", "Review the weakest assessed skill." },
            new[] { "Open Learning Path", "Simulate Academic Twin" }));
    }

    private string NormalizeSchedule(string rawText, ContentGenerationRequest request)
    {
        var parsed = TryDeserializeJson<GeneratedSchedulePlan>(rawText);
        if (parsed?.Items is { Count: > 0 })
        {
            return JsonFieldSerializer.Serialize(parsed);
        }

        return JsonFieldSerializer.Serialize(new GeneratedSchedulePlan(
            Enumerable.Range(0, 7)
                .Select(i => new GeneratedScheduleItem($"Study block {i + 1}", request.LearningGoal, i, 1.5, null))
                .ToList()));
    }

    private string NormalizeAdaptive(string rawText, ContentGenerationRequest request)
    {
        var parsed = TryDeserializeJson<GeneratedAdaptivePathPatch>(rawText);
        if (parsed is not null && !string.IsNullOrWhiteSpace(parsed.Summary))
        {
            return JsonFieldSerializer.Serialize(parsed);
        }

        return JsonFieldSerializer.Serialize(new GeneratedAdaptivePathPatch(
            "Keep the current path and add focused review tasks.",
            new[] { "Focused weak-skill review", "Short practice quiz", "Reflection checkpoint" },
            new[] { request.LearningGoal }));
    }

    private T? TryDeserializeJson<T>(string rawText)
    {
        var json = ExtractJsonBlock(rawText);
        if (json is null)
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, ResponseReadOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Gemini returned JSON that did not match schema {Schema}.", typeof(T).Name);
            return default;
        }
    }

    private static string? ExtractJsonBlock(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();

        // Gỡ hàng rào markdown nếu có (```json ... ``` hoặc ``` ... ```).
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
            {
                trimmed = trimmed[(firstNewline + 1)..];
            }

            var fenceEnd = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd >= 0)
            {
                trimmed = trimmed[..fenceEnd];
            }

            trimmed = trimmed.Trim();
        }

        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            return trimmed;
        }

        // Cố gắng tìm cặp ngoặc nhọn ngoài cùng nếu JSON nằm xen văn bản.
        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            return trimmed[start..(end + 1)];
        }

        return null;
    }
}
