using AiLearningPath.Application.Assessments;
using AiLearningPath.Application.Auth;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Paths;
using AiLearningPath.Application.Profiles;
using AiLearningPath.Domain.Assessments;
using AiLearningPath.Domain.Common;
using AiLearningPath.Infrastructure.Auth;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiLearningPath.Tests.Integration;

/// <summary>
/// Integration test cho luồng end-to-end của toàn hệ thống (Task 15.2):
/// đăng ký → đăng nhập → tạo hồ sơ + chọn Learning Goal → bắt đầu &amp; nộp bài đánh giá
/// → xây Learning DNA → sinh lộ trình học → xem dashboard → mô phỏng Academic Twin.
///
/// Các dịch vụ ngoài (Gemini qua <see cref="IContentGenerator"/>, ML Service qua
/// <see cref="IPredictionService"/>) được thay bằng stub xác định (deterministic),
/// nên test không phụ thuộc mạng. Toàn bộ trạng thái được lưu/đọc thật qua EF Core
/// (<see cref="AppDbContext"/>) bằng In-Memory provider — mỗi test dùng một database
/// riêng để cô lập.
///
/// Kiểm chứng các điểm tích hợp xuyên suốt: R2.3 (xác thực JWT cho tài khoản hợp lệ),
/// R5.1 (sinh câu hỏi qua Gemini mock), R7.1 (sinh lộ trình qua Gemini mock),
/// R9.1 (dự đoán qua ML mock) và R14.1 (mọi dữ liệu học tập gắn với đúng chủ sở hữu).
/// </summary>
public sealed class EndToEndFlowIntegrationTests
{
    private const string Goal = "FrontendDevelopment";

    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IOptions<JwtSettings> CreateJwtSettings() =>
        Options.Create(new JwtSettings
        {
            // Khóa đủ dài cho HMAC-SHA256 (>= 256 bit).
            Key = "end-to-end-integration-test-signing-key-0123456789",
            Issuer = "ai-learning-path-tests",
            Audience = "ai-learning-path-tests",
            ExpiryMinutes = 60
        });

    /// <summary>
    /// Stub xác định cho dịch vụ sinh nội dung AI (Gemini). Tùy theo loại nội dung yêu cầu:
    /// trả về payload JSON bộ câu hỏi cho bài đánh giá (R5.1), hoặc nội dung lộ trình gắn
    /// với Learning Goal cho path generator (R7.1). Ghi lại các yêu cầu để kiểm chứng.
    /// </summary>
    private sealed class StubContentGenerator : IContentGenerator
    {
        private readonly int _questionCount;
        private readonly List<ContentGenerationRequest> _requests = new();

        public StubContentGenerator(int questionCount) => _questionCount = questionCount;

        public IReadOnlyList<ContentGenerationRequest> Requests => _requests;

        public Task<ContentGenerationResult> GenerateAsync(
            ContentGenerationRequest request, CancellationToken cancellationToken = default)
        {
            _requests.Add(request);

            if (request.Kind == AssessmentContentKind.Assessment)
            {
                var questions = Enumerable.Range(1, _questionCount)
                    .Select(i => new GeneratedQuestion(
                        Id: Guid.NewGuid(),
                        SkillArea: $"{request.LearningGoal}-Skill-{i}",
                        Prompt: $"[{request.LearningGoal}] Câu hỏi số {i}?",
                        Options: new[] { "A", "B", "C", "D" },
                        CorrectOption: "A"))
                    .ToList();

                var json = JsonFieldSerializer.Serialize(new GeneratedAssessmentContent(questions));
                return Task.FromResult(new ContentGenerationResult(json));
            }

            // LearningPath (và mọi loại khác): trả về nội dung văn bản gắn với Learning Goal.
            var content = $"[{request.LearningGoal}] Nội dung lộ trình do AI sinh ra.";
            return Task.FromResult(new ContentGenerationResult(content));
        }
    }

    /// <summary>
    /// Stub xác định cho dịch vụ dự đoán ML: xác suất tăng theo số giờ học mỗi ngày
    /// (kẹp về [0, 1]). Ghi lại số lần gọi để kiểm chứng luồng end-to-end (R9.1).
    /// </summary>
    private sealed class StubPredictionService : IPredictionService
    {
        public int CallCount { get; private set; }

        public PredictionFeatures? LastFeatures { get; private set; }

        public Task<double> PredictSuccessProbabilityAsync(
            PredictionFeatures features, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastFeatures = features;
            var probability = Math.Clamp(features.HoursPerDay / 10d, 0d, 1d);
            return Task.FromResult(probability);
        }
    }

