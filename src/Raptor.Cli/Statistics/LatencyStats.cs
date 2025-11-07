namespace Raptor.Cli.Statistics;

/// <summary>
/// Represents latency statistics including min, max, average, and percentiles.
/// </summary>
internal readonly struct LatencyStats(long min, long max, long avg, long p50, long p95, long p99)
{
    /// <summary>
    /// Gets the minimum latency in milliseconds.
    /// </summary>
    public readonly long Min = min;

    /// <summary>
    /// Gets the maximum latency in milliseconds.
    /// </summary>
    public readonly long Max = max;

    /// <summary>
    /// Gets the average latency in milliseconds.
    /// </summary>
    public readonly long Avg = avg;

    /// <summary>
    /// Gets the 50th percentile (median) latency in milliseconds.
    /// </summary>
    public readonly long P50 = p50;

    /// <summary>
    /// Gets the 95th percentile latency in milliseconds.
    /// </summary>
    public readonly long P95 = p95;

    /// <summary>
    /// Gets the 99th percentile latency in milliseconds.
    /// </summary>
    public readonly long P99 = p99;
}

