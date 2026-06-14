namespace AiLearningPath.Application.Authorization;

/// <summary>
/// Phân quyền theo chủ sở hữu dữ liệu (ownership-based authorization, R14).
/// Một người yêu cầu (requester) chỉ được phép truy cập tài nguyên khi họ chính là
/// chủ sở hữu của tài nguyên đó.
/// </summary>
public interface IResourceAuthorizer
{
    /// <summary>
    /// Trả về <c>true</c> khi và chỉ khi <paramref name="requesterId"/> trùng với
    /// <paramref name="resourceOwnerId"/> (người yêu cầu là chủ sở hữu tài nguyên).
    /// </summary>
    /// <param name="requesterId">Định danh tài khoản đã xác thực đang yêu cầu truy cập.</param>
    /// <param name="resourceOwnerId">Định danh chủ sở hữu của tài nguyên được yêu cầu.</param>
    bool CanAccess(Guid requesterId, Guid resourceOwnerId);
}
