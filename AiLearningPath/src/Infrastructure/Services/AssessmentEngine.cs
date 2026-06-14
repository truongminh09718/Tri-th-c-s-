using System.Globalization;
using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.LearningDna;
using AiLearningPath.Domain.Assessments;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Domain.Profiles;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai <see cref="IAssessmentEngine"/> trên nền EF Core (<see cref="AppDbContext"/>).
/// Điều phối nghiệp vụ đánh giá năng lực (R5):
/// <list type="bullet">
///   <item><see cref="StartAsync"/> sinh bộ câu hỏi qua <see cref="IContentGenerator"/> (bọc Gemini)
///   theo Learning Goal và lưu <see cref="Assessment"/> (R5.1).</item>
///   <item><see cref="SubmitAsync"/> áp dụng <see cref="AssessmentGrader.ValidateCompleteness"/> (R5.4),
///   <see cref="AssessmentGrader.Grade"/> (R5.2) rồi lưu <see cref="AssessmentResult"/>
///   liên kết với hồ sơ sinh viên (R5.3).</item>
/// </list>
/// Bộ câu hỏi (kèm đáp án) được lưu dạng JSON trong <see cref="Assessment.QuestionsJson"/>
/// qua <see cref="JsonFieldSerializer"/>; điểm mạnh/yếu của kết quả cũng được serialize JSON.
/// </summary>
public sealed class AssessmentEngine : IAssessmentEngine
{
    private readonly AppDbContext _db;
    private readonly IContentGenerator _contentGenerator;
    private readonly ILearningDnaEngine? _learningDnaEngine;

