namespace AiLearningPath.Application.Authorization;

/// <summary>
/// Triển khai thuần (pure) của <see cref="IResourceAuthorizer"/>.
/// <see cref="CanAccess"/> là hàm thuần, không phụ thuộc I/O nên dễ kiểm thử bằng
/// property-based testing (Property 19).
/// </summary>
public sealed class ResourceAuthorizer : IResourceAuthorizer
{
    /// <inheritdoc />
    public bool CanAccess(Guid requesterId, Guid resourceOwnerId) =>
        requesterId == resourceOwnerId;
}
