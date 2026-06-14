using System.Globalization;
using AiLearningPath.Application.ExternalServices;
using AiLearningPath.Application.Paths;
using AiLearningPath.Domain.Assessments;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Domain.LearningDna;
using AiLearningPath.Domain.Paths;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai <see cref="IPathGenerator"/> trên nền EF Core (<see cref="AppDbContext"/>).
/// Điều phối nghiệp vụ sinh lộ trình học tập cá nhân hóa (R7):
/// <list type="bullet">
///   <item>Kiểm tra điều kiện tiên quyết qua <see cref="PathBuilder.CheckPrerequisites"/> —
///   yêu cầu sinh viên đã hoàn thành AI Assessment (R7.4).</item>
///   <item>Gọi <see cref="IContentGenerator"/> (bọc Gemini) sinh nội dung lộ trình theo
///   Learning Goal (R7.1).</item>
///   <item>Xây cấu trúc tháng → tuần → ngày bằng <see cref="PathBuilder.Build"/> (R7.1, R7.2)
///   và ước lượng tính khả thi bằng <see cref="PathBuilder.AssessFeasibility"/> (R7.5).</item>
///   <item>Ánh xạ <see cref="PathPlan"/> sang entity <see cref="LearningPath"/> →
///   <see cref="PathPhase"/> → <see cref="LearningTask"/> và lưu liên kết với hồ sơ
///   sinh viên kèm cờ <c>FeasibilityWarning</c> (R7.3, R7.5).</item>
/// </list>
/// Mọi thao tác đều gắn với <c>userId</c> để hỗ trợ ownership-based authorization (R14).
/// </summary>
public sealed class PathGenerator : IPathGenerator
{
    private readonly AppDbContext _db;
    private readonly IContentGenerator _contentGenerator;

