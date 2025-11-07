using System.Diagnostics;
using System.IO;
using Raptor.Cli.Statistics;
using Xunit;

namespace Raptor.Tests.Statistics;

/// <summary>
/// Unit tests for the <see cref="ResultsReporter"/> class.
/// Uses a collection to ensure tests don't run in parallel and interfere with console redirection.
/// </summary>
[Collection("ConsoleOutputTests")]
public class ResultsReporterTests : IDisposable
{
    private static readonly object _lock = new object();
    private StringWriter _stringWriter = null!;
    private readonly TextWriter _originalOut;

    public ResultsReporterTests()
    {
        lock (_lock)
        {
            _originalOut = System.Console.Out;
            ResetStringWriter();
        }
    }

    private void ResetStringWriter()
    {
        lock (_lock)
        {
            if (_stringWriter != null)
            {
                var sb = _stringWriter.GetStringBuilder();
                sb.Clear();
            }
            else
            {
                _stringWriter = new StringWriter();
            }
            
            System.Console.SetOut(_stringWriter);
        }
    }

    private StringWriter CreateTestWriter()
    {
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            var testWriter = new StringWriter();
            System.Console.SetOut(testWriter);
            return testWriter;
        }
    }

    private void RestoreConsole(StringWriter testWriter)
    {
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            testWriter?.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            _stringWriter?.Dispose();
        }
    }

    [Fact]
    public void Report_ShouldWriteResults_WhenEmptyStats()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var duration = 0.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("=== Load Test Results ===", output);
            Assert.Contains("Total Requests:", output);
            Assert.Contains("0", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldWriteSummary_WithCorrectValues()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var timestamp = Stopwatch.GetTimestamp();
            collector.Record(timestamp, 200, false, 100);
            collector.Record(timestamp, 200, false, 150);
            collector.Record(timestamp, 404, true, 200);
            var duration = 1.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.NotNull(output);
            Assert.NotEmpty(output);
            Assert.Contains("Total Requests:", output);
            Assert.Contains("3", output);
            Assert.Contains("Successful:", output);
            Assert.Contains("2", output);
            Assert.Contains("Errors:", output);
            Assert.Contains("1", output);
            Assert.Contains("Duration:", output);
            Assert.Contains("1.00s", output);
            Assert.Contains("Requests/sec:", output);
            Assert.Contains("3.00", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldCalculateRPS_WhenDurationProvided()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var timestamp = Stopwatch.GetTimestamp();
            for (var i = 0; i < 100; i++)
            {
                collector.Record(timestamp, 200, false, 100);
            }
            var duration = 2.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("Requests/sec:", output);
            Assert.Contains("50.00", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldHandleZeroDuration()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var timestamp = Stopwatch.GetTimestamp();
            collector.Record(timestamp, 200, false, 100);
            var duration = 0.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("Requests/sec:", output);
            Assert.Contains("0.00", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldWriteLatencyStats()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var timestamp = Stopwatch.GetTimestamp();
            collector.Record(timestamp, 200, false, 100);
            collector.Record(timestamp, 200, false, 200);
            collector.Record(timestamp, 200, false, 300);
            var duration = 1.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("Latency (ms):", output);
            Assert.Contains("Min:", output);
            Assert.Contains("Max:", output);
            Assert.Contains("Avg:", output);
            Assert.Contains("P50:", output);
            Assert.Contains("P95:", output);
            Assert.Contains("P99:", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldWriteStatusCodes()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var timestamp = Stopwatch.GetTimestamp();
            collector.Record(timestamp, 200, false, 100);
            collector.Record(timestamp, 200, false, 110);
            collector.Record(timestamp, 404, true, 150);
            collector.Record(timestamp, 500, true, 200);
            var duration = 1.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("Status Codes:", output);
            Assert.Contains("200:", output);
            Assert.Contains("404:", output);
            Assert.Contains("500:", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldSortStatusCodes()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var timestamp = Stopwatch.GetTimestamp();
            collector.Record(timestamp, 500, true, 200);
            collector.Record(timestamp, 200, false, 100);
            collector.Record(timestamp, 404, true, 150);
            collector.Record(timestamp, 301, false, 110);
            var duration = 1.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            var statusCodeIndex = output.IndexOf("Status Codes:");
            Assert.True(statusCodeIndex >= 0);

            var statusCodeSection = output.Substring(statusCodeIndex);
            var index200 = statusCodeSection.IndexOf("200:");
            var index301 = statusCodeSection.IndexOf("301:");
            var index404 = statusCodeSection.IndexOf("404:");
            var index500 = statusCodeSection.IndexOf("500:");

            Assert.True(index200 < index301);
            Assert.True(index301 < index404);
            Assert.True(index404 < index500);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldCalculateStatusCodePercentages()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var timestamp = Stopwatch.GetTimestamp();
            collector.Record(timestamp, 200, false, 100);
            collector.Record(timestamp, 200, false, 110);
            collector.Record(timestamp, 404, true, 150);
            var duration = 1.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("200:", output);
            Assert.Contains("66.7", output);
            Assert.Contains("404:", output);
            Assert.Contains("33.3", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldNotWriteStatusCodes_WhenEmpty()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var duration = 1.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.DoesNotContain("Status Codes:", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldFormatLatencyValues()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var timestamp = Stopwatch.GetTimestamp();
            collector.Record(timestamp, 200, false, 1234);
            collector.Record(timestamp, 200, false, 5678);
            var duration = 1.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("1234", output);
            Assert.Contains("5678", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldFormatAverageLatency_AsDecimal()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(10);
            var timestamp = Stopwatch.GetTimestamp();
            collector.Record(timestamp, 200, false, 100);
            collector.Record(timestamp, 200, false, 200);
            var duration = 1.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("Avg:", output);
            var avgIndex = output.IndexOf("Avg:");
            var avgLine = output.Substring(avgIndex, 50);
            Assert.Contains("150", avgLine);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldHandleLargeRequestCounts()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(1000);
            var timestamp = Stopwatch.GetTimestamp();
            for (var i = 0; i < 1000; i++)
            {
                collector.Record(timestamp, 200, false, 100);
            }
            var duration = 10.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("1,000", output);
            Assert.Contains("100.00", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Report_ShouldHandleMixedStatusCodes()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var collector = new StatsCollector(20);
            var timestamp = Stopwatch.GetTimestamp();
            for (var i = 0; i < 5; i++)
            {
                collector.Record(timestamp, 200, false, 100);
            }
            for (var i = 0; i < 3; i++)
            {
                collector.Record(timestamp, 404, true, 150);
            }
            for (var i = 0; i < 2; i++)
            {
                collector.Record(timestamp, 500, true, 200);
            }
            var duration = 1.0;

            ResultsReporter.Report(collector, duration);

            var output = testWriter.ToString();
            Assert.Contains("Total Requests:", output);
            Assert.Contains("10", output);
            Assert.Contains("Successful:", output);
            Assert.Contains("5", output);
            Assert.Contains("Errors:", output);
            Assert.Contains("5", output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }
}

