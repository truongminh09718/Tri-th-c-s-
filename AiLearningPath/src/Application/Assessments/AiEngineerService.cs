using System;
using System.Collections.Generic;
using System.Linq;

namespace AiLearningPath.Application.Assessments;

public class AiEngineerService
{
    private readonly List<QuestionItem> _questions = new()
    {
        new(1, "TOEIC", "Vocabulary", "Từ 'customer' có nghĩa là gì?", new[] { "khách hàng", "giáo viên", "bác sĩ", "tài xế" }, "khách hàng"),
        new(2, "TOEIC", "Vocabulary", "Từ 'meeting' có nghĩa là gì?", new[] { "cuộc họp", "bữa ăn", "kỳ nghỉ", "bài hát" }, "cuộc họp"),
        new(3, "TOEIC", "Vocabulary", "Từ 'deadline' có nghĩa là gì?", new[] { "hạn chót", "lời chào", "địa chỉ", "vé xe" }, "hạn chót"),
        new(4, "TOEIC", "Vocabulary", "Từ 'report' có nghĩa là gì?", new[] { "báo cáo", "cửa hàng", "máy tính", "nhà hàng" }, "báo cáo"),
        new(5, "TOEIC", "Vocabulary", "Từ 'department' có nghĩa là gì?", new[] { "phòng ban", "sân bay", "bệnh viện", "trường học" }, "phòng ban"),

        new(6, "TOEIC", "Grammar", "She ___ a report every Monday.", new[] { "writes", "write", "writing", "written" }, "writes"),
        new(7, "TOEIC", "Grammar", "The package ___ yesterday.", new[] { "was delivered", "delivered", "deliver", "delivering" }, "was delivered"),
        new(8, "TOEIC", "Grammar", "They ___ in the meeting room now.", new[] { "are sitting", "sits", "sat", "sitting" }, "are sitting"),
        new(9, "TOEIC", "Grammar", "If he has time, he ___ the client.", new[] { "will call", "called", "calling", "calls yesterday" }, "will call"),
        new(10, "TOEIC", "Grammar", "This product is ___ than the old one.", new[] { "better", "best", "good", "well" }, "better"),

        new(11, "TOEIC", "Reading", "In a business email, 'Best regards' is used to ____.", new[] { "close an email politely", "start a song", "write a price", "create a password" }, "close an email politely"),
        new(12, "TOEIC", "Reading", "In a company notice, 'schedule' means ____.", new[] { "a plan of times and events", "a bill", "an address", "a dish" }, "a plan of times and events"),
        new(13, "TOEIC", "Reading", "In an advertisement, 'discount' means ____.", new[] { "a lower price", "a salary increase", "an invitation", "a phone call" }, "a lower price"),
        new(14, "TOEIC", "Reading", "In a meeting invitation, 'agenda' means ____.", new[] { "a list of meeting topics", "a coupon code", "a flight ticket", "a score sheet" }, "a list of meeting topics"),
        new(15, "TOEIC", "Reading", "In a contract, 'signature' means ____.", new[] { "a written name showing agreement", "a class schedule", "a password", "a party" }, "a written name showing agreement"),

        new(16, "TOEIC", "Listening", "TOEIC Listening Part 1 usually asks you to ____.", new[] { "describe a picture", "write an essay", "read a long text", "give a speech" }, "describe a picture"),
        new(17, "TOEIC", "Listening", "TOEIC Listening Part 2 usually checks ____.", new[] { "short questions and answers", "email writing", "chart reading", "translation" }, "short questions and answers"),
        new(18, "TOEIC", "Listening", "When listening to an announcement, you should notice ____.", new[] { "time, place, and speaker", "paper color", "book pages", "font style" }, "time, place, and speaker"),
        new(19, "TOEIC", "Listening", "In a business phone call, you should listen for ____.", new[] { "names, phone numbers, and time", "shirt color", "bedrooms", "song title" }, "names, phone numbers, and time"),
        new(20, "TOEIC", "Listening", "Keywords in TOEIC Listening help you ____.", new[] { "identify important information", "translate every word", "ignore context", "guess randomly" }, "identify important information"),

        new(1, "IELTS", "Reading", "What does skimming mean in IELTS Reading?", new[] { "reading quickly for the main idea", "reading every word slowly", "listening to audio", "writing an essay" }, "reading quickly for the main idea"),
        new(2, "IELTS", "Reading", "What does scanning mean in IELTS Reading?", new[] { "looking for specific information", "memorizing vocabulary", "speaking faster", "writing a summary" }, "looking for specific information"),
        new(3, "IELTS", "Reading", "What is the main idea of a paragraph?", new[] { "the central point of the text", "a small detail", "a grammar rule", "a spelling mistake" }, "the central point of the text"),
        new(4, "IELTS", "Reading", "What does an inference question require?", new[] { "understand information that is implied", "copy a sentence", "translate the title", "count words" }, "understand information that is implied"),
        new(5, "IELTS", "Reading", "What does Matching Headings ask you to do?", new[] { "match headings with paragraphs", "write an essay", "listen to a lecture", "describe a chart" }, "match headings with paragraphs"),

        new(6, "IELTS", "Listening", "IELTS Listening Part 1 usually includes ____.", new[] { "daily conversations", "academic essays", "business contracts", "novels" }, "daily conversations"),
        new(7, "IELTS", "Listening", "Before listening starts, you should ____.", new[] { "read the questions first", "close the paper", "ignore keywords", "write randomly" }, "read the questions first"),
        new(8, "IELTS", "Listening", "Active listening means ____.", new[] { "listening with a clear purpose", "listening without focus", "sleeping with audio", "skipping the audio" }, "listening with a clear purpose"),
        new(9, "IELTS", "Listening", "Note-taking helps you ____.", new[] { "remember key information", "draw pictures", "write long essays", "avoid listening" }, "remember key information"),
        new(10, "IELTS", "Listening", "IELTS Listening may ask you to ____.", new[] { "fill in missing words", "write an opinion essay", "give a speech", "read a passage aloud" }, "fill in missing words"),

        new(11, "IELTS", "Writing", "IELTS Writing Task 1 usually asks you to ____.", new[] { "describe a chart, table, map, or process", "talk about your family", "listen to a phone call", "read an email" }, "describe a chart, table, map, or process"),
        new(12, "IELTS", "Writing", "IELTS Writing Task 2 usually asks you to ____.", new[] { "write an essay", "describe a picture orally", "answer short grammar questions", "translate a sentence" }, "write an essay"),
        new(13, "IELTS", "Writing", "An opinion essay requires you to ____.", new[] { "give your own viewpoint", "copy the question", "read numbers", "draw a map" }, "give your own viewpoint"),
        new(14, "IELTS", "Writing", "A discussion essay requires you to ____.", new[] { "discuss two views", "write a job application", "listen to a podcast", "read a timetable" }, "discuss two views"),
        new(15, "IELTS", "Writing", "A conclusion should ____.", new[] { "summarize the main points", "introduce a new unrelated idea", "copy the introduction", "ask a question" }, "summarize the main points"),

        new(16, "IELTS", "Speaking", "IELTS Speaking Part 1 usually asks about ____.", new[] { "personal topics", "essay structure", "company reports", "scientific formulas" }, "personal topics"),
        new(17, "IELTS", "Speaking", "IELTS Speaking Part 2 asks you to ____.", new[] { "speak about a topic for 1-2 minutes", "write a formal letter", "listen to a lecture", "read a chart" }, "speak about a topic for 1-2 minutes"),
        new(18, "IELTS", "Speaking", "IELTS Speaking Part 3 usually tests ____.", new[] { "extended answers and analysis", "spelling only", "short yes/no answers", "multiple choice only" }, "extended answers and analysis"),
        new(19, "IELTS", "Speaking", "Fluency means ____.", new[] { "speaking smoothly without too many pauses", "speaking very loudly", "using difficult words only", "reading from notes" }, "speaking smoothly without too many pauses"),
        new(20, "IELTS", "Speaking", "Pronunciation is important because it helps ____.", new[] { "the examiner understand your ideas", "increase your writing score only", "avoid grammar", "make answers longer only" }, "the examiner understand your ideas")
    };

