using AiLearningPath.Domain.Common;

namespace AiLearningPath.Domain.Twin;

/// <summary>
/// Logic thuần kiểm tra tiên quyết cho mô phỏng AI Academic Twin (R9.4).
/// Không phụ thuộc I/O nên có thể kiểm thử bằng property-based testing.
/// </summary>
/// <remarks>
/// Hàm <see cref="CheckPrerequisites"/> có tính xác định (deterministic): cùng một
/// ngữ cảnh sinh viên luôn cho ra cùng một kết quả.
/// </remarks>
public static class TwinValidator
{
    /// <summary>
    /// Kiểm tra tiên quyết mô phỏng Academic Twin (R9.4). Chỉ hợp lệ khi và chỉ khi
    /// ngữ cảnh sinh viên có cả Learning Goal lẫn kết quả đánh giá (AssessmentResult).
    /// Khi thiếu một hoặc cả hai điều kiện, trả về kết quả không hợp lệ kèm thông báo
    /// liệt kê các bước tiên quyết còn thiếu.
    /// </summary>
    /// <param name="ctx">Ngữ cảnh học tập của sinh viên.</param>
    /// <returns>
    /// <see cref="ValidationResult.Success"/> khi đã đủ tiên quyết; ngược lại
    /// <see cref="ValidationResult.Failure(IEnumerable{string})"/> với các lỗi tương ứng.
    /// </returns>
    public static ValidationResult CheckPrerequisites(StudentContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var errors = new List<string>();

        if (!ctx.HasLearningGoal)
        {
            errors.Add("Vui lòng chọn Learning Goal trước khi mô phỏng Academic Twin.");
        }

        if (!ctx.HasAssessmentResult)
        {
            errors.Add("Vui lòng hoàn thành AI Assessment trước khi mô phỏng Academic Twin.");
        }

        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors);
    }
}
