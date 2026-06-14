namespace AiLearningPath.Domain.Common;

/// <summary>
/// Kiểu lỗi API nhất quán cho toàn hệ thống. Khi serialize sẽ có dạng:
/// <code>{ "error": { "code": "VALIDATION_ERROR", "message": "...", "details": {} } }</code>
/// Dùng <see cref="ApiErrorResponse"/> làm bao ngoài để tạo đúng cấu trúc trên.
/// </summary>
public sealed class ApiError
{
    public ApiError(string code, string message, IReadOnlyDictionary<string, object>? details = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        ArgumentNullException.ThrowIfNull(message);

        Code = code;
        Message = message;
        Details = details ?? EmptyDetails;
    }

    private static readonly IReadOnlyDictionary<string, object> EmptyDetails =
        new Dictionary<string, object>(0);

    /// <summary>Mã lỗi ổn định, không phụ thuộc ngôn ngữ (ví dụ: VALIDATION_ERROR).</summary>
    public string Code { get; }

    /// <summary>Mô tả lỗi rõ ràng cho người dùng/đối tác tích hợp.</summary>
    public string Message { get; }

    /// <summary>Thông tin bổ sung tùy chọn (trường lỗi, giá trị hợp lệ...).</summary>
    public IReadOnlyDictionary<string, object> Details { get; }
}

/// <summary>
/// Bao ngoài cho <see cref="ApiError"/> nhằm tạo body lỗi nhất quán dạng
/// <c>{ "error": { ... } }</c> khi trả về từ API.
/// </summary>
public sealed class ApiErrorResponse
{
    public ApiErrorResponse(ApiError error)
    {
        Error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public ApiError Error { get; }

    /// <summary>Tạo body lỗi từ các thành phần cơ bản.</summary>
    public static ApiErrorResponse Create(
        string code,
        string message,
        IReadOnlyDictionary<string, object>? details = null) =>
        new(new ApiError(code, message, details));
}
