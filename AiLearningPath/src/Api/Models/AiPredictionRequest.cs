namespace AiLearningPath.Api.Models;

public sealed class AiPredictionRequest
{
    public double StudyHoursPerDay { get; set; }
    public double AttendanceRate { get; set; }
    public double MockTestScore { get; set; }
    public double HistoricalGpa { get; set; }
}
