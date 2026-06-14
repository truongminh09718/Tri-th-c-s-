using AiLearningPath.Application.LearningDna;
using AiLearningPath.Domain.Assessments;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Domain.LearningDna;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai <see cref="ILearningDnaEngine"/> trên nền EF Core (<see cref="AppDbContext"/>).
/// Điều phối nghiệp vụ và gọi domain logic thuần (<see cref="DnaBuilder"/>) để suy ra
/// hồ sơ Learning DNA, sau đó ánh xạ kết quả thuần (<see cref="LearningDnaResult"/>) sang
/// entity <see cref="LearningDnaProfile"/> (serialize các trường danh sách bằng
/// <see cref="JsonFieldSerializer"/>). Mọi thao tác đều gắn với <c>userId</c> để hỗ trợ
/// ownership-based authorization (R14).
/// </summary>
public sealed class LearningDnaEngine : ILearningDnaEngine
{
    private readonly AppDbContext _db;

    public LearningDnaEngine(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task<LearningDnaProfile> BuildAsync(
        Guid userId, AssessmentResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        // Lấy sở thích hồ sơ làm đầu vào thuần cho DnaBuilder (số giờ học/khung giờ ưa thích).
        // Khi sinh viên chưa có hồ sơ, dùng giá trị mặc định an toàn (0 giờ, khung mặc định).
        var profile = await _db.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        var profileInput = new DnaProfileInput(
            profile?.StudyHoursPerDay ?? 0d,
            PreferredStudyPeriod: null);

        // R6.1: suy ra Learning DNA từ kết quả đánh giá + sở thích hồ sơ (domain logic thuần).
        var gradingResult = ToGradingResult(result);
        var dna = DnaBuilder.Build(gradingResult, profileInput);

        // R6.2: lưu hồ sơ liên kết với tài khoản. Quan hệ 1-1, nên ghi đè hồ sơ hiện có.
        var entity = await _db.LearningDnaProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (entity is null)
        {
            entity = new LearningDnaProfile { UserId = userId };
            _db.LearningDnaProfiles.Add(entity);
        }

        ApplyToEntity(entity, dna);

        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public async Task<LearningDnaProfile> UpdateAsync(
        Guid userId, ProgressData newData, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(newData);

        var entity = await _db.LearningDnaProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (entity is null)
        {
            throw new LearningDnaNotFoundException(userId);
        }

        // R6.3: hợp nhất dữ liệu tiến độ mới vào hồ sơ hiện tại (domain logic thuần).
        var current = ToResult(entity);
        var merged = DnaBuilder.Merge(current, newData);

        ApplyToEntity(entity, merged);

        await _db.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public async Task<LearningDnaProfile> GetAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var entity = await _db.LearningDnaProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        return entity ?? throw new LearningDnaNotFoundException(userId);
    }

    /// <summary>
    /// Ánh xạ entity <see cref="AssessmentResult"/> sang mô hình thuần
    /// <see cref="AssessmentGradingResult"/> dùng làm đầu vào cho <see cref="DnaBuilder"/>.
    /// <see cref="DnaBuilder.Build"/> chỉ dùng Score, Strengths và Weaknesses, nên
    /// SkillBreakdown để rỗng.
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
    /// Ánh xạ entity <see cref="LearningDnaProfile"/> hiện tại sang mô hình thuần
    /// <see cref="LearningDnaResult"/> để hợp nhất bằng <see cref="DnaBuilder.Merge"/>.
    /// </summary>
    private static LearningDnaResult ToResult(LearningDnaProfile entity)
    {
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
    /// Ghi giá trị từ mô hình thuần <see cref="LearningDnaResult"/> vào entity, serialize
    /// các trường danh sách sang JSON (R6.2).
    /// </summary>
    private static void ApplyToEntity(LearningDnaProfile entity, LearningDnaResult dna)
    {
        entity.LearningStyle = dna.LearningStyle;
        entity.EffectiveHoursJson = JsonFieldSerializer.Serialize(dna.EffectiveHours);
        entity.LearningSpeed = dna.LearningSpeed;
        entity.FocusAbility = dna.FocusAbility;
        entity.StrengthsJson = JsonFieldSerializer.Serialize(dna.Strengths);
        entity.WeaknessesJson = JsonFieldSerializer.Serialize(dna.Weaknesses);
    }
}
