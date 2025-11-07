using System.Linq;
using System.Reflection;
using Xunit;

namespace Raptor.Tests;

/// <summary>
/// Unit tests for the Program class, specifically the IsHelpRequested method.
/// Note: Since IsHelpRequested is a local function in a top-level program,
/// we use reflection to access it. The method is compiled as a static method
/// in the generated Program class.
/// </summary>
public class ProgramTests
{
    private static bool IsHelpRequested(string[] args)
    {
        var assembly = Assembly.GetAssembly(typeof(Raptor.Cli.Console.Arguments))!;
        var programType = assembly.GetType("Program") ?? 
                         assembly.GetTypes().FirstOrDefault(t => t.Name.Contains("Program") && t.IsClass);
        
        if (programType == null)
        {
            throw new InvalidOperationException("Program type not found. The IsHelpRequested method may need to be extracted to a testable class.");
        }

        var method = programType.GetMethod("IsHelpRequested", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
        
        if (method == null)
        {
            var allMethods = programType.GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public);
            method = allMethods.FirstOrDefault(m => 
                m.Name.Contains("IsHelpRequested", StringComparison.OrdinalIgnoreCase) &&
                m.ReturnType == typeof(bool) &&
                m.GetParameters().Length == 1 && 
                m.GetParameters()[0].ParameterType == typeof(string[]));
        }
        
        if (method == null)
        {
            throw new InvalidOperationException("IsHelpRequested method not found. The method may need to be extracted to a testable class.");
        }

        if (method.ReturnType != typeof(bool))
        {
            throw new InvalidOperationException($"IsHelpRequested method found but returns {method.ReturnType} instead of bool.");
        }

        var result = method.Invoke(null, new object[] { args });
        return (bool)result!;
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnTrue_WhenHelpLongFlagPresent()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnTrue_WhenHelpShortFlagPresent()
    {
        // Arrange
        var args = new[] { "-h" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnFalse_WhenNoHelpFlag()
    {
        // Arrange
        var args = new[] { "--url", "https://api.example.com" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnFalse_WhenEmptyArgs()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnTrue_WhenHelpFlagMixedWithOtherArgs()
    {
        // Arrange
        var args = new[] { "--url", "https://api.example.com", "--help" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnTrue_WhenHelpFlagAtStart()
    {
        // Arrange
        var args = new[] { "--help", "--url", "https://api.example.com" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnTrue_WhenShortHelpFlagMixedWithOtherArgs()
    {
        // Arrange
        var args = new[] { "--url", "https://api.example.com", "-h", "--concurrency", "5" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnFalse_WhenSimilarFlagsPresent()
    {
        // Arrange
        var args = new[] { "--help-me", "--helpful", "-help" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnFalse_WhenPartialMatch()
    {
        // Arrange
        var args = new[] { "--hel", "-" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnTrue_WhenMultipleHelpFlags()
    {
        // Arrange
        var args = new[] { "--help", "-h", "--help" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnFalse_WhenCaseDoesNotMatch()
    {
        // Arrange
        var args = new[] { "--HELP", "-H" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsHelpRequested_ShouldReturnFalse_WhenLongerStrings()
    {
        // Arrange
        var args = new[] { "--help-", "--helpful", "-h-", "-help" };

        // Act
        var result = IsHelpRequested(args);

        // Assert
        Assert.False(result);
    }
}

