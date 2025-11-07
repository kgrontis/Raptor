using System.Runtime.CompilerServices;
using System.Text;

namespace Raptor.Cli.Statistics;

/// <summary>
/// Formats and displays load test results with minimal allocations using StringBuilder.
/// Provides formatted output for throughput, latency statistics, status code distribution, and error counts.
/// </summary>
internal static class ResultsReporter
{
    /// <summary>
    /// Formats and writes the complete load test results to the console.
    /// </summary>
    /// <param name="stats">The statistics collector containing all request results.</param>
    /// <param name="durationSeconds">The total duration of the test in seconds.</param>
    public static void Report(StatsCollector stats, double durationSeconds)
    {
        var latencyStats = stats.GetLatencyStats();
        var statusCodes = stats.GetStatusCodes();
        var totalRequests = stats.TotalRequests;
        var errorCount = stats.ErrorCount;
        var successCount = totalRequests - errorCount;
        var rps = durationSeconds > 0 ? totalRequests / durationSeconds : 0;

        var sb = new StringBuilder(1024);

        sb.AppendLine();
        sb.AppendLine("=== Load Test Results ===");
        sb.AppendLine();

        sb.AppendLine("Summary:");
        AppendFormatted(sb, "  Total Requests:", totalRequests.ToString("N0"));
        AppendFormatted(sb, "  Successful:", successCount.ToString("N0"));
        AppendFormatted(sb, "  Errors:", errorCount.ToString("N0"));
        AppendFormatted(sb, "  Duration:", $"{durationSeconds:F2}s");
        AppendFormatted(sb, "  Requests/sec:", rps.ToString("F2"));
        sb.AppendLine();

        sb.AppendLine("Latency (ms):");
        AppendFormatted(sb, "  Min:", latencyStats.Min.ToString("F0"));
        AppendFormatted(sb, "  Max:", latencyStats.Max.ToString("F0"));
        AppendFormatted(sb, "  Avg:", latencyStats.Avg.ToString("F0"));
        AppendFormatted(sb, "  P50:", latencyStats.P50.ToString("F0"));
        AppendFormatted(sb, "  P95:", latencyStats.P95.ToString("F0"));
        AppendFormatted(sb, "  P99:", latencyStats.P99.ToString("F0"));
        sb.AppendLine();

        if (statusCodes.Count > 0)
        {
            sb.AppendLine("Status Codes:");
            var statusCodeList = new List<(int code, int count)>(statusCodes.Count);
            foreach (var kvp in statusCodes)
            {
                statusCodeList.Add((kvp.Key, kvp.Value));
            }

            SortStatusCodes(statusCodeList);

            for (var i = 0; i < statusCodeList.Count; i++)
            {
                var (code, count) = statusCodeList[i];
                var percentage = totalRequests > 0 ? (count * 100.0 / totalRequests) : 0;
                sb.Append($"    {code}: {count:N0} ({percentage:F1}%)");
                if (i < statusCodeList.Count - 1)
                {
                    sb.AppendLine();
                }
            }
        }

        System.Console.Write(sb.ToString());
    }

    /// <summary>
    /// Appends a formatted line to the StringBuilder with consistent padding.
    /// </summary>
    /// <param name="sb">The StringBuilder to append to.</param>
    /// <param name="label">The label text.</param>
    /// <param name="value">The value text.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AppendFormatted(StringBuilder sb, string label, string value)
    {
        sb.Append(label);
        sb.Append(' ', Math.Max(1, 20 - label.Length));
        sb.AppendLine(value);
    }

    /// <summary>
    /// Sorts status codes by code value using manual insertion sort (avoid LINQ).
    /// </summary>
    /// <param name="list">The list of status codes to sort.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SortStatusCodes(List<(int code, int count)> list)
    {
        for (var i = 1; i < list.Count; i++)
        {
            var current = list[i];
            var j = i - 1;

            while (j >= 0 && list[j].code > current.code)
            {
                list[j + 1] = list[j];
                j--;
            }

            list[j + 1] = current;
        }
    }
}

