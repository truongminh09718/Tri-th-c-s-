using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;

namespace AiLearningPath.Application.Career;

/// <summary>
/// Dịch vụ định hướng nghề nghiệp — Career Path AI (R10).
/// Cung cấp danh mục nghề nghiệp được hỗ trợ (R10.1) và sinh lộ trình kỹ năng kèm
/// đề xuất chứng chỉ/dự án qua <see cref="ExternalServices.IContentGenerator"/> (bọc Gemini)
/// (R10.2, R10.3), rồi lưu <see cref="CareerPath"/> liên kết với hồ sơ sinh viên (R10.4).
/// Mọi thao tác gắn với <c>userId</c> của tài khoản đã xác thực để hỗ trợ
/// ownership-based authorization (R14).
/// </summary>
public interface ICareerPathService
{
    /// <summary>
    /// Trả về danh sách nghề nghiệp được hỗ trợ (R10.1): Frontend, Backend, Data Analyst,
    /// AI Engineer, Tester. Đây là dữ liệu tĩnh từ danh mục miền, không phụ thuộc I/O.
    /// </summary>
    IReadOnlyList<string> ListCareers();

    /// <summary>
    /// Sinh lộ trình nghề nghiệp cho một nghề được chọn (R10.2–R10.4): gọi dịch vụ sinh
    /// nội dung AI tạo lộ trình kỹ năng cùng đề xuất chứng chỉ/dự án, rồi lưu liên kết
    /// với hồ sơ sinh viên.
    /// </summary>
    /// <param name="userId">Định danh chủ sở hữu (tài khoản đã xác thực).</param>
    /// <param name="career">Nghề nghiệp được chọn (phải thuộc danh mục hỗ trợ).</param>
    /// <param name="cancellationToken">Token hủy thao tác.</param>
    /// <returns>Lộ trình nghề nghiệp đã được tạo và lưu.</returns>
    /// <exception cref="CareerValidationException">Khi nghề nghiệp không thuộc danh mục hỗ trợ (R10.1).</exception>
    /// <exception cref="CareerGenerationException">Khi nội dung lộ trình do dịch vụ AI trả về không hợp lệ.</exception>
    Task<CareerPath> GenerateAsync(
        Guid userId, string career, CancellationToken cancellationToken = default);
}

/// <summary>
/// Ngoại lệ phát sinh khi nghề nghiệp được chọn không thuộc danh mục hỗ trợ (R10.1).
/// Mang theo <see cref="ValidationResult"/> để lớp API chuyển thành phản hồi lỗi 400 nhất quán.
/// </summary>
public sealed class CareerValidationException : Exception
{
    public CareerValidationException(ValidationResult validation)
        : base(validation.FirstError ?? "Dữ liệu lộ trình nghề nghiệp không hợp lệ.")
    {
        Validation = validation;
    }

    /// <summary>Chi tiết kết quả kiểm tra hợp lệ bị thất bại.</summary>
    public ValidationResult Validation { get; }
}

/// <summary>
/// Ngoại lệ phát sinh khi nội dung lộ trình nghề nghiệp do dịch vụ sinh nội dung AI trả về
/// không hợp lệ (rỗng/không phân tích được). Lớp API chuyển thành phản hồi 502.
/// </summary>
public sealed class CareerGenerationException : Exception
{
    public CareerGenerationException(string message) : base(message)
    {
    }
}
