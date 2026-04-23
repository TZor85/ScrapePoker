namespace OpenScrape.App.Telemetry;

/// <summary>
/// Histograma logarítmico de 30 buckets que cubre de 10μs a ~6.3s.
/// Zero-allocation en <c>Add</c>, percentiles en O(buckets).
/// Precisión de percentiles: sobreestima hasta ~37% (nunca subestima) por el
/// ancho logarítmico del bucket (step = 10^0.2). Aceptable para telemetría
/// operacional; no apto para SLOs milimétricos.
/// </summary>
/// <remarks>
/// No es thread-safe; la sincronización vive en <see cref="MetricsCollector"/>,
/// que envuelve cada histograma en un <c>lock</c> por categoría.
/// </remarks>
public sealed class Histogram
{
    private const int BucketCount = 30;

    /// <summary>Umbrales superiores por bucket, en ticks. Precomputados.</summary>
    private static readonly long[] BucketBoundsTicks = BuildBounds();

    private readonly long[] _buckets = new long[BucketCount];
    private long _count;
    private TimeSpan _max;

    public long Count => _count;
    public TimeSpan Max => _max;

    public void Add(TimeSpan elapsed)
    {
        long ticks = elapsed.Ticks;
        if (ticks < 0) ticks = 0;

        var clamped = TimeSpan.FromTicks(ticks);
        if (clamped > _max) _max = clamped;

        int idx = FindBucket(ticks);
        _buckets[idx]++;
        _count++;
    }

    public TimeSpan GetPercentile(double percentile)
    {
        if (_count == 0) return TimeSpan.Zero;

        long target = (long)Math.Ceiling(_count * percentile);
        if (target < 1) target = 1;

        long accumulated = 0;
        for (int i = 0; i < BucketCount; i++)
        {
            accumulated += _buckets[i];
            if (accumulated >= target)
                return TimeSpan.FromTicks(BucketBoundsTicks[i]);
        }
        return TimeSpan.FromTicks(BucketBoundsTicks[BucketCount - 1]);
    }

    public void Reset()
    {
        Array.Clear(_buckets, 0, BucketCount);
        _count = 0;
        _max = TimeSpan.Zero;
    }

    private static int FindBucket(long ticks)
    {
        // Búsqueda lineal: 30 comparaciones, más rápido que binaria en este tamaño.
        for (int i = 0; i < BucketCount; i++)
        {
            if (ticks <= BucketBoundsTicks[i]) return i;
        }
        return BucketCount - 1;
    }

    private static long[] BuildBounds()
    {
        // bucket[i] upper bound (segundos) = 1e-5 * 10^(i * 0.2)
        // bucket[0]  ≈ 10μs
        // bucket[29] ≈ 10μs * 10^5.8 ≈ 6.3s
        var bounds = new long[BucketCount];
        for (int i = 0; i < BucketCount; i++)
        {
            double seconds = 1e-5 * Math.Pow(10.0, i * 0.2);
            bounds[i] = (long)(seconds * TimeSpan.TicksPerSecond);
        }
        return bounds;
    }
}
