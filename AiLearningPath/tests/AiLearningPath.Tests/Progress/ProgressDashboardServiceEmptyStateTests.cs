using AiLearningPath.Application.Progress;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Persistence;
using AiLearningPath.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Tests.Progress;

/// <summary>
/// Unit test cho <see cref="ProgressDashboardService.GetDashboardAsync"/> ở trạng thái rỗng (R8.4):
/// khi sinh viên chưa có bất kỳ dữ liệu học tập nào (không nhiệm vụ trên lộ trình, không phiên học,
/// không mốc tiến độ), dashboard phải trả về trạng thái rỗng kèm thông điệp hướng dẫn bắt đầu học,
/// với mọi chỉ số ở giá trị mặc định 0 và dữ liệu biểu đồ rỗng.
///
/// Dùng EF Core In-Memory provider với một database riêng cho mỗi test (tên ngẫu nhiên) để cô lập
/// trạng thái, theo đúng quy ước của <see cref="Profiles.ProfileServiceTests"/>.
/// </summary>
public sealed class ProgressDashboardServiceEmptyStateTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenNoLearningData_ReturnsEmptyStateWithGuidance()
    {
        using var db = CreateDb();
        var service = new ProgressDashboardService(db);
        var userId = Guid.NewGuid();

        var dashboard = await service.GetDashboardAsync(userId);

        // R8.4: trạng thái rỗng được đánh dấu rõ ràng và kèm hướng dẫn không rỗng.
        Assert.True(dashboard.IsEmpty);
        Assert.False(string.IsNullOrWhiteSpace(dashboard.Guidance));
        Assert.Equal(ProgressDashboardService.EmptyStateGuidance, dashboard.Guidance);
    }

    [Fact]
    public async Task GetDashboardAsync_WhenNoLearningData_ReturnsZeroedMetricsAndEmptyChart()
    {
        using var db = CreateDb();
        var service = new ProgressDashboardService(db);
        var userId = Guid.NewGuid();

        var dashboard = await service.GetDashboardAsync(userId);

        // Mọi chỉ số đều ở giá trị mặc định khi chưa có dữ liệu học tập.
        Assert.Equal(0d, dashboard.LearningScore);
        Assert.Equal(0d, dashboard.CompletionRate);
        Assert.Equal(0d, dashboard.TotalStudyHours);
        Assert.Equal(0, dashboard.CompletedTasks);
        Assert.Equal(0, dashboard.TotalTasks);

        // Dữ liệu biểu đồ rỗng (không có mốc tiến độ nào).
        Assert.Empty(dashboard.Chart.Weekly);
        Assert.Empty(dashboard.Chart.Monthly);
    }

    [Fact]
    public async Task GetDashboardAsync_IsScopedPerUser_OtherUsersDataDoesNotAffectEmptyState()
    {
        using var db = CreateDb();
        var service = new ProgressDashboardService(db);
        var owner = Guid.NewGuid();
        var otherUser = Guid.NewGuid();

        // Người dùng khác có dữ liệu học tập, nhưng không được tính vào dashboard của owner.
        db.StudySessions.Add(new StudySession
        {
            UserId = otherUser,
            StartedAt = DateTime.UtcNow,
            DurationHours = 4
        });
        db.ProgressSnapshots.Add(new ProgressSnapshot
        {
            UserId = otherUser,
            RecordedAt = DateTime.UtcNow,
            LearningScore = 80,
            CompletionRate = 0.5
        });
        await db.SaveChangesAsync();

        var dashboard = await service.GetDashboardAsync(owner);

        // Owner vẫn ở trạng thái rỗng vì không sở hữu dữ liệu nào.
        Assert.True(dashboard.IsEmpty);
        Assert.False(string.IsNullOrWhiteSpace(dashboard.Guidance));
        Assert.Equal(0d, dashboard.TotalStudyHours);
        Assert.Empty(dashboard.Chart.Weekly);
        Assert.Empty(dashboard.Chart.Monthly);
    }
}
