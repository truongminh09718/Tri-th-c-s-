using AiLearningPath.Application.Authorization;
using FsCheck;
using FsCheck.Xunit;

namespace AiLearningPath.Tests;

/// <summary>
/// Property-based tests cho <see cref="ResourceAuthorizer"/> (phân quyền theo chủ sở hữu dữ liệu).
/// Validates: Requirements 14.1, 14.2
/// </summary>
public class ResourceAuthorizerPropertyTests
{
    private static readonly IResourceAuthorizer Authorizer = new ResourceAuthorizer();

    // Feature: ai-learning-path, Property 19: Phân quyền theo chủ sở hữu dữ liệu
    // Với hai Guid bất kỳ, CanAccess trả về true KHI VÀ CHỈ KHI hai Guid bằng nhau.
    [Property(MaxTest = 100)]
    public bool CanAccess_ReturnsTrue_IffRequesterEqualsOwner(Guid requesterId, Guid resourceOwnerId)
    {
        bool actual = Authorizer.CanAccess(requesterId, resourceOwnerId);
        bool expected = requesterId == resourceOwnerId;
        return actual == expected;
    }

    // Feature: ai-learning-path, Property 19: Phân quyền theo chủ sở hữu dữ liệu
    // (a) Khi requesterId == resourceOwnerId thì luôn trả về true (R14.1).
    [Property(MaxTest = 100)]
    public bool CanAccess_ReturnsTrue_WhenRequesterIsOwner(Guid ownerId)
    {
        return Authorizer.CanAccess(ownerId, ownerId);
    }

    // Feature: ai-learning-path, Property 19: Phân quyền theo chủ sở hữu dữ liệu
    // (b) Khi requesterId != resourceOwnerId thì luôn trả về false (R14.2).
    [Property(MaxTest = 100)]
    public Property CanAccess_ReturnsFalse_WhenRequesterIsNotOwner(Guid requesterId, Guid resourceOwnerId)
    {
        // Chỉ xét các trường hợp hai Guid khác nhau; FsCheck sẽ sinh đủ mẫu nhờ ràng buộc.
        return (!Authorizer.CanAccess(requesterId, resourceOwnerId))
            .When(requesterId != resourceOwnerId);
    }
}
