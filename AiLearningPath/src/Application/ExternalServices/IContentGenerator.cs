namespace AiLearningPath.Application.ExternalServices;

/// <summary>
/// Yêu cầu sinh nội dung gửi tới dịch vụ sinh nội dung AI (Gemini).
/// Dùng chung cho việc sinh bộ câu hỏi đánh giá (R5.1), nội dung lộ trình học (R7.1)
/// và lộ trình kỹ năng nghề nghiệp (R10).
/// </summary>
/// <param name="Kind">Loại nội dung cần sinh (ví dụ: Assessment, LearningPath, CareerPath).</param>
/// <param name="LearningGoal">Mục tiêu học tập hoặc nghề nghiệp liên quan.</param>
/// <param name="Parameters">Tham số bổ sung cho prompt (số câu hỏi, trình độ, số ngày...).</param>
public sealed record ContentGenerationRequest(
    string Kind,
    string LearningGoal,
    IReadOnlyDictionary<string, string> Parameters);

/// <summary>
/// Kết quả sinh nội dung trả về từ dịch vụ AI. <see cref="Content"/> là payload thô
/// (thường là JSON) để các service ở lớp Application phân tích thành mô hình miền.
/// </summary>
/// <param name="Content">Nội dung được sinh ra (thường ở dạng JSON).</param>
public sealed record ContentGenerationResult(string Content);

/// <summary>
/// Trừu tượng hóa dịch vụ sinh nội dung AI (bọc Gemini API).
/// Được tách thành interface để có thể mock trong unit/property/integration test,
/// giúp kiểm thử logic nghiệp vụ độc lập với I/O mạng.
/// </summary>
public interface IContentGenerator
{
    /// <summary>
    /// Sinh nội dung theo yêu cầu. Triển khai cụ thể (Gemini) nằm ở lớp Infrastructure.
    /// </summary>
    Task<ContentGenerationResult> GenerateAsync(
        ContentGenerationRequest request,
        CancellationToken cancellationToken = default);
}
