using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AiLearningPath.Infrastructure.Persistence;

/// <summary>
/// Factory dùng tại design-time để công cụ EF Core (dotnet ef) tạo migration
/// mà không cần khởi động ứng dụng. Chuỗi kết nối thực tế được cấu hình qua DI
/// khi chạy ứng dụng (xem appsettings.json).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=AiLearningPath;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True");

        return new AppDbContext(optionsBuilder.Options);
    }
}