    public object GetAssessmentTest(string goal = "TOEIC")
    {
        return _questions
            .Where(q => q.Goal.Equals(goal, StringComparison.OrdinalIgnoreCase))
            .Select(q => new
            {
                id = q.Id,
                skill = q.Skill,
                question = q.Question,
                options = q.Options
            })
            .ToList();
    }

    public object GradeTest(AiEngineerGradeRequest request)
    {
        var questions = _questions
            .Where(q => q.Goal.Equals(request.Goal, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var reviews = new List<QuestionReview>();

        foreach (var answer in request.Answers)
        {
            var q = questions.FirstOrDefault(x => x.Id == answer.QuestionId);
            if (q == null) continue;

            bool isCorrect = string.Equals(
    "A",
    answer.Answer.Trim(),
    StringComparison.OrdinalIgnoreCase
);

            reviews.Add(new QuestionReview
            {
                QuestionId = q.Id,
                Question = q.Question,
                Skill = q.Skill,
                YourAnswer = answer.Answer,
                CorrectAnswer = q.CorrectAnswer,
                IsCorrect = isCorrect
            });
        }

        int total = reviews.Count;
        int correct = reviews.Count(x => x.IsCorrect);
        int score = total == 0 ? 0 : correct * 100 / total;

        var skillScores = reviews
            .GroupBy(x => x.Skill)
            .ToDictionary(
                g => g.Key,
                g => g.Count(x => x.IsCorrect) * 100 / g.Count()
            );

        string strength = skillScores.Any()
            ? skillScores.OrderByDescending(x => x.Value).First().Key
            : "No data";

        string weakness = skillScores.Any()
            ? skillScores.OrderBy(x => x.Value).First().Key
            : "No data";

        string level = score >= 80 ? "Advanced"
            : score >= 60 ? "Intermediate"
            : "Beginner";

       return new
{
    score = score,
    correct = correct,
    level = level,
    strength = strength,
    weakness = weakness,

    advice = $"Focus on improving {weakness}.",

    dailyPlan = new[]
    {
        $"30 minutes {weakness} practice",
        "20 minutes vocabulary review",
        "10 minutes mistake review"
    },

    weeklyPlan = new[]
    {
        "Monday: Practice",
        "Tuesday: Vocabulary",
        "Wednesday: Mini Test",
        "Thursday: Review",
        "Friday: Practice",
        "Saturday: Full Test",
        "Sunday: Rest"
    },

    recommendation = $"Your current level is {level}. Continue improving {weakness}."
};
    }
}

public record QuestionItem(
    int Id,
    string Goal,
    string Skill,
    string Question,
    string[] Options,
    string CorrectAnswer
);

public class AnswerItem
{
    public int QuestionId { get; set; }
    public string Answer { get; set; } = "";
}

public class AiEngineerGradeRequest
{
    public string Goal { get; set; } = "TOEIC";
    public List<AnswerItem> Answers { get; set; } = new();
}

public class QuestionReview
{
    public int QuestionId { get; set; }
    public string Question { get; set; } = "";
    public string YourAnswer { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public bool IsCorrect { get; set; }
    public string Skill { get; set; } = "";
}