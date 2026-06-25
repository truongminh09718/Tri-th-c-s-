using AiLearningPath.Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace AiLearningPath.Api.Controllers;

/// <summary>
/// Backend cho trang "Dự đoán AI" (wwwroot/aiprediction.html).
///
/// <para>
/// Cả hai chức năng (Dự đoán kết quả &amp; Mô phỏng xác suất đạt mục tiêu) đều BÁM THEO
/// Learning Goal trong hồ sơ người dùng (R4.1): client gửi kèm <c>GoalType</c> và
/// <c>TargetScore</c> đọc từ hồ sơ. Mỗi loại mục tiêu được quy về một thang điểm phù hợp:
/// <list type="bullet">
///   <item>TOEIC → thang 10..990</item>
///   <item>IELTS → thang 0..9</item>
///   <item>GPA / Môn đại học → thang 0..4</item>
///   <item>Frontend / Backend / Data Analyst / AI Engineer → điểm sẵn sàng kỹ năng 0..100</item>
/// </list>
/// Nhờ vậy sinh viên có mục tiêu "Data Analyst" sẽ thấy điểm kỹ năng, không phải TOEIC.
/// </para>
///
/// <para>
/// Toàn bộ công thức là deterministic (không ngẫu nhiên) và hệ số dương trên nỗ lực học,
/// nên cùng đầu vào luôn ra cùng kết quả và càng học nhiều thì dự đoán không giảm.
/// Controller tự tính trong C# nên trang LUÔN hoạt động, không phụ thuộc tiến trình ML.
/// </para>
/// </summary>
[ApiController]
[Route("api/aiprediction")]
[Produces("application/json")]
public sealed class AiPredictionController : ControllerBase
{
    private readonly ILogger<AiPredictionController> _logger;

    public AiPredictionController(ILogger<AiPredictionController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Dự đoán kết quả theo đúng Learning Goal của hồ sơ (R4.1). Trả về nhãn mục tiêu,
    /// điểm dự đoán theo thang phù hợp và mức mục tiêu để client hiển thị động.
    /// </summary>
    [HttpPost("predict")]
    [ProducesResponseType(typeof(GoalPredictionResult), StatusCodes.Status200OK)]
    public IActionResult Predict([FromBody] AiPredictionRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { status = "error", message = "Dữ liệu đầu vào không hợp lệ." });
        }

        var goal = GoalProfile.Resolve(request.GoalType, request.TargetScore);
        var predicted = goal.Predict(request);

