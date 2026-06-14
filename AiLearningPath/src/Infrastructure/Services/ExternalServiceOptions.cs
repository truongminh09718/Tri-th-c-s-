namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Cấu hình dịch vụ sinh nội dung AI (Gemini), bind từ section <c>"Gemini"</c> của
/// appsettings. Khi <see cref="ApiKey"/> và <see cref="Endpoint"/> chưa được cấu hình,
/// hệ thống dùng triển khai placeholder xác định (<see cref="PlaceholderContentGenerator"/>)
/// để ứng dụng vẫn chạy end-to-end mà không phụ thuộc mạng/khóa thật.
/// </summary>
public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    /// <summary>Khóa API Gemini. Để trống nghĩa là chưa cấu hình dịch vụ thật.</summary>
    public string? ApiKey { get; set; }

    /// <summary>Endpoint HTTP của dịch vụ sinh nội dung. Để trống nghĩa là chưa cấu hình.</summary>
    public string? Endpoint { get; set; }
}

/// <summary>
/// Cấu hình dịch vụ dự đoán ML (Python + Scikit-learn microservice), bind từ section
/// <c>"MlService"</c> của appsettings. Khi <see cref="Endpoint"/> chưa được cấu hình,
/// hệ thống dùng triển khai placeholder xác định
/// (<see cref="PlaceholderPredictionService"/>) để ứng dụng vẫn chạy end-to-end.
/// </summary>
public sealed class MlServiceOptions
{
    public const string SectionName = "MlService";

    /// <summary>Endpoint HTTP của ML Service. Để trống nghĩa là chưa cấu hình dịch vụ thật.</summary>
    public string? Endpoint { get; set; }
}
