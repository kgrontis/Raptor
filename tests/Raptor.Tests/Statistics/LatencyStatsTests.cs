using Raptor.Cli.Statistics;
using Xunit;

namespace Raptor.Tests.Statistics;

/// <summary>
/// Unit tests for the <see cref="LatencyStats"/> struct.
/// </summary>
public class LatencyStatsTests
{
    [Fact]
    public void Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var min = 10L;
        var max = 1000L;
        var avg = 500L;
        var p50 = 450L;
        var p95 = 950L;
        var p99 = 990L;

        // Act
        var stats = new LatencyStats(min, max, avg, p50, p95, p99);

        // Assert
        Assert.Equal(min, stats.Min);
        Assert.Equal(max, stats.Max);
        Assert.Equal(avg, stats.Avg);
        Assert.Equal(p50, stats.P50);
        Assert.Equal(p95, stats.P95);
        Assert.Equal(p99, stats.P99);
    }

    [Fact]
    public void Constructor_ShouldHandleZeroValues()
    {
        // Arrange & Act
        var stats = new LatencyStats(0, 0, 0, 0, 0, 0);

        // Assert
        Assert.Equal(0, stats.Min);
        Assert.Equal(0, stats.Max);
        Assert.Equal(0, stats.Avg);
        Assert.Equal(0, stats.P50);
        Assert.Equal(0, stats.P95);
        Assert.Equal(0, stats.P99);
    }

    [Fact]
    public void Constructor_ShouldHandleLargeValues()
    {
        // Arrange
        var min = 1L;
        var max = long.MaxValue;
        var avg = long.MaxValue / 2;
        var p50 = long.MaxValue / 4;
        var p95 = (long)((decimal)long.MaxValue * 9 / 10);
        var p99 = (long)((decimal)long.MaxValue * 99 / 100);

        // Act
        var stats = new LatencyStats(min, max, avg, p50, p95, p99);

        // Assert
        Assert.Equal(min, stats.Min);
        Assert.Equal(max, stats.Max);
        Assert.Equal(avg, stats.Avg);
        Assert.Equal(p50, stats.P50);
        Assert.Equal(p95, stats.P95);
        Assert.Equal(p99, stats.P99);
    }

    [Fact]
    public void Constructor_ShouldHandleEqualValues()
    {
        // Arrange
        var value = 150L;

        // Act
        var stats = new LatencyStats(value, value, value, value, value, value);

        // Assert
        Assert.Equal(value, stats.Min);
        Assert.Equal(value, stats.Max);
        Assert.Equal(value, stats.Avg);
        Assert.Equal(value, stats.P50);
        Assert.Equal(value, stats.P95);
        Assert.Equal(value, stats.P99);
    }

    [Fact]
    public void Default_ShouldHaveZeroValues()
    {
        // Act
        var stats = default(LatencyStats);

        // Assert
        Assert.Equal(0, stats.Min);
        Assert.Equal(0, stats.Max);
        Assert.Equal(0, stats.Avg);
        Assert.Equal(0, stats.P50);
        Assert.Equal(0, stats.P95);
        Assert.Equal(0, stats.P99);
    }

    [Fact]
    public void Properties_ShouldBeReadonly()
    {
        // Arrange
        var stats = new LatencyStats(10, 100, 50, 45, 95, 99);

        // Act & Assert - Properties should be readonly, so we can only read them
        var minRead = stats.Min;
        var maxRead = stats.Max;
        var avgRead = stats.Avg;
        var p50Read = stats.P50;
        var p95Read = stats.P95;
        var p99Read = stats.P99;

        Assert.Equal(10, minRead);
        Assert.Equal(100, maxRead);
        Assert.Equal(50, avgRead);
        Assert.Equal(45, p50Read);
        Assert.Equal(95, p95Read);
        Assert.Equal(99, p99Read);
    }

    [Fact]
    public void Constructor_ShouldHandleTypicalLatencyValues()
    {
        // Arrange
        var min = 50L;
        var max = 5000L;
        var avg = 250L;
        var p50 = 200L;
        var p95 = 3000L;
        var p99 = 4500L;

        // Act
        var stats = new LatencyStats(min, max, avg, p50, p95, p99);

        // Assert
        Assert.True(stats.Min < stats.P50);
        Assert.True(stats.P50 < stats.Avg || stats.P50 == stats.Avg);
        Assert.True(stats.Avg < stats.P95);
        Assert.True(stats.P95 < stats.P99);
        Assert.True(stats.P99 <= stats.Max);
    }
}

