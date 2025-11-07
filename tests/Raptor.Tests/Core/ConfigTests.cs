using System.Net.Http;
using Raptor.Cli.Core;
using Xunit;

namespace Raptor.Tests.Core;

/// <summary>
/// Unit tests for the <see cref="Config"/> struct.
/// </summary>
public class ConfigTests
{
    [Fact]
    public void IsValid_ShouldReturnTrue_WhenValidConfigWithDuration()
    {
        // Arrange
        var config = new Config
        {
            Url = "https://api.example.com",
            Concurrency = 5,
            DurationSeconds = 10
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenValidConfigWithRequestCount()
    {
        // Arrange
        var config = new Config
        {
            Url = "https://api.example.com",
            Concurrency = 5,
            RequestCount = 100
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenUrlIsNull()
    {
        // Arrange
        var config = new Config
        {
            Url = null!,
            Concurrency = 5,
            DurationSeconds = 10
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenUrlIsEmpty()
    {
        // Arrange
        var config = new Config
        {
            Url = string.Empty,
            Concurrency = 5,
            DurationSeconds = 10
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenConcurrencyIsZero()
    {
        // Arrange
        var config = new Config
        {
            Url = "https://api.example.com",
            Concurrency = 0,
            DurationSeconds = 10
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenBothDurationAndRequestCount()
    {
        // Arrange
        var config = new Config
        {
            Url = "https://api.example.com",
            Concurrency = 5,
            DurationSeconds = 10,
            RequestCount = 100
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenNeitherDurationNorRequestCount()
    {
        // Arrange
        var config = new Config
        {
            Url = "https://api.example.com",
            Concurrency = 5
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnFalse_WhenMissingUrlAndConcurrency()
    {
        // Arrange
        var config = new Config
        {
            Url = null!,
            Concurrency = 0,
            DurationSeconds = 10
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WithMinimumValidValues()
    {
        // Arrange
        var config = new Config
        {
            Url = "https://api.example.com",
            Concurrency = 1,
            DurationSeconds = 1
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenUrlIsWhitespace()
    {
        // Arrange
        var config = new Config
        {
            Url = "   ",
            Concurrency = 5,
            DurationSeconds = 10
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        // Note: string.IsNullOrEmpty returns false for whitespace-only strings
        // so whitespace URLs are considered valid by the current implementation
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenRequestCountIsMinimum()
    {
        // Arrange
        var config = new Config
        {
            Url = "https://api.example.com",
            Concurrency = 1,
            RequestCount = 1
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenConcurrencyIsMaximum()
    {
        // Arrange
        var config = new Config
        {
            Url = "https://api.example.com",
            Concurrency = ushort.MaxValue,
            DurationSeconds = 10
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ShouldReturnTrue_WhenOptionalFieldsAreSet()
    {
        // Arrange
        var config = new Config
        {
            Url = "https://api.example.com",
            Method = HttpMethod.Post,
            Concurrency = 5,
            DurationSeconds = 10,
            Body = "{\"test\":\"data\"}",
            Headers = new Dictionary<string, string> { { "Authorization", "Bearer token" } }
        };

        // Act
        var isValid = config.IsValid;

        // Assert
        Assert.True(isValid);
    }
}

