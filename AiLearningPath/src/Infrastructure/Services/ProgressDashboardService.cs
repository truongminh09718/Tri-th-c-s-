using AiLearningPath.Application.Progress;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Domain.Progress;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai <see cref="IProgressDashboardService"/> trên nền EF Core (<see cref="AppDbContext"/>).
/// Điều phối nghiệp vụ tổng hợp tiến độ học tập (R8):
/// <list type="bullet">
///   <item><see cref="GetDashboardAsync"/> nạp lộ trình, phiên học, mốc tiến độ và kết quả
///   đánh giá của sinh viên rồi gọi domain logic thuần <see cref="ProgressCalculator"/>
///   tính Learning Score, tỷ lệ hoàn thành, tổng giờ học và dữ liệu biểu đồ (R8.1, R8.3).
///   Khi chưa có dữ liệu học tập, trả về trạng thái rỗng kèm hướng dẫn (R8.4).</item>
///   <item><see cref="CompleteTaskAsync"/> đánh dấu nhiệm vụ hoàn thành, ghi nhận mốc
///   tiến độ mới rồi trả về dashboard đã cập nhật (R8.2).</item>
/// </list>
/// Mọi thao tác đều gắn với <c>userId</c> để hỗ trợ ownership-based authorization (R14).
/// </summary>
public sealed class ProgressDashboardService : IProgressDashboardService
{
    /// <summary>
    /// Thông điệp hướng dẫn hiển thị khi sinh viên chưa có dữ liệu học tập (R8.4).
    /// </summary>
    public const string EmptyStateGuidance =
        "Bạn chưa có dữ liệu học tập. Hãy hoàn thành bài đánh giá năng lực và sinh lộ trình "
        + "học tập để bắt đầu theo dõi tiến độ.";

    private readonly AppDbContext _db;

    public ProgressDashboardService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task<DashboardView> GetDashboardAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        // Nạp toàn bộ nhiệm vụ trên các lộ trình của sinh viên để tính tỷ lệ hoàn thành.
        var tasks = await _db.LearningTasks
            .AsNoTracking()
            .Where(t => t.PathPhase!.LearningPath!.UserId == userId)
            .Select(t => new { t.Completed })
            .ToListAsync(cancellationToken);

        var sessions = await _db.StudySessions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new { s.StartedAt, s.DurationHours })
            .ToListAsync(cancellationToken);

        var snapshots = await _db.ProgressSnapshots
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Select(s => new { s.RecordedAt, s.LearningScore, s.CompletionRate })
            .ToListAsync(cancellationToken);

        // R8.4: không có bất kỳ dữ liệu học tập nào → trạng thái rỗng kèm hướng dẫn.
        if (tasks.Count == 0 && sessions.Count == 0 && snapshots.Count == 0)
        {
            return EmptyDashboard();
        }

        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.Completed);
        var completionRate = ProgressCalculator.ComputeCompletionRate(completedTasks, totalTasks);

        var sessionInputs = sessions
            .Select(s => new StudySessionInput(s.StartedAt, s.DurationHours))
            .ToList();
        var totalStudyHours = ProgressCalculator.ComputeTotalStudyHours(sessionInputs);

        // Điểm đánh giá gần nhất (nếu có) tham gia công thức Learning Score.
        var assessmentScore = await _db.AssessmentResults
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => (double?)r.Score)
            .FirstOrDefaultAsync(cancellationToken) ?? 0d;

        var learningScore = ProgressCalculator.ComputeLearningScore(
            new LearningScoreInput(
                completionRate,
                assessmentScore,
                ComputeStudyConsistency(sessionInputs)));

        var chart = ProgressCalculator.BuildChartData(
            snapshots
                .Select(s => new ProgressPoint(s.RecordedAt, s.LearningScore, s.CompletionRate))
                .ToList());

        return new DashboardView(
            learningScore,
            completionRate,
            totalStudyHours,
            completedTasks,
            totalTasks,
            chart,
            IsEmpty: false,
            Guidance: null);
    }

    /// <inheritdoc />
    public async Task<DashboardView> CompleteTaskAsync(
        Guid userId, Guid taskId, CancellationToken cancellationToken = default)
    {
        // Chỉ nạp nhiệm vụ thuộc lộ trình của chính sinh viên (ownership-based authorization, R14.1).
        var task = await _db.LearningTasks
            .FirstOrDefaultAsync(
                t => t.Id == taskId && t.PathPhase!.LearningPath!.UserId == userId,
                cancellationToken);

        if (task is null)
        {
            throw new DashboardTaskNotFoundException(taskId);
        }

        // R8.2: đánh dấu nhiệm vụ hoàn thành (idempotent — đánh dấu lại không làm thay đổi gì).
        if (!task.Completed)
        {
            task.Completed = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Tổng hợp lại dashboard sau khi cập nhật và ghi nhận một mốc tiến độ mới (R8.2).
        var dashboard = await GetDashboardAsync(userId, cancellationToken);

        _db.ProgressSnapshots.Add(new ProgressSnapshot
        {
            UserId = userId,
            RecordedAt = DateTime.UtcNow,
            LearningScore = dashboard.LearningScore,
            CompletionRate = dashboard.CompletionRate
        });
        await _db.SaveChangesAsync(cancellationToken);

        return dashboard;
    }

    /// <summary>
    /// Ước lượng mức độ đều đặn khi học từ các phiên học gần đây, quy về [0, 1] dùng làm
    /// một tín hiệu của Learning Score. Tính bằng tỷ lệ số ngày riêng biệt có học trên
    /// khoảng 7 ngày gần nhất.
    /// </summary>
    private static double ComputeStudyConsistency(IReadOnlyList<StudySessionInput> sessions)
    {
        if (sessions.Count == 0)
        {
            return 0d;
        }

        const int window = 7;
        var distinctDays = sessions
            .Select(s => s.StartedAt.Date)
            .Distinct()
            .Count();

        return Math.Clamp((double)distinctDays / window, 0d, 1d);
    }

    private static DashboardView EmptyDashboard() => new(
        LearningScore: 0d,
        CompletionRate: 0d,
        TotalStudyHours: 0d,
        CompletedTasks: 0,
        TotalTasks: 0,
        Chart: ChartData.Empty,
        IsEmpty: true,
        Guidance: EmptyStateGuidance);
}