    // Luồng chính end-to-end: từ đăng nhập tới sinh lộ trình, xem dashboard và mô phỏng twin,
    // với Gemini/ML được mock và dữ liệu lưu/đọc thật qua EF Core.
    [Fact]
    public async Task FullJourney_FromRegistration_ToPathDashboardAndTwin_PersistsThroughEfCore()
    {
        using var db = CreateDb();
        var content = new StubContentGenerator(AssessmentDefaults.QuestionCount);
        var prediction = new StubPredictionService();

        // --- Bước 1: Đăng ký + đăng nhập (R1, R2) ---
        var authService = new AuthService(db, CreateJwtSettings());

        const string email = "student@e2e.local";
        const string password = "Sup3rSecret!";

        var register = await authService.RegisterAsync(email, password);
        Assert.True(register.Succeeded);
        var userId = register.UserId!.Value;

        // Tài khoản được lưu thật qua EF Core.
        Assert.True(await db.Users.AnyAsync(u => u.Id == userId && u.Email == email));

        var login = await authService.LoginAsync(email, password);
        Assert.True(login.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));

        // R2.3: JWT của tài khoản hợp lệ xác minh được và mang đúng định danh chủ sở hữu.
        var principal = authService.ValidateToken(login.Token!);
        Assert.NotNull(principal);
        var subjectClaim = principal!.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? principal.FindFirst("sub");
        Assert.NotNull(subjectClaim);
        Assert.Equal(userId.ToString(), subjectClaim!.Value);

        // --- Bước 2: Tạo hồ sơ + chọn Learning Goal (R3, R4) ---
        var profileService = new ProfileService(db);

        await profileService.CreateOrUpdateAsync(userId, new ProfileInput(
            FullName: "Nguyễn Văn A",
            StudentCode: "SV001",
            Major: "Công nghệ thông tin",
            LearningGoal: null,
            TargetScore: null,
            CareerGoal: "Frontend Developer",
            StudyHoursPerDay: 4));

        await profileService.SelectGoalAsync(userId, new LearningGoalSelection(Goal, TargetScore: null));

        var savedProfile = await profileService.GetAsync(userId);
        Assert.Equal(Goal, savedProfile.LearningGoal);
        Assert.Equal(4, savedProfile.StudyHoursPerDay);

        // --- Bước 3: Bắt đầu + nộp bài đánh giá → xây Learning DNA (R5, R6) ---
        // Wire assessment → DNA: khi nộp bài, AssessmentEngine tự kích hoạt LearningDnaEngine.
        var dnaEngine = new LearningDnaEngine(db);
        var assessmentEngine = new AssessmentEngine(db, content, dnaEngine);

        var assessment = await assessmentEngine.StartAsync(userId, Goal);
        Assert.Equal(AssessmentStatus.InProgress, assessment.Status);

        // R5.1: generator được gọi để sinh bộ câu hỏi theo đúng Learning Goal.
        Assert.Contains(content.Requests, r =>
            r.Kind == AssessmentContentKind.Assessment && r.LearningGoal == Goal);

        // Đọc lại bộ câu hỏi đã persist và trả lời đúng toàn bộ.
        var questions = JsonFieldSerializer
            .Deserialize<List<AssessmentQuestionItem>>(assessment.QuestionsJson)!;
        Assert.Equal(AssessmentDefaults.QuestionCount, questions.Count);

        var answers = questions
            .Select(q => new Answer(q.Id, q.CorrectOption))
            .ToList();

        var result = await assessmentEngine.SubmitAsync(userId, assessment.Id, answers);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(100d, result.Score); // tất cả câu trả lời đúng

        // Bài đánh giá chuyển sang trạng thái Completed và kết quả được persist.
        var persistedAssessment = await db.Assessments.AsNoTracking()
            .SingleAsync(a => a.Id == assessment.Id);
        Assert.Equal(AssessmentStatus.Completed, persistedAssessment.Status);

        // R6.1: Learning DNA Profile được xây tự động sau khi nộp bài và gắn đúng chủ sở hữu.
        var dna = await db.LearningDnaProfiles.AsNoTracking().SingleAsync(d => d.UserId == userId);
        Assert.Equal(userId, dna.UserId);

        // --- Bước 4: Sinh lộ trình học (R7) ---
        var pathGenerator = new PathGenerator(db, content);
        const int targetDays = 30;

        var path = await pathGenerator.GenerateAsync(userId, targetDays);
        Assert.Equal(userId, path.UserId);
        Assert.Equal(Goal, path.LearningGoal);
        Assert.Equal(targetDays, path.TargetDays);

        // R7.1: generator được gọi để sinh nội dung lộ trình.
        Assert.Contains(content.Requests, r => r.Kind == PathContentKind.LearningPath);

        // Lộ trình persist với phân cấp tháng → tuần → ngày, bao phủ đủ targetDays (R7.1, R7.2).
        var persistedPath = await db.LearningPaths
            .Include(p => p.Phases)
            .ThenInclude(ph => ph.Tasks)
            .AsNoTracking()
            .SingleAsync(p => p.Id == path.Id);
        var totalTasks = persistedPath.Phases.Sum(ph => ph.Tasks.Count);
        Assert.Equal(targetDays, totalTasks);

        // --- Bước 5: Xem dashboard (R8) ---
        var dashboardService = new ProgressDashboardService(db);
        var dashboardBefore = await dashboardService.GetDashboardAsync(userId);

