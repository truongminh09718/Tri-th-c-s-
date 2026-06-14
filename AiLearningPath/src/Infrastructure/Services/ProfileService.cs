using AiLearningPath.Application.Profiles;
using AiLearningPath.Domain.Entities;
using AiLearningPath.Domain.Profiles;
using AiLearningPath.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Triển khai <see cref="IProfileService"/> trên nền EF Core (<see cref="AppDbContext"/>).
/// Điều phối nghiệp vụ và gọi domain logic thuần (<see cref="ProfileValidator"/>,
/// <see cref="LearningGoalCatalog"/>) để áp dụng các quy tắc hợp lệ trước khi lưu.
/// Mọi thao tác đều gắn với <c>userId</c> để hỗ trợ ownership-based authorization (R14).
/// </summary>
public sealed class ProfileService : IProfileService
{
    /// <summary>Tên trường số giờ học mỗi ngày (dùng cho <see cref="UpdateFieldAsync"/>).</summary>
    public const string StudyHoursField = nameof(Profile.StudyHoursPerDay);

    /// <summary>Tên trường Learning Goal (dùng cho <see cref="UpdateFieldAsync"/>).</summary>
    public const string LearningGoalField = nameof(Profile.LearningGoal);

    private readonly AppDbContext _db;

    public ProfileService(AppDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <inheritdoc />
    public async Task<Profile> CreateOrUpdateAsync(
        Guid userId, ProfileInput input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // R3.4: số giờ học phải nằm trong [0, 24].
        var hoursValidation = ProfileValidator.ValidateStudyHours(input.StudyHoursPerDay);
        if (!hoursValidation.IsValid)
        {
            throw new ProfileValidationException(hoursValidation);
        }

        // R4.4: nếu có Learning Goal thì phải thuộc danh mục hỗ trợ.
        if (!string.IsNullOrEmpty(input.LearningGoal))
        {
            var goalValidation = ProfileValidator.ValidateGoal(input.LearningGoal);
            if (!goalValidation.IsValid)
            {
                throw new ProfileValidationException(goalValidation);
            }
        }

        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new Profile { UserId = userId };
            _db.Profiles.Add(profile);
        }

        profile.FullName = input.FullName;
        profile.StudentCode = input.StudentCode;
        profile.Major = input.Major;
        profile.CareerGoal = input.CareerGoal;
        profile.StudyHoursPerDay = input.StudyHoursPerDay;
        profile.LearningGoal = input.LearningGoal ?? string.Empty;

        // R4.3: chỉ lưu điểm mục tiêu khi goal yêu cầu; ngược lại xóa điểm cũ.
        profile.TargetScore =
            !string.IsNullOrEmpty(input.LearningGoal) && LearningGoalCatalog.RequiresTargetScore(input.LearningGoal)
                ? input.TargetScore
                : null;

        await _db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    /// <inheritdoc />
    public async Task<Profile> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        return profile ?? throw new ProfileNotFoundException(userId);
    }

    /// <inheritdoc />
    public async Task<Profile> UpdateFieldAsync(
        Guid userId, string field, object? value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(field);

        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            throw new ProfileNotFoundException(userId);
        }

        switch (field)
        {
            case StudyHoursField:
                var hours = ConvertToDouble(value);
                // R3.4: áp dụng validation số giờ học.
                var hoursValidation = ProfileValidator.ValidateStudyHours(hours);
                if (!hoursValidation.IsValid)
                {
                    throw new ProfileValidationException(hoursValidation);
                }

                profile.StudyHoursPerDay = hours;
                break;

            case LearningGoalField:
                var goal = value?.ToString() ?? string.Empty;
                // R4.4: áp dụng validation Learning Goal.
                var goalValidation = ProfileValidator.ValidateGoal(goal);
                if (!goalValidation.IsValid)
                {
                    throw new ProfileValidationException(goalValidation);
                }

                profile.LearningGoal = goal;
                if (!LearningGoalCatalog.RequiresTargetScore(goal))
                {
                    profile.TargetScore = null;
                }

                break;

            case nameof(Profile.FullName):
                profile.FullName = value?.ToString() ?? string.Empty;
                break;

            case nameof(Profile.StudentCode):
                profile.StudentCode = value?.ToString() ?? string.Empty;
                break;

            case nameof(Profile.Major):
                profile.Major = value?.ToString() ?? string.Empty;
                break;

            case nameof(Profile.CareerGoal):
                profile.CareerGoal = value?.ToString() ?? string.Empty;
                break;

            case nameof(Profile.TargetScore):
                profile.TargetScore = value is null ? null : ConvertToNullableInt(value);
                break;

            default:
                throw new ProfileValidationException(
                    Domain.Common.ValidationResult.Failure($"Trường hồ sơ '{field}' không hợp lệ hoặc không được phép cập nhật."));
        }

        await _db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    /// <inheritdoc />
    public async Task<Profile> SelectGoalAsync(
        Guid userId, LearningGoalSelection selection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(selection);

        // R4.4: goal phải thuộc danh mục hỗ trợ.
        var goalValidation = ProfileValidator.ValidateGoal(selection.Goal);
        if (!goalValidation.IsValid)
        {
            throw new ProfileValidationException(goalValidation);
        }

        var requiresTargetScore = LearningGoalCatalog.RequiresTargetScore(selection.Goal);

        // R4.3: goal yêu cầu điểm mục tiêu thì phải cung cấp điểm.
        if (requiresTargetScore && selection.TargetScore is null)
        {
            throw new ProfileValidationException(
                Domain.Common.ValidationResult.Failure(
                    $"Mục tiêu học tập '{selection.Goal}' yêu cầu nhập điểm số mục tiêu."));
        }

        var profile = await _db.Profiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new Profile { UserId = userId };
            _db.Profiles.Add(profile);
        }

        profile.LearningGoal = selection.Goal;
        // R4.3: lưu điểm mục tiêu khi goal yêu cầu; ngược lại không lưu.
        profile.TargetScore = requiresTargetScore ? selection.TargetScore : null;

        await _db.SaveChangesAsync(cancellationToken);
        return profile;
    }

    private static double ConvertToDouble(object? value)
    {
        if (value is null)
        {
            throw new ProfileValidationException(
                Domain.Common.ValidationResult.Failure("Giá trị số giờ học không được để trống."));
        }

        try
        {
            return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            throw new ProfileValidationException(
                Domain.Common.ValidationResult.Failure("Giá trị số giờ học không phải là số hợp lệ."));
        }
    }

    private static int ConvertToNullableInt(object value)
    {
        try
        {
            return Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            throw new ProfileValidationException(
                Domain.Common.ValidationResult.Failure("Giá trị điểm số mục tiêu không phải là số nguyên hợp lệ."));
        }
    }
}
