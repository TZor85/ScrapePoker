using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using OpenScrape.Domain.ValueObjects;

namespace OpenScrape.App.Telemetry;

/// <summary>
/// Implementación thread-safe del recolector de métricas. Cada categoría tiene dos
/// histogramas (última mano y sesión) bajo un lock propio; no hay contención global.
/// </summary>
public sealed class MetricsCollector : IMetricsCollector
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly ConcurrentDictionary<string, CategoryState> _categories = new();

    // Acceso protegido por _handLock
    private readonly object _handLock = new();
    private string? _currentHandId;

    public MetricsCollector(ILogger<MetricsCollector> logger)
    {
        _logger = logger;
    }

    public ScopedMeasurement Measure(string category) => new(this, category);

    public ScopedMeasurement MeasureSessionOnly(string category) => new(this, category, sessionOnly: true);

    public void Record(string category, TimeSpan elapsed)
    {
        var state = _categories.GetOrAdd(category, _ => new CategoryState());
        lock (state.Lock)
        {
            state.LastHand.Add(elapsed);
            state.Session.Add(elapsed);
        }
    }

    public void RecordSessionOnly(string category, TimeSpan elapsed)
    {
        var state = _categories.GetOrAdd(category, _ => new CategoryState());
        lock (state.Lock)
        {
            state.Session.Add(elapsed);
        }
    }

    public void StartHand(string handId)
    {
        lock (_handLock)
        {
            if (_currentHandId is not null)
            {
                bool hadSamples = false;
                foreach (var state in _categories.Values)
                {
                    lock (state.Lock)
                    {
                        if (state.LastHand.Count > 0) hadSamples = true;
                        state.LastHand.Reset();
                    }
                }
                if (hadSamples)
                {
                    _logger.LogWarning(
                        "Telemetria: StartHand({HandId}) llamado con agregado de mano anterior pendiente, se descarta",
                        handId);
                }
            }
            _currentHandId = handId;
        }
    }

    public TelemetryAggregate? EndHand()
    {
        lock (_handLock)
        {
            if (_currentHandId is null) return null;

            var phases = new Dictionary<string, CategoryStats>();
            foreach (var (category, state) in _categories)
            {
                if (TelemetryCategories.SessionOnly.Contains(category)) continue;

                lock (state.Lock)
                {
                    if (state.LastHand.Count == 0) continue;
                    phases[category] = ToStats(state.LastHand);
                    state.LastHand.Reset();
                }
            }

            var agg = new TelemetryAggregate(
                _currentHandId,
                DateTime.UtcNow,
                phases);

            _currentHandId = null;
            return agg;
        }
    }

    public MetricsSnapshot SnapshotSession()
    {
        var lastHand = new Dictionary<string, CategoryStats>();
        var session = new Dictionary<string, CategoryStats>();
        string? handId;

        lock (_handLock) { handId = _currentHandId; }

        foreach (var (category, state) in _categories)
        {
            lock (state.Lock)
            {
                if (state.LastHand.Count > 0)
                    lastHand[category] = ToStats(state.LastHand);
                if (state.Session.Count > 0)
                    session[category] = ToStats(state.Session);
            }
        }

        return new MetricsSnapshot(handId, lastHand, session);
    }

    public void ResetSession()
    {
        foreach (var state in _categories.Values)
        {
            lock (state.Lock)
            {
                state.LastHand.Reset();
                state.Session.Reset();
            }
        }
    }

    private static CategoryStats ToStats(Histogram h) => new(
        P50Ms: h.GetPercentile(0.5).TotalMilliseconds,
        P95Ms: h.GetPercentile(0.95).TotalMilliseconds,
        MaxMs: h.Max.TotalMilliseconds,
        Count: h.Count);

    private sealed class CategoryState
    {
        public readonly object Lock = new();
        public readonly Histogram LastHand = new();
        public readonly Histogram Session = new();
    }
}