        return Ok(new GoalPredictionResult(
            goal.GoalType,
            goal.Label,
            goal.MetricLabel,
            Math.Round(predicted, 2),
            goal.Format(predicted),
            goal.Format(goal.TargetScore),
            goal.Unit));
    }

    /// <summary>
    /// Mô phỏng xác suất đạt mục tiêu theo Learning Goal của hồ sơ (R9). Dùng đường cong
    /// logistic trên khoảng cách giữa điểm dự đoán và điểm mục tiêu của chính loại mục tiêu đó.
    /// </summary>
    [HttpPost("twin-simulation")]
    [ProducesResponseType(typeof(GoalSimulationResult), StatusCodes.Status200OK)]
    public IActionResult SimulateTwin([FromBody] AiPredictionRequest request)
    {
        if (request is null)
        {
            return BadRequest(new { status = "error", message = "Dữ liệu đầu vào không hợp lệ." });
        }

        var goal = GoalProfile.Resolve(request.GoalType, request.TargetScore);
        var predicted = goal.Predict(request);

        var gap = predicted - goal.TargetScore;
        var probability = 1.0 / (1.0 + Math.Exp(-gap / goal.LogisticScale));
        var percent = Math.Round(Math.Clamp(probability, 0d, 1d) * 100.0, 1);

        var target = $"{goal.Label} ({goal.Format(goal.TargetScore)})";
        return Ok(new GoalSimulationResult(
            goal.GoalType,
            goal.Label,
            target,
            goal.Format(predicted),
            percent));
    }

    /// <summary>
    /// Mô tả cách một Learning Goal được quy về thang điểm, công thức dự đoán deterministic,
    /// điểm mục tiêu mặc định và độ dốc logistic dùng cho mô phỏng xác suất.
    /// </summary>
    private sealed class GoalProfile
    {
        public required string GoalType { get; init; }
        public required string Label { get; init; }
        public required string MetricLabel { get; init; }
        public required string Unit { get; init; }
        public required double TargetScore { get; init; }
        public required double LogisticScale { get; init; }
        public required Func<AiPredictionRequest, double> Predict { get; init; }

        /// <summary>Định dạng điểm cho hiển thị (số nguyên hoặc một chữ số thập phân) kèm đơn vị.</summary>
        public string Format(double score)
        {
            var rounded = Math.Round(score, 1);
            var number = rounded % 1 == 0
                ? ((int)rounded).ToString(CultureInfo.InvariantCulture)
                : rounded.ToString("0.0", CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(Unit) ? number : $"{number}{Unit}";
        }

        /// <summary>
        /// Suy ra <see cref="GoalProfile"/> từ mã Learning Goal của hồ sơ (R4.1) và điểm mục
        /// tiêu tùy chọn. Mặc định về GPA khi hồ sơ chưa chọn mục tiêu.
        /// </summary>
        public static GoalProfile Resolve(string? goalType, double? targetScore)
        {
            var normalized = (goalType ?? string.Empty).Trim();

            return normalized switch
            {
                "TOEIC" => new GoalProfile
                {
                    GoalType = "TOEIC",
                    Label = "TOEIC",
                    MetricLabel = "Điểm TOEIC dự đoán",
                    Unit = "",
                    TargetScore = Positive(targetScore, 700.0),
                    LogisticScale = 60.0,
                    Predict = PredictToeic,
                },
                "IELTS" => new GoalProfile
                {
                    GoalType = "IELTS",
                    Label = "IELTS",
                    MetricLabel = "Band IELTS dự đoán",
                    Unit = "",
                    TargetScore = Positive(targetScore, 6.5),
                    LogisticScale = 0.8,
                    Predict = PredictIelts,
                },
                "GPA" or "UniversitySubject" => new GoalProfile
                {
                    GoalType = "GPA",
                    Label = normalized == "UniversitySubject" ? "Môn đại học (GPA)" : "GPA",
                    MetricLabel = "GPA dự đoán",
                    Unit = "",
                    TargetScore = Positive(targetScore, 3.2),
                    LogisticScale = 0.35,
                    Predict = PredictGpa,
                },
                "FrontendDevelopment" or "BackendDevelopment" or "DataAnalyst" or "AIEngineer" =>
                    new GoalProfile
                    {
                        GoalType = normalized,
                        Label = SkillLabel(normalized),
                        MetricLabel = "Mức sẵn sàng kỹ năng dự đoán",
                        Unit = "/100",
                        TargetScore = Positive(targetScore, 80.0),
                        LogisticScale = 12.0,
                        Predict = PredictSkillReadiness,
                    },
                // Hồ sơ chưa chọn mục tiêu => mặc định GPA để vẫn có dự đoán hữu ích.
                _ => new GoalProfile
                {
                    GoalType = "GPA",
                    Label = "GPA",
                    MetricLabel = "GPA dự đoán",
                    Unit = "",
                    TargetScore = Positive(targetScore, 3.2),
                    LogisticScale = 0.35,
                    Predict = PredictGpa,
                },
            };
        }

        private static double Positive(double? value, double fallback) =>
            value is > 0 ? value.Value : fallback;

        private static string SkillLabel(string goalType) => goalType switch
        {
            "FrontendDevelopment" => "Frontend Development",
            "BackendDevelopment" => "Backend Development",
            "DataAnalyst" => "Data Analyst",
            "AIEngineer" => "AI Engineer",
            _ => goalType,
        };
    }

    /// <summary>Ước lượng GPA (thang 0..4) deterministic — cùng công thức với ml-service.</summary>
    private static double PredictGpa(AiPredictionRequest r)
    {
        var gpa =
            0.55 * r.HistoricalGpa
            + 0.09 * Math.Min(r.StudyHoursPerDay, 8.0)
            + 0.010 * r.AttendanceRate
            + 0.06 * Math.Min(r.MockTestScore, 10.0);
        return Math.Round(Math.Clamp(gpa, 0d, 4d), 2);
    }

    /// <summary>Ước lượng TOEIC (thang 10..990) deterministic — cùng công thức với ml-service.</summary>
    private static double PredictToeic(AiPredictionRequest r)
    {
        var toeic =
            250.0
            + 55.0 * Math.Min(r.MockTestScore, 10.0)
            + 22.0 * Math.Min(r.StudyHoursPerDay, 8.0)
            + 1.6 * r.AttendanceRate;
        return Math.Round(Math.Clamp(toeic, 10d, 990d));
    }

    /// <summary>Ước lượng IELTS (thang 0..9) deterministic từ điểm thi thử + giờ học + chuyên cần.</summary>
    private static double PredictIelts(AiPredictionRequest r)
    {
        var ielts =
            2.5
            + 0.55 * Math.Min(r.MockTestScore, 9.0)
            + 0.18 * Math.Min(r.StudyHoursPerDay, 8.0)
            + 0.012 * r.AttendanceRate;
        return Math.Round(Math.Clamp(ielts, 0d, 9d) * 2, MidpointRounding.AwayFromZero) / 2; // làm tròn 0.5
    }

    /// <summary>
    /// Ước lượng "mức sẵn sàng kỹ năng" (thang 0..100) deterministic cho các mục tiêu nghề
    /// (Frontend/Backend/Data Analyst/AI Engineer). Giờ luyện tập (study hours) và bài kiểm tra
    /// thử là tín hiệu chính, kèm chuyên cần và nền tảng học vấn (GPA). Hệ số dương trên mọi
    /// yếu tố nỗ lực để dự đoán không giảm khi học nhiều hơn (đồng nhất với Requirement 18.5).
    /// </summary>
    private static double PredictSkillReadiness(AiPredictionRequest r)
    {
        var readiness =
            6.0 * Math.Min(r.StudyHoursPerDay, 8.0)
            + 0.25 * r.AttendanceRate
            + 2.5 * Math.Min(r.MockTestScore, 10.0)
            + 4.0 * Math.Clamp(r.HistoricalGpa, 0d, 4d);
        return Math.Round(Math.Clamp(readiness, 0d, 100d), 1);
    }

    /// <summary>Kết quả dự đoán theo mục tiêu của hồ sơ.</summary>
    public sealed record GoalPredictionResult(
        [property: System.Text.Json.Serialization.JsonPropertyName("goal_type")] string GoalType,
        [property: System.Text.Json.Serialization.JsonPropertyName("goal_label")] string GoalLabel,
        [property: System.Text.Json.Serialization.JsonPropertyName("metric_label")] string MetricLabel,
        [property: System.Text.Json.Serialization.JsonPropertyName("predicted_value")] double PredictedValue,
        [property: System.Text.Json.Serialization.JsonPropertyName("predicted_display")] string PredictedDisplay,
        [property: System.Text.Json.Serialization.JsonPropertyName("target_display")] string TargetDisplay,
        [property: System.Text.Json.Serialization.JsonPropertyName("unit")] string Unit);

    /// <summary>Kết quả mô phỏng xác suất đạt mục tiêu theo hồ sơ.</summary>
    public sealed record GoalSimulationResult(
        [property: System.Text.Json.Serialization.JsonPropertyName("goal_type")] string GoalType,
        [property: System.Text.Json.Serialization.JsonPropertyName("goal_label")] string GoalLabel,
        [property: System.Text.Json.Serialization.JsonPropertyName("target")] string Target,
        [property: System.Text.Json.Serialization.JsonPropertyName("predicted_display")] string PredictedDisplay,
        [property: System.Text.Json.Serialization.JsonPropertyName("probability_success_percent")] double ProbabilitySuccessPercent);
}
