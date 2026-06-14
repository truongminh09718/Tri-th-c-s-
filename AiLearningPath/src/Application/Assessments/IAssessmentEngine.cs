using AiLearningPath.Domain.Assessments;
using AiLearningPath.Domain.Common;
using AiLearningPath.Domain.Entities;

namespace AiLearningPath.Application.Assessments;

/// <summary>
/// Dịch vụ tạo và chấm bài đánh giá năng lực đầu vào (R5).
/// Điều phối việc sinh bộ câu hỏi qua <see cref="ExternalServices.IContentGenerator"/>
/// (bọc Gemini) và gọi domain logic thuần <see cref="AssessmentGrader"/> để chấm điểm.
/// Mọi thao tác gắn với <c>userId</c> của tài khoản đã xác thực để hỗ trợ
/// ownership-based authorization (R14).
/// </summary>
public interface IAssessmentEngine
{
    /// <summary>
    /// Bắt đầu một bài đánh giá cho một Learning Goal đã chọn (R5.1): sinh bộ câu hỏi
    /// tương ứng qua dịch vụ sinh nội dung AI và lưu <see cref="Assessment"/>.
    /// </summary>
    /// <exception cref="AssessmentValidationException">Khi Learning Goal không hợp lệ.</exception>
    /// <exception cref="AssessmentGenerationException">Khi nội dung câu hỏi sinh ra không hợp lệ.</exception>
    Task<Assessment> StartAsync(Guid userId, string learningGoal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nộp bài đánh giá đã hoàn thành (R5.2, R5.3, R5.4): kiểm tra đầy đủ câu trả lời,
    /// chấm bài, lưu <see cref="AssessmentResult"/> liên kết với hồ sơ sinh viên.
    /// </summary>
    /// <exception cref="AssessmentNotFoundException">Khi không tìm thấy bài đánh giá của sinh viên.</exception>
    /// <exception cref="AssessmentValidationException">Khi còn câu hỏi chưa được trả lời (R5.4).</exception>
    Task<AssessmentResult> SubmitAsync(
        Guid userId, Guid assessmentId, IReadOnlyList<Answer> answers, CancellationToken cancellationToken = default);
}

/// <summary>
/// Ngoại lệ phát sinh khi đầu vào bài đánh giá không hợp lệ (Learning Goal không hỗ trợ,
/// hoặc nộp bài còn câu hỏi trống — R5.4). Mang theo <see cref="ValidationResult"/> để
/// lớp API chuyển thành phản hồi lỗi 400 nhất quán.
/// </summary>
public sealed class AssessmentValidationException : Exception
{
    public AssessmentValidationException(ValidationResult validation)
        : base(validation.FirstError ?? "Dữ liệu bài đánh giá không hợp lệ.")
    {
        Validation = validation;
    }

    /// <summary>Chi tiết kết quả kiểm tra hợp lệ bị thất bại.</summary>
    public ValidationResult Validation { get; }
}

/// <summary>
/// Ngoại lệ phát sinh khi không tìm thấy bài đánh giá thuộc về sinh viên. Lớp API
/// chuyển thành phản hồi 404.
/// </summary>
public sealed class AssessmentNotFoundException : Exception
{
    public AssessmentNotFoundException(Guid assessmentId)
        : base($"Không tìm thấy bài đánh giá {assessmentId}.")
    {
        AssessmentId = assessmentId;
    }

    public Guid AssessmentId { get; }
}

/// <summary>
/// Ngoại lệ phát sinh khi nội dung câu hỏi do dịch vụ sinh nội dung AI trả về không thể
/// phân tích thành bộ câu hỏi hợp lệ. Lớp API chuyển thành phản hồi 502.
/// </summary>
public sealed class AssessmentGenerationException : Exception
{
    public AssessmentGenerationException(string message) : base(message)
    {
    }
}
