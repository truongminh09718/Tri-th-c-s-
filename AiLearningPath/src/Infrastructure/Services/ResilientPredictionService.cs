using System.Text.Json;
using AiLearningPath.Application.ExternalServices;
using Microsoft.Extensions.Logging;

namespace AiLearningPath.Infrastructure.Services;

/// <summary>
/// Resilient decorator for <see cref="IPredictionService"/>.
/// Transient provider failures, internal timeouts and malformed responses fall back to
/// <see cref="PlaceholderPredictionService"/> so Academic Twin still receives a valid probability.
/// Caller-triggered cancellation is rethrown so disconnected requests and shutdown pressure remain
/// visible upstream instead of doing hidden fallback work.
/// </summary>
public sealed class ResilientPredictionService : IBatchPredictionService
{
    private readonly MlPredictionService _primary;
    private readonly PlaceholderPredictionService _fallback;
    private readonly ILogger<ResilientPredictionService> _logger;

    public ResilientPredictionService(
        MlPredictionService primary,
        PlaceholderPredictionService fallback,
        ILogger<ResilientPredictionService> logger)
    {
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
        _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<double> PredictSuccessProbabilityAsync(
        PredictionFeatures features, CancellationToken cancellationToken = default)
    {
        var probabilities = await PredictSuccessProbabilitiesAsync(new[] { features }, cancellationToken);
        return probabilities[0];
    }

    public async Task<IReadOnlyList<double>> PredictSuccessProbabilitiesAsync(
        IReadOnlyList<PredictionFeatures> features,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(features);

        try
        {
            return await _primary
                .PredictSuccessProbabilitiesAsync(features, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug(
                ex,
                "Prediction request was canceled by the caller; skipping fallback.");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "ML Service returned an HTTP error; using fallback prediction.");
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(
                ex,
                "ML Service call timed out internally; using fallback prediction.");
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "ML Service returned invalid JSON; using fallback prediction.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Unexpected ML Service failure; using fallback prediction.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var values = new List<double>(features.Count);
        foreach (var feature in features)
        {
            values.Add(await _fallback.PredictSuccessProbabilityAsync(feature, cancellationToken).ConfigureAwait(false));
        }

        return values;
    }
}
