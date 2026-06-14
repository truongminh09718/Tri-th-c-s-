namespace AiLearningPath.Application.Career;

/// <summary>
/// Loại nội dung yêu cầu dịch vụ sinh nội dung AI (<see cref="ExternalServices.IContentGenerator"/>)
/// tạo ra cho lộ trình nghề nghiệp.
/// </summary>
public static class CareerContentKind
{
    public const string CareerPath = "CareerPath";
}

/// <summary>
/// Cấu trúc JSON do <see cref="ExternalServices.IContentGenerator"/> trả về khi sinh
/// lộ trình nghề nghiệp. Lớp Application phân tích payload này thành lộ trình kỹ năng
/// kèm đề xuất chứng chỉ và dự án (R10.2, R10.3).
/// </summary>
/// <param name="Skills">Danh sách kỹ năng của lộ trình nghề nghiệp.</param>
/// <param name="Certifications">Danh sách chứng chỉ liên quan được đề xuất.</param>
/// <param name="Projects">Danh sách dự án thực tế được gợi ý.</param>
public sealed record GeneratedCareerContent(
    IReadOnlyList<string>? Skills,
    IReadOnlyList<string>? Certifications,
    IReadOnlyList<string>? Projects);
