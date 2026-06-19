using AiLearningPath.Application.Study;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Tests.Study;

public sealed class StudyLessonServiceTests
{
    [Fact]
    public async Task GetTodayAsync_WithoutAssessment_RequiresAssessment()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var result = await service.GetTodayAsync(Guid.NewGuid());

        Assert.Equal(TodayLessonStatuses.RequiresAssessment, result.Status);
        Assert.Null(result.Lesson);
    }

    [Fact]
    public async Task GetTodayAsync_SelectsFirstIncompleteTaskFromLatestPath()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var completed = new LearningTask { Skill = "Cũ", Description = "Đã xong", Completed = true, EstimatedHours = 1 };
        var expected = new LearningTask { Skill = "Nền tảng", Description = "Bài tiếp theo", EstimatedHours = 2 };
        db.AssessmentResults.Add(new AssessmentResult { UserId = userId, Level = "Beginner" });
        db.LearningPaths.Add(new LearningPath
        {
            UserId = userId,
            LearningGoal = "web-development",
            TargetDays = 30,
            Phases =
            [
                new PathPhase
                {
                    MonthIndex = 1,
                    WeekIndex = 1,
                    Title = "Tuần 1",
                    Tasks = [completed, expected]
                }
            ]
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetTodayAsync(userId);

        Assert.Equal(TodayLessonStatuses.Ready, result.Status);
        Assert.Equal(expected.Id, result.Lesson!.TaskId);
        Assert.NotEmpty(result.Lesson.Materials);
        Assert.Equal(10, result.Lesson.Quizzes.Count);
        Assert.All(result.Lesson.Quizzes, quiz => Assert.InRange(quiz.Options.Count, 3, 4));
    }

    [Fact]
    public async Task CompleteAsync_CompletesTaskAndRecordsStudySession()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var task = new LearningTask { Skill = "HTML", Description = "Thực hành", EstimatedHours = 1 };
        db.AssessmentResults.Add(new AssessmentResult { UserId = userId, Level = "Beginner", Score = 70 });
        db.LearningPaths.Add(new LearningPath
        {
            UserId = userId,
            LearningGoal = "web-development",
            TargetDays = 30,
            Phases = [new PathPhase { MonthIndex = 1, WeekIndex = 1, Title = "Tuần 1", Tasks = [task] }]
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.CompleteAsync(
            userId,
            task.Id,
            new CompleteLessonRequest(25, 5, "Đã hiểu bài", true));

        Assert.True((await db.LearningTasks.FindAsync(task.Id))!.Completed);
        var session = Assert.Single(await db.StudySessions.ToListAsync());
        Assert.Equal(25d / 60d, session.DurationHours, 6);
        Assert.Equal(1d, result.CompletionRate);
        Assert.Equal(5, result.Rating);
    }

    [Fact]
    public async Task GetTodayAsync_IeltsPlaceholderPath_ReturnsRealSkillContent()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var grammarTask = new LearningTask
        {
            Skill = "Grammar",
            Description = "[IELTS] Nội dung lộ trình học tập mẫu (placeholder).",
            EstimatedHours = 2
        };
        db.AssessmentResults.Add(new AssessmentResult { UserId = userId, Level = "Intermediate" });
        db.LearningPaths.Add(new LearningPath
        {
            UserId = userId,
            LearningGoal = "IELTS",
            TargetDays = 30,
            Phases =
            [
                new PathPhase
                {
                    MonthIndex = 1,
                    WeekIndex = 1,
                    Title = "Tuần 1",
                    Tasks =
                    [
                        new LearningTask
                        {
                            Skill = "Listening",
                            Description = "[IELTS] Nội dung lộ trình học tập mẫu (placeholder).",
                            EstimatedHours = 2
                        },
                        grammarTask
                    ]
                }
            ]
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        var result = await service.GetTodayAsync(userId);

        Assert.Equal(TodayLessonStatuses.Ready, result.Status);
        Assert.DoesNotContain("placeholder", result.Lesson!.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("hội thoại IELTS", result.Lesson.Description);
        Assert.Equal(3, result.Lesson.Materials.Count);
        Assert.Contains(result.Lesson.Materials, material => material.Type == "Thực hành");
        Assert.Equal(10, result.Lesson.Quizzes.Count);
        Assert.Contains(result.Lesson.Quizzes, quiz =>
            quiz.Question.Contains("move on", StringComparison.OrdinalIgnoreCase));

        var selected = await service.GetTodayAsync(userId, grammarTask.Id);
        Assert.Equal(grammarTask.Id, selected.Lesson!.TaskId);
        Assert.Equal("Grammar", selected.Lesson.Skill);
        Assert.Contains("quá khứ hoàn thành", selected.Lesson.Description);
        Assert.Contains(selected.Lesson.Tasks, task => task.Id == grammarTask.Id && task.IsToday);
    }

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static StudyLessonService CreateService(AppDbContext db) =>
        new(db, new ProgressDashboardService(db));
}
