using System.Diagnostics;
using Raptor.Cli.Core;
using Xunit;

namespace Raptor.Tests.Core;

/// <summary>
/// Unit tests for the <see cref="RequestResult"/> struct.
/// </summary>
public class RequestResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var timestamp = Stopwatch.GetTimestamp();
        var statusCode = 200;
        var isError = false;
        var durationMs = 150L;

        // Act
        var result = new RequestResult(timestamp, statusCode, isError, durationMs);

        // Assert
        Assert.Equal(timestamp, result.TimestampNs);
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(isError, result.IsError);
        Assert.Equal(durationMs, result.DurationMs);
    }

    [Fact]
    public void Constructor_ShouldHandleErrorStatusCode()
    {
        // Arrange
        var timestamp = Stopwatch.GetTimestamp();
        var statusCode = 500;
        var isError = true;
        var durationMs = 200L;

        // Act
        var result = new RequestResult(timestamp, statusCode, isError, durationMs);

        // Assert
        Assert.Equal(timestamp, result.TimestampNs);
        Assert.Equal(statusCode, result.StatusCode);
        Assert.True(result.IsError);
        Assert.Equal(durationMs, result.DurationMs);
    }

    [Fact]
    public void Constructor_ShouldHandleZeroStatusCode()
    {
        // Arrange
        var timestamp = Stopwatch.GetTimestamp();
        var statusCode = 0;
        var isError = true;
        var durationMs = 50L;

        // Act
        var result = new RequestResult(timestamp, statusCode, isError, durationMs);

        // Assert
        Assert.Equal(0, result.StatusCode);
        Assert.True(result.IsError);
    }

    [Fact]
    public void Constructor_ShouldHandleVariousStatusCodes()
    {
        // Arrange
        var timestamp = Stopwatch.GetTimestamp();
        var statusCodes = new[] { 100, 200, 301, 404, 500, 599 };

        // Act & Assert
        foreach (var code in statusCodes)
        {
            var isError = code >= 400;
            var result = new RequestResult(timestamp, code, isError, 100);
            Assert.Equal(code, result.StatusCode);
            Assert.Equal(isError, result.IsError);
        }
    }

    [Fact]
    public void Constructor_ShouldHandleVariousDurations()
    {
        // Arrange
        var timestamp = Stopwatch.GetTimestamp();
        var durations = new[] { 0L, 1L, 100L, 1000L, 10000L, long.MaxValue };

        // Act & Assert
        foreach (var duration in durations)
        {
            var result = new RequestResult(timestamp, 200, false, duration);
            Assert.Equal(duration, result.DurationMs);
        }
    }

    [Fact]
    public void Constructor_ShouldHandleVariousTimestamps()
    {
        // Arrange
        var timestamps = new[] { 0L, Stopwatch.GetTimestamp(), long.MaxValue };

        // Act & Assert
        foreach (var timestamp in timestamps)
        {
            var result = new RequestResult(timestamp, 200, false, 100);
            Assert.Equal(timestamp, result.TimestampNs);
        }
    }

    [Fact]
    public void Properties_ShouldBeReadonly()
    {
        // Arrange
        var timestamp = Stopwatch.GetTimestamp();
        var result = new RequestResult(timestamp, 200, false, 100);

        // Act & Assert - Properties should be readonly, so we can only read them
        var timestampRead = result.TimestampNs;
        var statusCodeRead = result.StatusCode;
        var isErrorRead = result.IsError;
        var durationRead = result.DurationMs;

        Assert.Equal(timestamp, timestampRead);
        Assert.Equal(200, statusCodeRead);
        Assert.False(isErrorRead);
        Assert.Equal(100, durationRead);
    }

    [Fact]
    public void Default_ShouldHaveZeroValues()
    {
        // Act
        var result = default(RequestResult);

        // Assert
        Assert.Equal(0, result.TimestampNs);
        Assert.Equal(0, result.StatusCode);
        Assert.False(result.IsError);
        Assert.Equal(0, result.DurationMs);
    }
}

