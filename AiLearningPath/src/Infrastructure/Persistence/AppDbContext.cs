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
    public DbSet<AiInteractionLog> AiInteractionLogs => Set<AiInteractionLog>();
    public DbSet<AiCacheEntry> AiCacheEntries => Set<AiCacheEntry>();
    public DbSet<AiFeedback> AiFeedback => Set<AiFeedback>();
    public DbSet<TutorConversation> TutorConversations => Set<TutorConversation>();
    public DbSet<TutorMessage> TutorMessages => Set<TutorMessage>();
    public DbSet<StudySchedule> StudySchedules => Set<StudySchedule>();
    public DbSet<StudyScheduleItem> StudyScheduleItems => Set<StudyScheduleItem>();
    public DbSet<AdaptationEvent> AdaptationEvents => Set<AdaptationEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Kiểu cột cho các trường JSON lớn khác nhau giữa các nhà cung cấp: SQL Server dùng
        // "nvarchar(max)", còn SQLite/InMemory dùng "TEXT". Chọn theo provider để EnsureCreated
        // sinh đúng DDL (SQLite không hiểu "nvarchar(max)").
        var largeText = Database.IsSqlServer() ? "nvarchar(max)" : "TEXT";

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

            entity.HasMany<AiFeedback>()
                .WithOne(f => f.User)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany<TutorConversation>()
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany<StudySchedule>()
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
            entity.Property(a => a.QuestionsJson).HasColumnType(largeText);

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
            entity.Property(r => r.StrengthsJson).HasColumnType(largeText);
            entity.Property(r => r.WeaknessesJson).HasColumnType(largeText);
            entity.Property(r => r.SkillBreakdownJson).HasColumnType(largeText);

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
            entity.Property(d => d.EffectiveHoursJson).HasColumnType(largeText);
            entity.Property(d => d.StrengthsJson).HasColumnType(largeText);
            entity.Property(d => d.WeaknessesJson).HasColumnType(largeText);
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
            entity.Property(c => c.SkillsJson).HasColumnType(largeText);
            entity.Property(c => c.CertificationsJson).HasColumnType(largeText);
            entity.Property(c => c.ProjectsJson).HasColumnType(largeText);
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

        modelBuilder.Entity<AiInteractionLog>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => l.UserId);
            entity.HasIndex(l => l.CorrelationId);
            entity.Property(l => l.Operation).HasMaxLength(80);
            entity.Property(l => l.Provider).HasMaxLength(80);
            entity.Property(l => l.Status).HasMaxLength(80);
        });

        modelBuilder.Entity<AiCacheEntry>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.CacheKey).IsUnique();
            entity.HasIndex(c => c.ExpiresAt);
            entity.Property(c => c.Content).HasColumnType(largeText);
        });

        modelBuilder.Entity<AiFeedback>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.HasIndex(f => f.UserId);
            entity.Property(f => f.Comment).HasColumnType(largeText);
        });

        modelBuilder.Entity<TutorConversation>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.UserId);
            entity.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.TutorConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TutorMessage>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => m.UserId);
            entity.Property(m => m.Content).HasColumnType(largeText);
        });

        modelBuilder.Entity<StudySchedule>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.UserId);
            entity.HasMany(s => s.Items)
                .WithOne(i => i.Schedule)
                .HasForeignKey(i => i.StudyScheduleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<StudyScheduleItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.HasIndex(i => i.StudyScheduleId);
        });

        modelBuilder.Entity<AdaptationEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.LearningPathId);
            entity.Property(e => e.AddedTasksJson).HasColumnType(largeText);
            entity.Property(e => e.FocusSkillsJson).HasColumnType(largeText);
            entity.HasOne(e => e.LearningPath)
                .WithMany()
                .HasForeignKey(e => e.LearningPathId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
