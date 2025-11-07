using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Raptor.Cli.Core;
using Raptor.Cli.Statistics;
using Xunit;

namespace Raptor.Tests.Statistics;

/// <summary>
/// Unit tests for the <see cref="StatsCollector"/> class.
/// </summary>
public class StatsCollectorTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance_WithCapacity()
    {
        // Act
        var collector = new StatsCollector(100);

        // Assert
        Assert.NotNull(collector);
        Assert.Equal(0, collector.Count);
        Assert.Equal(0, collector.TotalRequests);
        Assert.Equal(0, collector.ErrorCount);
    }

    [Fact]
    public void Record_ShouldIncrementCount_WhenSingleRecord()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 200, false, 100);

        // Assert
        Assert.Equal(1, collector.Count);
        Assert.Equal(1, collector.TotalRequests);
        Assert.Equal(0, collector.ErrorCount);
    }

    [Fact]
    public void Record_ShouldIncrementErrorCount_WhenIsError()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 500, true, 200);

        // Assert
        Assert.Equal(1, collector.Count);
        Assert.Equal(1, collector.TotalRequests);
        Assert.Equal(1, collector.ErrorCount);
    }

    [Fact]
    public void Record_ShouldStoreMultipleRecords()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 200, false, 100);
        collector.Record(timestamp, 404, true, 150);
        collector.Record(timestamp, 500, true, 200);

        // Assert
        Assert.Equal(3, collector.Count);
        Assert.Equal(3, collector.TotalRequests);
        Assert.Equal(2, collector.ErrorCount);
    }

    [Fact]
    public void Record_ShouldHandleCapacityOverflow()
    {
        // Arrange
        var collector = new StatsCollector(3);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 200, false, 100);
        collector.Record(timestamp, 200, false, 110);
        collector.Record(timestamp, 200, false, 120);
        collector.Record(timestamp, 200, false, 130);
        collector.Record(timestamp, 200, false, 140);

        // Assert
        Assert.Equal(3, collector.Count);
        Assert.Equal(5, collector.TotalRequests);
    }

    [Fact]
    public void Record_ShouldTrackStatusCodes()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 200, false, 100);
        collector.Record(timestamp, 404, true, 150);
        collector.Record(timestamp, 500, true, 200);
        collector.Record(timestamp, 200, false, 110);

        // Assert
        var statusCodes = collector.GetStatusCodes();
        Assert.Equal(2, statusCodes[200]);
        Assert.Equal(1, statusCodes[404]);
        Assert.Equal(1, statusCodes[500]);
    }

    [Fact]
    public void Record_ShouldTrackErrorStatusCode()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 0, true, 50);

        // Assert
        var statusCodes = collector.GetStatusCodes();
        Assert.Equal(1, statusCodes[0]);
        Assert.Equal(1, collector.ErrorCount);
    }

    [Fact]
    public void Record_ShouldTrackValidStatusCodes()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 100, false, 100);
        collector.Record(timestamp, 200, false, 100);
        collector.Record(timestamp, 301, false, 100);
        collector.Record(timestamp, 404, true, 100);
        collector.Record(timestamp, 500, true, 100);
        collector.Record(timestamp, 599, true, 100);

        // Assert
        var statusCodes = collector.GetStatusCodes();
        Assert.Equal(1, statusCodes[100]);
        Assert.Equal(1, statusCodes[200]);
        Assert.Equal(1, statusCodes[301]);
        Assert.Equal(1, statusCodes[404]);
        Assert.Equal(1, statusCodes[500]);
        Assert.Equal(1, statusCodes[599]);
    }

    [Fact]
    public void GetLatencyStats_ShouldReturnDefault_WhenEmpty()
    {
        // Arrange
        var collector = new StatsCollector(10);

        // Act
        var stats = collector.GetLatencyStats();

        // Assert
        Assert.Equal(0, stats.Min);
        Assert.Equal(0, stats.Max);
        Assert.Equal(0, stats.Avg);
        Assert.Equal(0, stats.P50);
        Assert.Equal(0, stats.P95);
        Assert.Equal(0, stats.P99);
    }

    [Fact]
    public void GetLatencyStats_ShouldCalculateStats_ForSingleRecord()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();
        var duration = 150L;

        // Act
        collector.Record(timestamp, 200, false, duration);
        var stats = collector.GetLatencyStats();

        // Assert
        Assert.Equal(duration, stats.Min);
        Assert.Equal(duration, stats.Max);
        Assert.Equal(duration, stats.Avg);
        Assert.Equal(duration, stats.P50);
        Assert.Equal(duration, stats.P95);
        Assert.Equal(duration, stats.P99);
    }

    [Fact]
    public void GetLatencyStats_ShouldCalculateStats_ForMultipleRecords()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 200, false, 100);
        collector.Record(timestamp, 200, false, 200);
        collector.Record(timestamp, 200, false, 300);
        collector.Record(timestamp, 200, false, 400);
        collector.Record(timestamp, 200, false, 500);

        var stats = collector.GetLatencyStats();

        // Assert
        Assert.Equal(100, stats.Min);
        Assert.Equal(500, stats.Max);
        Assert.Equal(300, stats.Avg);
    }

    [Fact]
    public void GetLatencyStats_ShouldCalculatePercentiles()
    {
        // Arrange
        var collector = new StatsCollector(100);
        var timestamp = Stopwatch.GetTimestamp();

        // Act - Create 100 records with sequential latencies
        for (var i = 1; i <= 100; i++)
        {
            collector.Record(timestamp, 200, false, i);
        }

        var stats = collector.GetLatencyStats();

        // Assert
        Assert.Equal(1, stats.Min);
        Assert.Equal(100, stats.Max);
        Assert.Equal(50, stats.Avg);
        Assert.True(stats.P50 >= 49 && stats.P50 <= 51);
        Assert.True(stats.P95 >= 94 && stats.P95 <= 96);
        Assert.True(stats.P99 >= 98 && stats.P99 <= 100);
    }

    [Fact]
    public void GetStatusCodes_ShouldReturnEmpty_WhenNoRecords()
    {
        // Arrange
        var collector = new StatsCollector(10);

        // Act
        var statusCodes = collector.GetStatusCodes();

        // Assert
        Assert.Empty(statusCodes);
    }

    [Fact]
    public void GetStatusCodes_ShouldReturnStatusCodes_WithCounts()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 200, false, 100);
        collector.Record(timestamp, 200, false, 110);
        collector.Record(timestamp, 404, true, 150);
        collector.Record(timestamp, 500, true, 200);

        var statusCodes = collector.GetStatusCodes();

        // Assert
        Assert.Equal(3, statusCodes.Count);
        Assert.Equal(2, statusCodes[200]);
        Assert.Equal(1, statusCodes[404]);
        Assert.Equal(1, statusCodes[500]);
    }

    [Fact]
    public void GetStatusCodes_ShouldOnlyIncludeNonZeroCounts()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 200, false, 100);
        collector.Record(timestamp, 500, true, 200);

        var statusCodes = collector.GetStatusCodes();

        // Assert
        Assert.Equal(2, statusCodes.Count);
        Assert.DoesNotContain(404, statusCodes.Keys);
    }

    [Fact]
    public void GetResults_ShouldReturnEmpty_WhenNoRecords()
    {
        // Arrange
        var collector = new StatsCollector(10);

        // Act
        var results = collector.GetResults();

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void GetResults_ShouldReturnRecordedResults()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp1 = Stopwatch.GetTimestamp();
        var timestamp2 = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp1, 200, false, 100);
        collector.Record(timestamp2, 404, true, 150);

        var results = collector.GetResults();

        // Assert
        Assert.Equal(2, results.Length);
        Assert.Equal(200, results[0].StatusCode);
        Assert.False(results[0].IsError);
        Assert.Equal(100, results[0].DurationMs);
        Assert.Equal(404, results[1].StatusCode);
        Assert.True(results[1].IsError);
        Assert.Equal(150, results[1].DurationMs);
    }

    [Fact]
    public void GetResults_ShouldRespectCapacity()
    {
        // Arrange
        var collector = new StatsCollector(3);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 200, false, 100);
        collector.Record(timestamp, 200, false, 110);
        collector.Record(timestamp, 200, false, 120);
        collector.Record(timestamp, 200, false, 130);

        var results = collector.GetResults();

        // Assert
        Assert.Equal(3, results.Length);
        Assert.Equal(4, collector.TotalRequests);
    }

    [Fact]
    public void Count_ShouldBeLimitedByCapacity()
    {
        // Arrange
        var collector = new StatsCollector(5);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        for (var i = 0; i < 10; i++)
        {
            collector.Record(timestamp, 200, false, 100);
        }

        // Assert
        Assert.Equal(5, collector.Count);
        Assert.Equal(10, collector.TotalRequests);
    }

    [Fact]
    public void TotalRequests_ShouldCountAllRecords()
    {
        // Arrange
        var collector = new StatsCollector(3);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        for (var i = 0; i < 10; i++)
        {
            collector.Record(timestamp, 200, false, 100);
        }

        // Assert
        Assert.Equal(10, collector.TotalRequests);
    }

    [Fact]
    public void ErrorCount_ShouldCountOnlyErrors()
    {
        // Arrange
        var collector = new StatsCollector(10);
        var timestamp = Stopwatch.GetTimestamp();

        // Act
        collector.Record(timestamp, 200, false, 100);
        collector.Record(timestamp, 301, false, 110);
        collector.Record(timestamp, 404, true, 150);
        collector.Record(timestamp, 500, true, 200);
        collector.Record(timestamp, 200, false, 120);

        // Assert
        Assert.Equal(2, collector.ErrorCount);
    }

    [Fact]
    public async Task Record_ShouldBeThreadSafe()
    {
        // Arrange
        var collector = new StatsCollector(1000);
        var timestamp = Stopwatch.GetTimestamp();
        var tasks = new List<Task>();

        // Act - Record from multiple threads
        for (var i = 0; i < 10; i++)
        {
            var threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    var statusCode = threadId % 2 == 0 ? 200 : 500;
                    var isError = statusCode >= 400;
                    collector.Record(timestamp, statusCode, isError, 100 + j);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(1000, collector.TotalRequests);
        var statusCodes = collector.GetStatusCodes();
        Assert.True(statusCodes.ContainsKey(200) || statusCodes.ContainsKey(500));
    }

    [Fact]
    public async Task GetStatusCodes_ShouldBeThreadSafe()
    {
        // Arrange
        var collector = new StatsCollector(100);
        var timestamp = Stopwatch.GetTimestamp();
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Act - Record and read concurrently
        var recordTask = Task.Run(() =>
        {
            var counter = 0;
            while (!cancellationToken.IsCancellationRequested && counter < 1000)
            {
                collector.Record(timestamp, 200, false, 100);
                counter++;
            }
        }, cancellationToken);

        var readTask = Task.Run(() =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var statusCodes = collector.GetStatusCodes();
                Assert.NotNull(statusCodes);
            }
        }, cancellationToken);

        await Task.Delay(100);
        cancellationTokenSource.Cancel();

        await Task.WhenAll(new[] { recordTask, readTask }).WaitAsync(TimeSpan.FromSeconds(5));

        // Assert - Should complete without exceptions
        Assert.True(collector.TotalRequests > 0);
    }
}

