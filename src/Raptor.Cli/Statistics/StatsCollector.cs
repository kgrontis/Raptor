using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading;
using Raptor.Cli.Core;

namespace Raptor.Cli.Statistics;

/// <summary>
/// Thread-safe collector for HTTP request statistics and results.
/// Maintains pre-allocated arrays for latency measurements and computes percentiles using Array.Sort.
/// Uses lock-free operations with Interlocked for thread-safe updates.
/// </summary>
internal sealed class StatsCollector(int capacity)
{
    private readonly RequestResult[] _results = new RequestResult[capacity];
    private readonly long[] _latencies = new long[capacity];
    private readonly int[] _statusCodeCounts = new int[600];
    private int _count = 0;
    private long _totalRequests = 0;
    private long _errorCount = 0;

    /// <summary>
    /// Records a single request result, updating all statistics atomically using lock-free operations.
    /// </summary>
    /// <param name="timestampNs">The timestamp in nanoseconds when the request completed.</param>
    /// <param name="statusCode">The HTTP status code (0 if request failed).</param>
    /// <param name="isError">Whether the request resulted in an error.</param>
    /// <param name="durationMs">The duration of the request in milliseconds.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Record(long timestampNs, int statusCode, bool isError, long durationMs)
    {
        var index = Interlocked.Increment(ref _count) - 1;
        if (index >= 0 && index < _results.Length)
        {
            _results[index] = new(timestampNs, statusCode, isError, durationMs);
            _latencies[index] = durationMs;
        }

        Interlocked.Increment(ref _totalRequests);
        if (isError)
        {
            Interlocked.Increment(ref _errorCount);
        }

        if (statusCode >= 0 && statusCode < _statusCodeCounts.Length)
        {
            Interlocked.Increment(ref _statusCodeCounts[statusCode]);
        }
    }

    /// <summary>
    /// Gets the number of results currently stored (limited by capacity).
    /// </summary>
    public int Count => Math.Min(Volatile.Read(ref _count), _results.Length);

    /// <summary>
    /// Gets the total number of requests recorded (including those beyond capacity).
    /// </summary>
    public long TotalRequests => _totalRequests;

    /// <summary>
    /// Gets the total number of requests that resulted in errors.
    /// </summary>
    public long ErrorCount => _errorCount;

    /// <summary>
    /// Gets latency statistics computed from the collected latency measurements.
    /// Uses Array.Sort for efficient percentile calculation.
    /// </summary>
    /// <returns>A struct containing min, max, avg, p50, p95, and p99 latency values.</returns>
    public LatencyStats GetLatencyStats()
    {
        var count = Count;
        if (count == 0)
        {
            return default;
        }

        var latencies = ArrayPool<long>.Shared.Rent(count);
        try
        {
            Array.Copy(_latencies, 0, latencies, 0, count);

            Array.Sort(latencies, 0, count);

            var min = latencies[0];
            var max = latencies[count - 1];

            long sum = 0;
            for (var i = 0; i < count; i++)
            {
                sum += latencies[i];
            }
            var avg = sum / count;

            var p50 = Percentile(latencies, count, 50);
            var p95 = Percentile(latencies, count, 95);
            var p99 = Percentile(latencies, count, 99);

            return new LatencyStats(min, max, avg, p50, p95, p99);
        }
        finally
        {
            ArrayPool<long>.Shared.Return(latencies);
        }
    }

    /// <summary>
    /// Gets a dictionary mapping HTTP status codes to their occurrence counts.
    /// Uses manual loop to avoid LINQ allocations.
    /// </summary>
    /// <returns>A dictionary where keys are status codes and values are occurrence counts.</returns>
    public Dictionary<int, int> GetStatusCodes()
    {
        Thread.MemoryBarrier();

        var result = new Dictionary<int, int>(16);
        for (var i = 0; i < _statusCodeCounts.Length; i++)
        {
            var count = Volatile.Read(ref _statusCodeCounts[i]);
            if (count > 0)
            {
                result[i] = count;
            }
        }
        return result;
    }

    /// <summary>
    /// Gets a copy of all stored request results as an array.
    /// </summary>
    /// <returns>An array containing all recorded request results.</returns>
    public RequestResult[] GetResults()
    {
        var count = Count;
        var results = new RequestResult[count];
        Array.Copy(_results, 0, results, 0, count);
        return results;
    }

    /// <summary>
    /// Calculates a percentile value from a sorted array using manual index calculation.
    /// </summary>
    /// <param name="sortedArray">The sorted array.</param>
    /// <param name="count">Number of elements in the array.</param>
    /// <param name="percentile">Percentile value (0-100).</param>
    /// <returns>The value at the specified percentile.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Percentile(long[] sortedArray, int count, int percentile)
    {
        var index = (int)(percentile / 100.0 * (count - 1));
        return sortedArray[index];
    }
}

