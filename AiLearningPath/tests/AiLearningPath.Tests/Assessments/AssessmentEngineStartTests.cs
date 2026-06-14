using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Domain.Common;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Tests.Assessments;

/// <summary>
/// Integration test cho <see cref="AssessmentEngine.StartAsync"/> (R5.1): sinh bộ câu hỏi
/// theo Learning Goal qua <see cref="IContentGenerator"/> (bọc Gemini) và lưu
/// <see cref="Domain.Entities.Assessment"/>.
///
/// <see cref="IContentGenerator"/> được thay bằng một stub xác định (deterministic) trả về
/// payload JSON câu hỏi tương ứng với Learning Goal yêu cầu, nên không cần gọi Gemini thật.
/// EF Core In-Memory provider được dùng để kiểm thử thao tác lưu/đọc thật qua DbContext
/// mà không cần SQL Server đang chạy; mỗi test dùng một database riêng để cô lập trạng thái.
/// </summary>
public sealed class AssessmentEngineStartTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>
    /// Stub xác định của <see cref="IContentGenerator"/>: sinh đúng <paramref name="questionCount"/>
    /// câu hỏi cho Learning Goal nhận được, đồng thời ghi lại yêu cầu cuối cùng để kiểm chứng.
    /// </summary>
    private sealed class StubContentGenerator : IContentGenerator
    {
        private readonly int _questionCount;

        public StubContentGenerator(int questionCount) => _questionCount = questionCount;

        public ContentGenerationRequest? LastRequest { get; private set; }

        public Task<ContentGenerationResult> GenerateAsync(
            ContentGenerationRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;

            // Sinh bộ câu hỏi xác định gắn với Learning Goal để đầu ra có thể kiểm chứng.
            var questions = Enumerable.Range(1, _questionCount)
                .Select(i => new GeneratedQuestion(
                    Id: Guid.NewGuid(),
                    SkillArea: $"{request.LearningGoal}-Skill-{i}",
                    Prompt: $"[{request.LearningGoal}] Câu hỏi số {i}?",
                    Options: new[] { "A", "B", "C", "D" },
                    CorrectOption: "A"))
                .ToList();

            var payload = new GeneratedAssessmentContent(questions);
            var json = JsonFieldSerializer.Serialize(payload);

            return Task.FromResult(new ContentGenerationResult(json));
        }
    }

    // R5.1: StartAsync sinh bộ câu hỏi theo Learning Goal và lưu Assessment với trạng thái InProgress.
    [Fact]
    public async Task StartAsync_GeneratesQuestionSetForChosenGoalAndPersists()
    {
        using var db = CreateDb();
        var generator = new StubContentGenerator(AssessmentDefaults.QuestionCount);
        var engine = new AssessmentEngine(db, generator);
        var userId = Guid.NewGuid();
        const string goal = "FrontendDevelopment";

        var assessment = await engine.StartAsync(userId, goal);

        // Bài đánh giá được gắn với đúng sinh viên và Learning Goal đã chọn.
        Assert.Equal(userId, assessment.UserId);
        Assert.Equal(goal, assessment.LearningGoal);
        Assert.Equal(AssessmentStatus.InProgress, assessment.Status);

        // Generator được gọi với đúng Learning Goal và loại nội dung Assessment (R5.1).
        Assert.NotNull(generator.LastRequest);
        Assert.Equal(goal, generator.LastRequest!.LearningGoal);
        Assert.Equal(AssessmentContentKind.Assessment, generator.LastRequest.Kind);

        // Bộ câu hỏi được serialize và persist vào DB.
        var persisted = await db.Assessments.AsNoTracking().SingleAsync(a => a.Id == assessment.Id);
        Assert.Equal(userId, persisted.UserId);
        Assert.Equal(goal, persisted.LearningGoal);
        Assert.False(string.IsNullOrWhiteSpace(persisted.QuestionsJson));

        // Bộ câu hỏi đọc lại đúng số lượng và gắn với Learning Goal đã chọn.
        var questions = JsonFieldSerializer.Deserialize<List<AssessmentQuestionItem>>(persisted.QuestionsJson);
        Assert.NotNull(questions);
        Assert.Equal(AssessmentDefaults.QuestionCount, questions!.Count);
        Assert.All(questions, q =>
        {
            Assert.NotEqual(Guid.Empty, q.Id);
            Assert.StartsWith(goal, q.SkillArea);
            Assert.Contains(goal, q.Prompt);
            Assert.False(string.IsNullOrWhiteSpace(q.CorrectOption));
        });

        // Mỗi câu hỏi có định danh duy nhất.
        Assert.Equal(questions.Count, questions.Select(q => q.Id).Distinct().Count());
    }

    // R5.1: Learning Goal khác nhau sinh ra bộ câu hỏi tương ứng với goal đó.
    [Theory]
    [InlineData("IELTS")]
    [InlineData("BackendDevelopment")]
    [InlineData("DataAnalyst")]
    public async Task StartAsync_GeneratesQuestionsTailoredToEachSupportedGoal(string goal)
    {
        using var db = CreateDb();
        var engine = new AssessmentEngine(db, new StubContentGenerator(AssessmentDefaults.QuestionCount));
        var userId = Guid.NewGuid();

        var assessment = await engine.StartAsync(userId, goal);

        var questions = JsonFieldSerializer.Deserialize<List<AssessmentQuestionItem>>(assessment.QuestionsJson);
        Assert.NotNull(questions);
        Assert.NotEmpty(questions!);
        Assert.All(questions, q => Assert.StartsWith(goal, q.SkillArea));
    }

    // R5.1/R4.4: Learning Goal không hợp lệ bị từ chối trước khi gọi generator và không lưu gì.
    [Fact]
    public async Task StartAsync_WithUnsupportedGoal_ThrowsAndDoesNotPersist()
    {
        using var db = CreateDb();
        var generator = new StubContentGenerator(AssessmentDefaults.QuestionCount);
        var engine = new AssessmentEngine(db, generator);

        await Assert.ThrowsAsync<AssessmentValidationException>(
            () => engine.StartAsync(Guid.NewGuid(), "NotARealGoal"));

        // Generator không được gọi và không có bài đánh giá nào được lưu.
        Assert.Null(generator.LastRequest);
        Assert.Empty(await db.Assessments.ToListAsync());
    }
}
