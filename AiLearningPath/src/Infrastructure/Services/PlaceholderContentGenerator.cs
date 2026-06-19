using System.Globalization;
using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.Career;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Paths;
using AiLearningPath.Domain.Common;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai placeholder (xác định) của <see cref="IContentGenerator"/> dùng khi dịch vụ
/// Gemini thật CHƯA được cấu hình (thiếu API key/endpoint trong appsettings).
///
/// Mục đích: cho phép ứng dụng khởi động và chạy luồng end-to-end (đăng ký → profile →
/// goal → assessment → DNA → path → dashboard → twin → career) mà không phụ thuộc mạng
/// hay khóa API thật. Nội dung sinh ra là deterministic và gắn với Learning Goal/nghề
/// nghiệp nhận được, có cùng cấu trúc JSON mà các service ở lớp Application kỳ vọng phân
/// tích — nên hành vi giống hệt các stub đã dùng trong kiểm thử.
///
/// LƯU Ý VẬN HÀNH: đây KHÔNG phải nội dung do AI thật sinh ra. Khi triển khai production,
/// cấu hình section "Gemini" (ApiKey + Endpoint) và thay bằng adapter HTTP gọi Gemini thật.
/// </summary>
public sealed class PlaceholderContentGenerator : IContentGenerator
{
    /// <inheritdoc />
    public Task<ContentGenerationResult> GenerateAsync(
        ContentGenerationRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var content = request.Kind switch
        {
            AssessmentContentKind.Assessment => GenerateAssessment(request),
            PathContentKind.LearningPath => GenerateLearningPath(request),
            CareerContentKind.CareerPath => GenerateCareerPath(request),
            _ => GenerateLearningPath(request),
        };

        return Task.FromResult(new ContentGenerationResult(content));
    }

    /// <summary>
    /// Sinh bộ câu hỏi đánh giá deterministic theo Learning Goal (R5.1). Số câu hỏi lấy từ
    /// tham số <c>questionCount</c> nếu có, ngược lại dùng <see cref="AssessmentDefaults.QuestionCount"/>.
    /// </summary>
    private static string GenerateAssessment(ContentGenerationRequest request)
    {
        var count = AssessmentDefaults.QuestionCount;
        count = 20;

      var goal = request.LearningGoal;

var questions = Enumerable.Range(1, count)
    .Select(i => new GeneratedQuestion(
        Id: Guid.NewGuid(),
        SkillArea: goal.ToUpper() == "IELTS"
            ? GetIeltsSkill(i)
            : GetToeicSkill(i),
        Prompt: goal.ToUpper() == "IELTS"
            ? GetIeltsQuestion(i)
            : GetToeicQuestion(i),
        Options: goal.ToUpper() == "IELTS"
            ? GetIeltsOptions(i)
            : GetToeicOptions(i),
        CorrectOption: "A"))
    .ToList();
        return JsonFieldSerializer.Serialize(new GeneratedAssessmentContent(questions));
    }

    /// <summary>
    /// Sinh nội dung lộ trình học tập deterministic, không rỗng, gắn với Learning Goal (R7.1).
    /// </summary>
    private static string GenerateLearningPath(ContentGenerationRequest request) =>
        $"[{request.LearningGoal}] Nội dung lộ trình học tập mẫu (placeholder).";

