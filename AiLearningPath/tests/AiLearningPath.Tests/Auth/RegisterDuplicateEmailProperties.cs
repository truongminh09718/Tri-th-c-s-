using AiLearningPath.Application.Auth;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Infrastructure.Auth;
using AiLearningPath.Infrastructure.Persistence;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AiLearningPath.Tests.Auth;

/// <summary>
/// Property-based test cho luồng đăng ký khi email đã tồn tại (R1.1, R1.2).
///
/// Dùng EF Core In-Memory provider để chạy <see cref="AuthService.RegisterAsync"/>
/// mà không cần SQL Server thật. Mỗi iteration tạo một database in-memory riêng
/// (tên ngẫu nhiên) để các lần chạy độc lập nhau.
///
/// Chạy tối thiểu 100 iteration với FsCheck.
/// </summary>
public class RegisterDuplicateEmailProperties
{
    /// <summary>
    /// Cấu hình JWT tối thiểu để khởi tạo <see cref="AuthService"/>.
    /// Đăng ký không sinh token nên các giá trị này chỉ cần hợp lệ về kiểu.
    /// </summary>
    private static IOptions<JwtSettings> JwtOptions => Options.Create(new JwtSettings
    {
        Key = "test-signing-key-which-is-long-enough-256bit!!",
        Issuer = "test-issuer",
        Audience = "test-audience",
        ExpiryMinutes = 60
    });

    /// <summary>
    /// Sinh một chuỗi email đã ở dạng chuẩn hóa (chữ thường, không khoảng trắng),
    /// khớp với cách <c>NormalizeEmail</c> xử lý đầu vào trong AuthService.
    /// </summary>
    private static Gen<string> NormalizedEmailGen =>
        from local in Gen.Choose(1, 10)
            .SelectMany(n => Gen.ArrayOf(n, Gen.Elements("abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray())))
        from domain in Gen.Choose(1, 8)
            .SelectMany(n => Gen.ArrayOf(n, Gen.Elements("abcdefghijklmnopqrstuvwxyz".ToCharArray())))
        select $"{new string(local)}@{new string(domain)}.com";

    /// <summary>Sinh một tập email phân biệt, không rỗng.</summary>
    private static Gen<string[]> DistinctEmailsGen =>
        from emails in Gen.NonEmptyListOf(NormalizedEmailGen)
        select emails.Distinct().ToArray();

    /// <summary>Sinh mật khẩu hợp lệ (>= 8 ký tự).</summary>
    private static Gen<string> ValidPasswordGen =>
        from length in Gen.Choose(PasswordMinLength, PasswordMinLength + 24)
        from chars in Gen.ArrayOf(length, Gen.Elements("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray()))
        select new string(chars);

    private const int PasswordMinLength = 8;

    private static AppDbContext NewInMemoryDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"auth-dup-{Guid.NewGuid():N}")
            .Options);

    // Feature: ai-learning-path, Property 3: Đăng ký email trùng bị từ chối
    [Property(MaxTest = 100)]
    public Property RegisterWithExistingEmail_IsAlwaysRejected_AndAccountCountUnchanged()
    {
        var arb = Arb.From(
            from emails in DistinctEmailsGen
            from password in ValidPasswordGen
            from index in Gen.Choose(0, emails.Length - 1)
            select (emails, password, index));

        return Prop.ForAll(arb, input =>
        {
            var (emails, password, index) = input;

            using var db = NewInMemoryDb();

            // Seed: tạo sẵn các tài khoản với mật khẩu đã băm.
            foreach (var email in emails)
            {
                db.Users.Add(new User
                {
                    Email = email,
                    PasswordHash = Domain.Auth.PasswordHasher.Hash(password)
                });
            }
            db.SaveChanges();

            var countBefore = db.Users.Count();
            var existingEmail = emails[index];

            var service = new AuthService(db, JwtOptions);
            var result = service.RegisterAsync(existingEmail, password).GetAwaiter().GetResult();

            var countAfter = db.Users.Count();

            bool rejected = !result.Succeeded
                && result.Error is not null
                && result.Error.Code == "EMAIL_ALREADY_USED";
            bool countUnchanged = countAfter == countBefore;

            return (rejected && countUnchanged)
                .Label($"rejected={rejected}, countBefore={countBefore}, countAfter={countAfter}, email={existingEmail}");
        });
    }
}
