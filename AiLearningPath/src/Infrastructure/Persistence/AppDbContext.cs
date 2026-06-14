using AiLearningPath.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext cho AI Learning Path.
/// Áp dụng ràng buộc Email duy nhất (R1.1) và mọi entity dữ liệu học tập có UserId
/// để hỗ trợ ownership-based authorization (R14.1).
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<AssessmentResult> AssessmentResults => Set<AssessmentResult>();
    public DbSet<LearningDnaProfile> LearningDnaProfiles => Set<LearningDnaProfile>();
    public DbSet<LearningPath> LearningPaths => Set<LearningPath>();
    public DbSet<PathPhase> PathPhases => Set<PathPhase>();
    public DbSet<LearningTask> LearningTasks => Set<LearningTask>();
    public DbSet<CareerPath> CareerPaths => Set<CareerPath>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<ProgressSnapshot> ProgressSnapshots => Set<ProgressSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- User: Email duy nhất (R1.1) ---
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(256);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).IsRequired();

            // Quan hệ 1-1 Profile và LearningDnaProfile
            entity.HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<Profile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(u => u.LearningDnaProfile)
                .WithOne(d => d.User)
                .HasForeignKey<LearningDnaProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ 1-nhiều
            entity.HasMany(u => u.Assessments)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.LearningPaths)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.CareerPaths)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.StudySessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.ProgressSnapshots)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Profile: mỗi UserId là duy nhất (1-1) ---
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.UserId).IsUnique();
        });

        // --- Assessment ---
        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.UserId);
            entity.Property(a => a.QuestionsJson).HasColumnType("nvarchar(max)");

            // Quan hệ 1-1 với AssessmentResult
            entity.HasOne(a => a.Result)
                .WithOne(r => r.Assessment)
                .HasForeignKey<AssessmentResult>(r => r.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- AssessmentResult ---
        modelBuilder.Entity<AssessmentResult>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => r.AssessmentId).IsUnique();
            entity.Property(r => r.StrengthsJson).HasColumnType("nvarchar(max)");
            entity.Property(r => r.WeaknessesJson).HasColumnType("nvarchar(max)");

            // UserId của AssessmentResult không cấu hình quan hệ điều hướng tới User
            // để tránh nhiều đường cascade; chỉ là cột sở hữu.
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // --- LearningDnaProfile ---
        modelBuilder.Entity<LearningDnaProfile>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.UserId).IsUnique();
            entity.Property(d => d.EffectiveHoursJson).HasColumnType("nvarchar(max)");
            entity.Property(d => d.StrengthsJson).HasColumnType("nvarchar(max)");
            entity.Property(d => d.WeaknessesJson).HasColumnType("nvarchar(max)");
        });

        // --- LearningPath → PathPhase → LearningTask ---
        modelBuilder.Entity<LearningPath>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.UserId);

            entity.HasMany(p => p.Phases)
                .WithOne(ph => ph.LearningPath)
                .HasForeignKey(ph => ph.LearningPathId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PathPhase>(entity =>
        {
            entity.HasKey(ph => ph.Id);
            entity.HasIndex(ph => ph.LearningPathId);

            entity.HasMany(ph => ph.Tasks)
                .WithOne(t => t.PathPhase)
                .HasForeignKey(t => t.PathPhaseId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LearningTask>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.PathPhaseId);
        });

        // --- CareerPath ---
        modelBuilder.Entity<CareerPath>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.UserId);
            entity.Property(c => c.SkillsJson).HasColumnType("nvarchar(max)");
            entity.Property(c => c.CertificationsJson).HasColumnType("nvarchar(max)");
            entity.Property(c => c.ProjectsJson).HasColumnType("nvarchar(max)");
        });

        // --- StudySession ---
        modelBuilder.Entity<StudySession>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.UserId);
        });

        // --- ProgressSnapshot ---
        modelBuilder.Entity<ProgressSnapshot>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.UserId);
        });
    }
}