    public AssessmentEngine(
        AppDbContext db,
        IContentGenerator contentGenerator,
        ILearningDnaEngine? learningDnaEngine = null)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _contentGenerator = contentGenerator ?? throw new ArgumentNullException(nameof(contentGenerator));
        // Tùy chọn (R6.1): khi được cung cấp, tự động xây Learning DNA Profile ngay sau khi
        // chấm xong bài đánh giá (wiring luồng assessment → DNA). Cho phép null để các bài
        // kiểm thử chỉ quan tâm tới logic chấm điểm vẫn khởi tạo được engine.
        _learningDnaEngine = learningDnaEngine;
    }

    /// <inheritdoc />
    public async Task<Assessment> StartAsync(
        Guid userId, string learningGoal, CancellationToken cancellationToken = default)
    {
        // R4.4/R5.1: Learning Goal phải thuộc danh mục hỗ trợ trước khi sinh câu hỏi.
        var goalValidation = ProfileValidator.ValidateGoal(learningGoal);
        if (!goalValidation.IsValid)
        {
            throw new AssessmentValidationException(goalValidation);
        }

        // R5.1: sinh bộ câu hỏi theo Learning Goal qua dịch vụ sinh nội dung AI (Gemini).
        var request = new ContentGenerationRequest(
            AssessmentContentKind.Assessment,
            learningGoal,
            new Dictionary<string, string>
            {
                ["questionCount"] = AssessmentDefaults.QuestionCount.ToString(CultureInfo.InvariantCulture)
            });

        var generated = await _contentGenerator.GenerateAsync(request, cancellationToken);
        var questions = ParseQuestions(generated.Content);

        var assessment = new Assessment
        {
            UserId = userId,
            LearningGoal = learningGoal,
            QuestionsJson = JsonFieldSerializer.Serialize(questions),
            Status = AssessmentStatus.InProgress
        };

        _db.Assessments.Add(assessment);
        await _db.SaveChangesAsync(cancellationToken);

        return assessment;
    }

    /// <inheritdoc />
    public async Task<AssessmentResult> SubmitAsync(
        Guid userId, Guid assessmentId, IReadOnlyList<Answer> answers, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(answers);

        // Chỉ nạp bài đánh giá thuộc về chính sinh viên (ownership-based authorization, R14.1).
        var assessment = await _db.Assessments
            .FirstOrDefaultAsync(a => a.Id == assessmentId && a.UserId == userId, cancellationToken);

        if (assessment is null)
        {
            throw new AssessmentNotFoundException(assessmentId);
        }

        var storedQuestions = JsonFieldSerializer.Deserialize<List<AssessmentQuestionItem>>(assessment.QuestionsJson)
            ?? new List<AssessmentQuestionItem>();

        // Ánh xạ sang mô hình thuần để dùng cho domain logic chấm điểm.
        var gradingQuestions = storedQuestions
            .Select(q => new AssessmentQuestion(q.Id, q.SkillArea, q.CorrectOption))
            .ToList();

        // R5.4: từ chối nếu còn câu hỏi chưa được trả lời.
        var completeness = AssessmentGrader.ValidateCompleteness(gradingQuestions, answers);
        if (!completeness.IsValid)
        {
            throw new AssessmentValidationException(completeness);
        }

        // R5.2: chấm bài (tính xác định) — suy ra trình độ, điểm mạnh/yếu, điểm số.
        var grading = AssessmentGrader.Grade(gradingQuestions, answers);

        // R5.3: lưu kết quả liên kết với hồ sơ sinh viên. Tạo mới hoặc cập nhật kết quả hiện có.
        var result = await _db.AssessmentResults
            .FirstOrDefaultAsync(r => r.AssessmentId == assessmentId, cancellationToken);

        if (result is null)
        {
            result = new AssessmentResult
            {
                AssessmentId = assessmentId,
                UserId = userId
            };
            _db.AssessmentResults.Add(result);
        }

        result.Level = grading.Level;
        result.Score = grading.Score;
        result.StrengthsJson = JsonFieldSerializer.Serialize(grading.Strengths);
        result.WeaknessesJson = JsonFieldSerializer.Serialize(grading.Weaknesses);

        assessment.Status = AssessmentStatus.Completed;

        await _db.SaveChangesAsync(cancellationToken);

        // Wiring luồng assessment → DNA (R6.1): ngay sau khi nộp và chấm xong bài đánh giá,
        // tự động xây (hoặc xây lại) Learning DNA Profile từ kết quả vừa lưu. Chỉ thực hiện
        // khi engine được cấu hình với ILearningDnaEngine (luôn đúng trong DI của ứng dụng).
        if (_learningDnaEngine is not null)
        {
            await _learningDnaEngine.BuildAsync(userId, result, cancellationToken);
        }

        return result;
    }

    /// <summary>
    /// Phân tích payload JSON do dịch vụ sinh nội dung AI trả về thành danh sách câu hỏi
    /// đầy đủ. Gán Id mới cho câu hỏi thiếu Id để đảm bảo mỗi câu hỏi có định danh duy nhất.
    /// </summary>
    /// <exception cref="AssessmentGenerationException">Khi payload rỗng hoặc không hợp lệ.</exception>
    private static List<AssessmentQuestionItem> ParseQuestions(string content)
    {
        GeneratedAssessmentContent? parsed;
        try
        {
            parsed = JsonFieldSerializer.Deserialize<GeneratedAssessmentContent>(content);
        }
        catch (Exception ex) when (ex is not AssessmentGenerationException)
        {
            throw new AssessmentGenerationException(
                "Nội dung câu hỏi do dịch vụ AI trả về không hợp lệ (không phân tích được JSON).");
        }

        if (parsed?.Questions is null || parsed.Questions.Count == 0)
        {
            throw new AssessmentGenerationException(
                "Dịch vụ AI không sinh được câu hỏi nào cho Learning Goal đã chọn.");
        }

        var usedIds = new HashSet<Guid>();
        var questions = new List<AssessmentQuestionItem>(parsed.Questions.Count);

        foreach (var q in parsed.Questions)
        {
            var id = q.Id ?? Guid.Empty;
            if (id == Guid.Empty || !usedIds.Add(id))
            {
                id = Guid.NewGuid();
                usedIds.Add(id);
            }

            questions.Add(new AssessmentQuestionItem(
                id,
                q.SkillArea ?? string.Empty,
                q.Prompt ?? string.Empty,
                q.Options ?? Array.Empty<string>(),
                q.CorrectOption ?? string.Empty));
        }

        return questions;
    }
}