        // Đã có lộ trình → dashboard không còn rỗng và phản ánh đúng tổng số nhiệm vụ.
        Assert.False(dashboardBefore.IsEmpty);
        Assert.Equal(targetDays, dashboardBefore.TotalTasks);
        Assert.Equal(0, dashboardBefore.CompletedTasks);
        Assert.InRange(dashboardBefore.CompletionRate, 0d, 1d);
        Assert.InRange(dashboardBefore.LearningScore, 0d, 100d);

        // Hoàn thành một nhiệm vụ → tỷ lệ hoàn thành không giảm (R8.2).
        var firstTask = await db.LearningTasks
            .Where(t => t.PathPhase!.LearningPath!.UserId == userId)
            .OrderBy(t => t.Id)
            .FirstAsync();

        var dashboardAfter = await dashboardService.CompleteTaskAsync(userId, firstTask.Id);
        Assert.Equal(1, dashboardAfter.CompletedTasks);
        Assert.True(dashboardAfter.CompletionRate >= dashboardBefore.CompletionRate);

        // --- Bước 6: Mô phỏng Academic Twin (R9) ---
        var twinService = new AcademicTwinService(db, prediction);

        var single = await twinService.SimulateAsync(userId, hoursPerDay: 4d);
        Assert.Equal(4d, single.HoursPerDay);
        Assert.InRange(single.SuccessProbability, 0d, 1d);

        // R9.1: dịch vụ dự đoán ML (mock) thực sự được gọi với đặc trưng dựng từ dữ liệu DB.
        Assert.True(prediction.CallCount >= 1);
        Assert.NotNull(prediction.LastFeatures);
        Assert.Equal(Goal, prediction.LastFeatures!.LearningGoal);
        Assert.Equal(result.Score, prediction.LastFeatures.CurrentLevelScore);

        // R9.2: mô phỏng nhiều mức thời lượng trả đủ kết quả và đơn điệu không giảm.
        var range = await twinService.SimulateRangeAsync(userId, new[] { 1d, 3d, 6d });
        Assert.Equal(3, range.Count);
        for (var i = 1; i < range.Count; i++)
        {
            Assert.True(range[i].SuccessProbability >= range[i - 1].SuccessProbability);
        }
    }

    // R14.1: dữ liệu học tập của một tài khoản không lẫn sang tài khoản khác qua toàn bộ luồng.
    [Fact]
    public async Task FullJourney_ForTwoUsers_KeepsLearningDataIsolatedPerOwner()
    {
        using var db = CreateDb();
        var content = new StubContentGenerator(AssessmentDefaults.QuestionCount);
        var authService = new AuthService(db, CreateJwtSettings());
        var profileService = new ProfileService(db);
        var dnaEngine = new LearningDnaEngine(db);
        var assessmentEngine = new AssessmentEngine(db, content, dnaEngine);
        var pathGenerator = new PathGenerator(db, content);

        var alice = (await authService.RegisterAsync("alice@e2e.local", "AlicePass123!")).UserId!.Value;
        var bob = (await authService.RegisterAsync("bob@e2e.local", "BobPass123!")).UserId!.Value;

        // Chỉ Alice đi hết luồng: hồ sơ → goal → assessment → DNA → path.
        await profileService.CreateOrUpdateAsync(alice, new ProfileInput(
            "Alice", "SV-A", "IT", null, null, "Frontend Developer", 4));
        await profileService.SelectGoalAsync(alice, new LearningGoalSelection(Goal, null));

        var assessment = await assessmentEngine.StartAsync(alice, Goal);
        var questions = JsonFieldSerializer
            .Deserialize<List<AssessmentQuestionItem>>(assessment.QuestionsJson)!;
        await assessmentEngine.SubmitAsync(
            alice, assessment.Id, questions.Select(q => new Answer(q.Id, q.CorrectOption)).ToList());
        await pathGenerator.GenerateAsync(alice, targetDays: 30);

        // Mọi dữ liệu học tập đều gắn với Alice; Bob không có dữ liệu nào (R14.1).
        Assert.All(await db.Assessments.AsNoTracking().ToListAsync(), a => Assert.Equal(alice, a.UserId));
        Assert.All(await db.AssessmentResults.AsNoTracking().ToListAsync(), r => Assert.Equal(alice, r.UserId));
        Assert.All(await db.LearningDnaProfiles.AsNoTracking().ToListAsync(), d => Assert.Equal(alice, d.UserId));
        Assert.All(await db.LearningPaths.AsNoTracking().ToListAsync(), p => Assert.Equal(alice, p.UserId));

        Assert.False(await db.Assessments.AsNoTracking().AnyAsync(a => a.UserId == bob));
        Assert.False(await db.LearningPaths.AsNoTracking().AnyAsync(p => p.UserId == bob));

        // Dashboard của Bob ở trạng thái rỗng vì chưa có dữ liệu học tập nào (R8.4 / R14.1).
        var bobDashboard = await new ProgressDashboardService(db).GetDashboardAsync(bob);
        Assert.True(bobDashboard.IsEmpty);
    }
}