    public PathGenerator(AppDbContext db, IContentGenerator contentGenerator)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _contentGenerator = contentGenerator ?? throw new ArgumentNullException(nameof(contentGenerator));
    }

    /// <inheritdoc />
    public async Task<LearningPath> GenerateAsync(
        Guid userId, int targetDays, CancellationToken cancellationToken = default)
    {
        // Khoảng thời gian mục tiêu phải hợp lệ trước khi xây lộ trình.
        if (targetDays < 1)
        {
            throw new PathValidationException(
                ValidationResult.Failure("Khoảng thời gian mục tiêu (số ngày) phải lớn hơn hoặc bằng 1."));
        }

        // Hồ sơ cung cấp Learning Goal và số giờ học khả dụng mỗi ngày (cho R7.5).
        var profile = await _db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        // Kết quả đánh giá là điều kiện tiên quyết (R7.4) và nguồn điểm mạnh/yếu cho lộ trình.
        var assessmentResult = await _db.AssessmentResults
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId, cancellationToken);

        // R7.4: kiểm tra tiên quyết bằng domain logic thuần.
        var context = new StudentContext(
            HasLearningGoal: !string.IsNullOrWhiteSpace(profile?.LearningGoal),
            HasAssessmentResult: assessmentResult is not null);

        var prerequisites = PathBuilder.CheckPrerequisites(context);
        if (!prerequisites.IsValid)
        {
            throw new PathValidationException(prerequisites);
        }

        var learningGoal = profile?.LearningGoal ?? string.Empty;
        var studyHoursPerDay = profile?.StudyHoursPerDay ?? 0d;

        // Ánh xạ kết quả đánh giá và Learning DNA sang mô hình thuần cho PathBuilder.
        var gradingResult = ToGradingResult(assessmentResult!);
        var dna = await LoadDnaResultAsync(userId, cancellationToken);

        // R7.1: sinh nội dung lộ trình qua dịch vụ sinh nội dung AI (Gemini).
        var request = new ContentGenerationRequest(
            PathContentKind.LearningPath,
            learningGoal,
            new Dictionary<string, string>
            {
                ["targetDays"] = targetDays.ToString(CultureInfo.InvariantCulture),
                ["level"] = gradingResult.Level
            });

        var generated = await _contentGenerator.GenerateAsync(request, cancellationToken);
        var aiContent = generated?.Content;
        if (string.IsNullOrWhiteSpace(aiContent))
        {
            throw new PathGenerationException(
                "Dịch vụ AI không sinh được nội dung lộ trình cho Learning Goal đã chọn.");
        }

        // R7.1, R7.2: xây cấu trúc lộ trình tháng → tuần → ngày (domain logic thuần).
        var plan = PathBuilder.Build(
            learningGoal,
            gradingResult,
            dna,
            targetDays,
            PathGenerationDefaults.EstimatedDailyHours,
            aiContent);

        // R7.5: ước lượng tính khả thi dựa trên số giờ học khả dụng mỗi ngày.
        var feasibility = PathBuilder.AssessFeasibility(plan, studyHoursPerDay);

        // R7.3: ánh xạ sang entity và lưu liên kết với hồ sơ sinh viên kèm cảnh báo.
        var entity = MapToEntity(userId, plan, feasibility);
        _db.LearningPaths.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        return entity;
    }

    /// <summary>
    /// Nạp Learning DNA Profile của sinh viên và ánh xạ sang mô hình thuần
    /// <see cref="LearningDnaResult"/>. Khi sinh viên chưa có hồ sơ DNA, trả về một
    /// hồ sơ rỗng an toàn để <see cref="PathBuilder.Build"/> vẫn hoạt động.
    /// </summary>
    private async Task<LearningDnaResult> LoadDnaResultAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entity = await _db.LearningDnaProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (entity is null)
        {
            return new LearningDnaResult(
                LearningStyle: string.Empty,
                EffectiveHours: Array.Empty<string>(),
                LearningSpeed: 1d,
                FocusAbility: 0d,
                Strengths: Array.Empty<string>(),
                Weaknesses: Array.Empty<string>());
        }

        var effectiveHours = JsonFieldSerializer.Deserialize<List<string>>(entity.EffectiveHoursJson)
            ?? new List<string>();
        var strengths = JsonFieldSerializer.Deserialize<List<string>>(entity.StrengthsJson)
            ?? new List<string>();
        var weaknesses = JsonFieldSerializer.Deserialize<List<string>>(entity.WeaknessesJson)
            ?? new List<string>();

        return new LearningDnaResult(
            entity.LearningStyle,
            effectiveHours,
            entity.LearningSpeed,
            entity.FocusAbility,
            strengths,
            weaknesses);
    }

    /// <summary>
    /// Ánh xạ entity <see cref="AssessmentResult"/> sang mô hình thuần
    /// <see cref="AssessmentGradingResult"/> dùng làm đầu vào cho <see cref="PathBuilder.Build"/>.
    /// <see cref="PathBuilder.Build"/> chỉ dùng Strengths/Weaknesses (và Level cho prompt),
    /// nên SkillBreakdown để rỗng.
    /// </summary>
    private static AssessmentGradingResult ToGradingResult(AssessmentResult result)
    {
        var strengths = JsonFieldSerializer.Deserialize<List<string>>(result.StrengthsJson)
            ?? new List<string>();
        var weaknesses = JsonFieldSerializer.Deserialize<List<string>>(result.WeaknessesJson)
            ?? new List<string>();

        return new AssessmentGradingResult(
            result.Level,
            result.Score,
            strengths,
            weaknesses,
            Array.Empty<SkillAreaScore>());
    }

    /// <summary>
    /// Ánh xạ lộ trình thuần (<see cref="PathPlan"/>) sang phân cấp entity
    /// <see cref="LearningPath"/> → <see cref="PathPhase"/> → <see cref="LearningTask"/>,
    /// gắn cờ <c>FeasibilityWarning</c> theo kết quả ước lượng khả thi (R7.5).
    /// </summary>
    private static LearningPath MapToEntity(Guid userId, PathPlan plan, FeasibilityResult feasibility)
    {
        var entity = new LearningPath
        {
            UserId = userId,
            LearningGoal = plan.LearningGoal,
            TargetDays = plan.TargetDays,
            FeasibilityWarning = feasibility.HasWarning
        };

        foreach (var phase in plan.Phases)
        {
            var phaseEntity = new PathPhase
            {
                LearningPathId = entity.Id,
                MonthIndex = phase.MonthIndex,
                WeekIndex = phase.WeekIndex,
                Title = phase.Title
            };

            foreach (var day in phase.Days)
            {
                phaseEntity.Tasks.Add(new LearningTask
                {
                    PathPhaseId = phaseEntity.Id,
                    Skill = day.Task.Skill,
                    Description = day.Task.Description,
                    EstimatedHours = day.Task.EstimatedHours,
                    Completed = false
                });
            }

            entity.Phases.Add(phaseEntity);
        }

        return entity;
    }
}
