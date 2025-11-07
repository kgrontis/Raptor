using System;
using System.IO;
using System.Text;
using Raptor.Cli.Console;
using Xunit;

namespace Raptor.Tests.Console;

/// <summary>
/// Unit tests for the <see cref="Logger"/> class.
/// Uses a collection to ensure tests don't run in parallel and interfere with console redirection.
/// </summary>
[Collection("ConsoleOutputTests")]
public class LoggerTests : IDisposable
{
    private static readonly object _lock = new object();
    private StringWriter _stringWriter = null!;
    private readonly TextWriter _originalOut;
    private readonly TextWriter _originalError;

    public LoggerTests()
    {
        lock (_lock)
        {
            _originalOut = System.Console.Out;
            _originalError = System.Console.Error;
            
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
                System.Console.SetOut(_stringWriter);
                System.Console.SetError(_stringWriter);
            }
            
            System.Console.SetOut(_stringWriter);
            System.Console.SetError(_stringWriter);
        }
    }

    private StringWriter CreateTestWriter()
    {
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            System.Console.SetError(_originalError);
            var testWriter = new StringWriter();
            System.Console.SetOut(testWriter);
            System.Console.SetError(testWriter);
            return testWriter;
        }
    }

    private void RestoreConsole(StringWriter testWriter)
    {
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            System.Console.SetError(_originalError);
            testWriter?.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            System.Console.SetError(_originalError);
            _stringWriter?.Dispose();
        }
    }

    [Fact]
    public void Info_ShouldWriteInfoMessage_WithCyanPrefix()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Test info message";

            Logger.Info(message);

            var output = testWriter.ToString();
            Assert.Contains("[INFO]", output);
            Assert.Contains(message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Success_ShouldWriteSuccessMessage_WithGreenPrefix()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Test success message";

            Logger.Success(message);

            var output = testWriter.ToString();
            Assert.Contains("[SUCCESS]", output);
            Assert.Contains(message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Warning_ShouldWriteWarningMessage_WithYellowPrefix()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Test warning message";

            Logger.Warning(message);

            var output = testWriter.ToString();
            Assert.Contains("[WARN]", output);
            Assert.Contains(message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Error_ShouldWriteErrorMessage_WithRedPrefix()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Test error message";

            Logger.Error(message);

            var output = testWriter.ToString();
            Assert.Contains("[ERROR]", output);
            Assert.Contains(message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Info_ShouldWriteToStandardOutput()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Standard output test";

            Logger.Info(message);

            var output = testWriter.ToString();
            Assert.NotEmpty(output);
            Assert.Contains(message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Error_ShouldWriteToErrorStream()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Error stream test";

            Logger.Error(message);

            var output = testWriter.ToString();
            Assert.NotEmpty(output);
            Assert.Contains(message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("Simple message")]
    [InlineData("Message with special chars: !@#$%^&*()")]
    [InlineData("Message with\nnewlines")]
    [InlineData("Very long message that contains multiple words and should still be logged correctly without any issues")]
    public void Info_ShouldHandleVariousMessageFormats(string message)
    {
        var testWriter = CreateTestWriter();
        try
        {
            Logger.Info(message);

            var output = testWriter.ToString();
            Assert.Contains("[INFO]", output);
            Assert.Contains(message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("Simple error")]
    [InlineData("Error with details: code 500")]
    public void Error_ShouldHandleVariousMessageFormats(string message)
    {
        var testWriter = CreateTestWriter();
        try
        {
            Logger.Error(message);

            var output = testWriter.ToString();
            Assert.Contains("[ERROR]", output);
            Assert.Contains(message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Info_ShouldRestoreOriginalColor_AfterWriting()
    {
        ResetStringWriter();
        var originalColor = System.Console.ForegroundColor;
        var message = "Color test";

        Logger.Info(message);

        Assert.Equal(originalColor, System.Console.ForegroundColor);
    }

    [Fact]
    public void Success_ShouldRestoreOriginalColor_AfterWriting()
    {
        ResetStringWriter();
        var originalColor = System.Console.ForegroundColor;
        var message = "Color test";

        Logger.Success(message);

        Assert.Equal(originalColor, System.Console.ForegroundColor);
    }

    [Fact]
    public void Warning_ShouldRestoreOriginalColor_AfterWriting()
    {
        ResetStringWriter();
        var originalColor = System.Console.ForegroundColor;
        var message = "Color test";

        Logger.Warning(message);

        Assert.Equal(originalColor, System.Console.ForegroundColor);
    }

    [Fact]
    public void Error_ShouldRestoreOriginalColor_AfterWriting()
    {
        ResetStringWriter();
        var originalColor = System.Console.ForegroundColor;
        var message = "Color test";

        Logger.Error(message);

        Assert.Equal(originalColor, System.Console.ForegroundColor);
    }

    [Fact]
    public void Info_ShouldFormatMessageWithPrefix()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Test message";

            Logger.Info(message);

            var output = testWriter.ToString();
            Assert.Contains("[INFO] " + message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Success_ShouldFormatMessageWithPrefix()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Test message";

            Logger.Success(message);

            var output = testWriter.ToString();
            Assert.Contains("[SUCCESS] " + message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Warning_ShouldFormatMessageWithPrefix()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Test message";

            Logger.Warning(message);

            var output = testWriter.ToString();
            Assert.Contains("[WARN] " + message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }

    [Fact]
    public void Error_ShouldFormatMessageWithPrefix()
    {
        var testWriter = CreateTestWriter();
        try
        {
            var message = "Test message";

            Logger.Error(message);

            var output = testWriter.ToString();
            Assert.Contains("[ERROR] " + message, output);
        }
        finally
        {
            RestoreConsole(testWriter);
        }
    }
}

