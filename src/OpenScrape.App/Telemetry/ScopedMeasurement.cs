using System.Diagnostics;

namespace OpenScrape.App.Telemetry;

/// <summary>
/// Medición ambiente zero-allocation. Se devuelve desde
/// <see cref="IMetricsCollector.Measure(string)"/> y al liberarse en el
/// <c>using</c> registra el tiempo transcurrido en la categoría correspondiente.
/// </summary>
public readonly struct ScopedMeasurement : IDisposable
{
    private readonly IMetricsCollector? _collector;
    private readonly string? _category;
    private readonly long _startTicks;

    internal ScopedMeasurement(IMetricsCollector collector, string category)
    {
        _collector = collector;
        _category = category;
        _startTicks = Stopwatch.GetTimestamp();
    }

    public void Dispose()
    {
        if (_collector is null || _category is null) return;

        long endTicks = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(_startTicks, endTicks);
        _collector.Record(_category, elapsed);
    }
}
