using AiLearningPath.Application.Assessments;
using AiLearningPath.Domain.Profiles;

namespace AiLearningPath.Tests.Assessments;

/// <summary>
/// Unit test cho <see cref="AssessmentQuestionBank"/> (R5.1): ngân hàng câu hỏi thật,
/// deterministic, theo từng Learning Goal được hỗ trợ. Bảo đảm mọi goal trong
/// <see cref="LearningGoalCatalog.Supported"/> đều có câu hỏi hợp lệ, đáp án đúng nằm trong
/// các lựa chọn, và việc lấy câu hỏi là xác định.
/// </summary>
public sealed class AssessmentQuestionBankTests
{
    // Mọi Learning Goal được hỗ trợ đều có câu hỏi trong ngân hàng (chức năng đánh giá thật).
    [Theory]
    [InlineData("TOEIC")]
    [InlineData("IELTS")]
    [InlineData("UniversitySubject")]
    [InlineData("FrontendDevelopment")]
    [InlineData("BackendDevelopment")]
    [InlineData("DataAnalyst")]
    [InlineData("AIEngineer")]
    public void EverySupportedGoal_HasQuestions(string goal)
    {
        Assert.True(AssessmentQuestionBank.HasQuestions(goal));

        var questions = AssessmentQuestionBank.GetQuestions(goal, AssessmentDefaults.QuestionCount);
        Assert.Equal(AssessmentDefaults.QuestionCount, questions.Count);
    }

    // Tất cả goal trong danh mục đều được phủ trong ngân hàng (không bỏ sót goal nào).
    [Fact]
    public void AllCatalogGoals_AreCoveredByBank()
    {
        foreach (var goal in LearningGoalCatalog.Supported)
        {
            Assert.True(
                AssessmentQuestionBank.HasQuestions(goal),
                $"Learning Goal '{goal}' chưa có câu hỏi trong ngân hàng.");
        }
    }

    // Mỗi câu hỏi hợp lệ: prompt/skill area không rỗng, có lựa chọn, đáp án đúng nằm trong lựa chọn.
    [Theory]
    [InlineData("TOEIC")]
    [InlineData("IELTS")]
    [InlineData("UniversitySubject")]
    [InlineData("FrontendDevelopment")]
    [InlineData("BackendDevelopment")]
    [InlineData("DataAnalyst")]
    [InlineData("AIEngineer")]
    public void AllQuestions_AreWellFormed(string goal)
    {
        var questions = AssessmentQuestionBank.GetQuestions(goal, AssessmentDefaults.QuestionCount);

        Assert.All(questions, q =>
        {
            Assert.False(string.IsNullOrWhiteSpace(q.SkillArea));
            Assert.False(string.IsNullOrWhiteSpace(q.Prompt));
            Assert.NotEmpty(q.Options);
            Assert.False(string.IsNullOrWhiteSpace(q.CorrectOption));
            Assert.Contains(q.CorrectOption, q.Options);
        });
    }

    // Lấy câu hỏi là xác định: cùng goal + count cho ra cùng kết quả.
    [Fact]
    public void GetQuestions_IsDeterministic()
    {
        var first = AssessmentQuestionBank.GetQuestions("FrontendDevelopment", 8);
        var second = AssessmentQuestionBank.GetQuestions("FrontendDevelopment", 8);

        Assert.Equal(first, second);
    }

    // Khi count vượt số câu có sẵn, ngân hàng lặp tuần hoàn để trả đủ số lượng.
    [Fact]
    public void GetQuestions_WhenCountExceedsBank_RepeatsToFill()
    {
        var questions = AssessmentQuestionBank.GetQuestions("TOEIC", 25);
        Assert.Equal(25, questions.Count);
    }

    // Goal không có trong ngân hàng trả về danh sách rỗng (để placeholder dùng fallback tổng hợp).
    [Fact]
    public void GetQuestions_ForUnknownGoal_ReturnsEmpty()
    {
        Assert.False(AssessmentQuestionBank.HasQuestions("NotARealGoal"));
        Assert.Empty(AssessmentQuestionBank.GetQuestions("NotARealGoal", 10));
    }

    // count <= 0 trả về rỗng.
    [Fact]
    public void GetQuestions_WithNonPositiveCount_ReturnsEmpty()
    {
        Assert.Empty(AssessmentQuestionBank.GetQuestions("IELTS", 0));
        Assert.Empty(AssessmentQuestionBank.GetQuestions("IELTS", -5));
    }
}