    /// <summary>
    /// Sinh lộ trình nghề nghiệp deterministic kèm chứng chỉ/dự án theo nghề (R10.2, R10.3).
    /// </summary>
    private static string GenerateCareerPath(ContentGenerationRequest request)
    {
        var career = request.LearningGoal;
        var content = new GeneratedCareerContent(
            Skills: new[] { $"{career}-Skill-1", $"{career}-Skill-2", $"{career}-Skill-3" },
            Certifications: new[] { $"{career}-Cert-1", $"{career}-Cert-2" },
            Projects: new[] { $"{career}-Project-1", $"{career}-Project-2" });

        return JsonFieldSerializer.Serialize(content);
    }
   private static string GetToeicSkill(int i)
{
    if (i <= 5) return "Grammar";
    if (i <= 10) return "Vocabulary";
    if (i <= 15) return "Listening";
    return "Reading";
}

private static string GetIeltsSkill(int i)
{
    if (i <= 5) return "Reading";
    if (i <= 10) return "Listening";
    if (i <= 15) return "Writing";
    return "Speaking";
}

private static string GetToeicQuestion(int i)
{
    string[] questions =
    {
        "She ___ a report every Monday.",
        "The package ___ yesterday.",
        "They ___ in the meeting room now.",
        "If he has time, he ___ the client.",
        "This product is ___ than the old one.",

        "What does 'customer' mean?",
        "What does 'deadline' mean?",
        "What does 'department' mean?",
        "What does 'invoice' mean?",
        "What does 'refund' mean?",

        "TOEIC Listening Part 1 usually asks you to ____.",
        "TOEIC Listening Part 2 usually checks ____.",
        "When listening to an announcement, you should notice ____.",
        "In a business phone call, you should listen for ____.",
        "Keywords in TOEIC Listening help you ____.",

        "In a business email, 'Best regards' is used to ____.",
        "In a company notice, 'schedule' means ____.",
        "In an advertisement, 'discount' means ____.",
        "In a meeting invitation, 'agenda' means ____.",
        "In a contract, 'signature' means ____."
    };

    return questions[i - 1];
}

private static string[] GetToeicOptions(int i)
{
    string[][] options =
    {
        new[] { "writes", "write", "writing", "written" },
        new[] { "was delivered", "delivered", "deliver", "delivering" },
        new[] { "are sitting", "sits", "sat", "sitting" },
        new[] { "will call", "called", "calling", "calls yesterday" },
        new[] { "better", "best", "good", "well" },

        new[] { "a person who buys goods or services", "a teacher", "a driver", "a doctor" },
        new[] { "the final time to finish something", "a greeting", "an address", "a ticket" },
        new[] { "a section of a company", "an airport", "a hospital", "a restaurant" },
        new[] { "a bill for goods or services", "a meeting room", "a job title", "a schedule" },
        new[] { "money returned to a customer", "a new product", "a company event", "a report" },

        new[] { "describe a picture", "write an essay", "read a long text", "give a speech" },
        new[] { "short questions and answers", "email writing", "chart reading", "translation" },
        new[] { "time, place, and speaker", "paper color", "book pages", "font style" },
        new[] { "names, phone numbers, and time", "shirt color", "bedrooms", "song title" },
        new[] { "identify important information", "translate every word", "ignore context", "guess randomly" },

        new[] { "close an email politely", "start a song", "write a price", "create a password" },
        new[] { "a plan of times and events", "a bill", "an address", "a dish" },
        new[] { "a lower price", "a salary increase", "an invitation", "a phone call" },
        new[] { "a list of meeting topics", "a coupon code", "a flight ticket", "a score sheet" },
        new[] { "a written name showing agreement", "a class schedule", "a password", "a party" }
    };

    return options[i - 1];
}

private static string GetIeltsQuestion(int i)
{
    string[] questions =
    {
        "What does skimming mean in IELTS Reading?",
        "What does scanning mean in IELTS Reading?",
        "What is the main idea of a paragraph?",
        "What does an inference question require?",
        "What does Matching Headings ask you to do?",

        "IELTS Listening Part 1 usually includes ____.",
        "Before listening starts, you should ____.",
        "Active listening means ____.",
        "Note-taking helps you ____.",
        "IELTS Listening may ask you to ____.",

        "IELTS Writing Task 1 usually asks you to ____.",
        "IELTS Writing Task 2 usually asks you to ____.",
        "An opinion essay requires you to ____.",
        "A discussion essay requires you to ____.",
        "A conclusion should ____.",

        "IELTS Speaking Part 1 usually asks about ____.",
        "IELTS Speaking Part 2 asks you to ____.",
        "IELTS Speaking Part 3 usually tests ____.",
        "Fluency means ____.",
        "Pronunciation is important because it helps ____."
    };

    return questions[i - 1];
}

private static string[] GetIeltsOptions(int i)
{
    string[][] options =
    {
        new[] { "reading quickly for the main idea", "reading every word slowly", "listening to audio", "writing an essay" },
        new[] { "looking for specific information", "memorizing vocabulary", "speaking faster", "writing a summary" },
        new[] { "the central point of the text", "a small detail", "a grammar rule", "a spelling mistake" },
        new[] { "understand information that is implied", "copy a sentence", "translate the title", "count words" },
        new[] { "match headings with paragraphs", "write an essay", "listen to a lecture", "describe a chart" },

        new[] { "daily conversations", "academic essays", "business contracts", "novels" },
        new[] { "read the questions first", "close the paper", "ignore keywords", "write randomly" },
        new[] { "listening with a clear purpose", "listening without focus", "sleeping with audio", "skipping the audio" },
        new[] { "remember key information", "draw pictures", "write long essays", "avoid listening" },
        new[] { "fill in missing words", "write an opinion essay", "give a speech", "read a passage aloud" },

        new[] { "describe a chart, table, map, or process", "talk about your family", "listen to a phone call", "read an email" },
        new[] { "write an essay", "describe a picture orally", "answer short grammar questions", "translate a sentence" },
        new[] { "give your own viewpoint", "copy the question", "read numbers", "draw a map" },
        new[] { "discuss two views", "write a job application", "listen to a podcast", "read a timetable" },
        new[] { "summarize the main points", "introduce a new unrelated idea", "copy the introduction", "ask a question" },

        new[] { "personal topics", "essay structure", "company reports", "scientific formulas" },
        new[] { "speak about a topic for 1-2 minutes", "write a formal letter", "listen to a lecture", "read a chart" },
        new[] { "extended answers and analysis", "spelling only", "short yes/no answers", "multiple choice only" },
        new[] { "speaking smoothly without too many pauses", "speaking very loudly", "using difficult words only", "reading from notes" },
        new[] { "the examiner understand your ideas", "increase your writing score only", "avoid grammar", "make answers longer only" }
    };

    return options[i - 1];
}
}
