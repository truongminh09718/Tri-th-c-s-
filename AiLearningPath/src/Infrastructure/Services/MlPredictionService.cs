using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AiLearningPath.Application.ExternalServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiLearningPath.Infrastructure.Services;

public sealed class MlPredictionService : IBatchPredictionService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly MlServiceOptions _options;
    private readonly ILogger<MlPredictionService> _logger;

    public MlPredictionService(
        HttpClient httpClient,
        IOptions<MlServiceOptions> options,
        ILogger<MlPredictionService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<double> PredictSuccessProbabilityAsync(
        PredictionFeatures features,
        CancellationToken cancellationToken = default)
    {
        var probabilities = await PredictSuccessProbabilitiesAsync(new[] { features }, cancellationToken);
        return probabilities[0];
    }

    public async Task<IReadOnlyList<double>> PredictSuccessProbabilitiesAsync(
        IReadOnlyList<PredictionFeatures> features,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(features);
        if (features.Count == 0)
        {
            return Array.Empty<double>();
        }

        var endpoint = (_options.Endpoint ?? string.Empty).TrimEnd('/');
        var requestUri = features.Count == 1 ? $"{endpoint}/predict" : $"{endpoint}/predict-batch";
        object body = features.Count == 1
            ? MapFeature(features[0])
            : new PredictBatchRequest(features.Select(MapFeature).ToList());

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds)));

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = JsonContent.Create(body, options: SerializerOptions)
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            request.Headers.Add("X-ML-Service-Key", _options.ApiKey);
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var probabilities = features.Count == 1
            ? new[]
            {
                (await response.Content.ReadFromJsonAsync<PredictResponse>(SerializerOptions, timeoutCts.Token)
                    .ConfigureAwait(false))?.Probability
                    ?? throw new JsonException("ML Service returned an empty prediction response.")
            }
            : (await response.Content.ReadFromJsonAsync<PredictBatchResponse>(SerializerOptions, timeoutCts.Token)
                .ConfigureAwait(false))?.Probabilities
                ?? throw new JsonException("ML Service returned an empty batch prediction response.");

        if (probabilities.Count != features.Count)
        {
            throw new JsonException("ML Service prediction count did not match request count.");
        }

        var clamped = probabilities.Select(p => Math.Clamp(p, 0d, 1d)).ToList();
        _logger.LogDebug("ML Service predicted {Count} probability value(s).", clamped.Count);
        return clamped;
    }

    private static PredictRequest MapFeature(PredictionFeatures features)
    {
        var goalType = string.IsNullOrWhiteSpace(features.LearningGoal) ? "GPA" : features.LearningGoal;
        var targetDays = features.TargetDays > 0 ? features.TargetDays : 90;
        return new PredictRequest(features.CurrentLevelScore, features.HoursPerDay, goalType, targetDays);
    }

    private sealed record PredictRequest(
        double CurrentLevelScore,
        double HoursPerDay,
        string GoalType,
        int TargetDays);

    private sealed record PredictResponse(
        [property: JsonPropertyName("probability")] double Probability);

    private sealed record PredictBatchRequest(IReadOnlyList<PredictRequest> Items);

    private sealed record PredictBatchResponse(
        [property: JsonPropertyName("probabilities")] IReadOnlyList<double> Probabilities);
}
