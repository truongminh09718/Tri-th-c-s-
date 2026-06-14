using AiLearningPath.Domain.Entities;
using AiLearningPath.Domain.LearningDna;

namespace AiLearningPath.Application.LearningDna;

/// <summary>
/// Dịch vụ xây dựng, cập nhật và truy xuất Learning DNA Profile (R6).
/// Điều phối nghiệp vụ và gọi domain logic thuần (<see cref="DnaBuilder"/>) để suy ra
/// hồ sơ học tập, đồng thời gắn mọi thao tác với <c>userId</c> của tài khoản đã xác thực
/// nhằm hỗ trợ ownership-based authorization (R14).
/// </summary>
public interface ILearningDnaEngine
{
    /// <summary>
    /// Tạo (hoặc tạo lại) Learning DNA Profile từ một kết quả đánh giá đã có và lưu
    /// liên kết với tài khoản của sinh viên (R6.1, R6.2). Mỗi sinh viên có duy nhất một
    /// hồ sơ DNA (quan hệ 1-1), nên lời gọi lặp lại sẽ ghi đè hồ sơ hiện tại.
    /// </summary>
    /// <param name="userId">Định danh chủ sở hữu (tài khoản đã xác thực).</param>
    /// <param name="result">Kết quả chấm bài đánh giá năng lực của sinh viên.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Learning DNA Profile đã được tạo và lưu.</returns>
    Task<LearningDnaProfile> BuildAsync(
        Guid userId, AssessmentResult result, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật Learning DNA Profile hiện tại từ dữ liệu tiến độ học tập mới (R6.3).
    /// </summary>
    /// <param name="userId">Định danh chủ sở hữu (tài khoản đã xác thực).</param>
    /// <param name="newData">Dữ liệu tiến độ học tập mới được ghi nhận.</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Learning DNA Profile đã cập nhật.</returns>
    /// <exception cref="LearningDnaNotFoundException">Khi sinh viên chưa có hồ sơ DNA.</exception>
    Task<LearningDnaProfile> UpdateAsync(
        Guid userId, ProgressData newData, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trả về Learning DNA Profile hiện tại của sinh viên (R6.4).
    /// </summary>
    /// <param name="userId">Định danh chủ sở hữu (tài khoản đã xác thực).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Learning DNA Profile hiện tại.</returns>
    /// <exception cref="LearningDnaNotFoundException">Khi sinh viên chưa có hồ sơ DNA.</exception>
    Task<LearningDnaProfile> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Ngoại lệ phát sinh khi không tìm thấy Learning DNA Profile của sinh viên (R6.4).
/// Lớp API chuyển thành phản hồi <c>404 Not Found</c>.
/// </summary>
public sealed class LearningDnaNotFoundException : Exception
{
    public LearningDnaNotFoundException(Guid userId)
        : base($"Không tìm thấy Learning DNA Profile cho người dùng {userId}.")
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}
