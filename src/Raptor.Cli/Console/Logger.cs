using System;

namespace Raptor.Cli.Console;

/// <summary>
/// Provides color-coded logging functionality for the CLI.
/// </summary>
internal static class Logger
{
    /// <summary>
    /// Logs an informational message with a cyan [INFO] prefix.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Info(string message)
    {
        WriteColored("[INFO]", System.ConsoleColor.Cyan);
        System.Console.WriteLine($" {message}");
    }

    /// <summary>
    /// Logs a success message with a green [SUCCESS] prefix.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Success(string message)
    {
        WriteColored("[SUCCESS]", System.ConsoleColor.Green);
        System.Console.WriteLine($" {message}");
    }

    /// <summary>
    /// Logs a warning message with a yellow [WARN] prefix.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Warning(string message)
    {
        WriteColored("[WARN]", System.ConsoleColor.Yellow);
        System.Console.WriteLine($" {message}");
    }

    /// <summary>
    /// Logs an error message with a red [ERROR] prefix to the error output stream.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public static void Error(string message)
    {
        WriteColored("[ERROR]", System.ConsoleColor.Red);
        System.Console.Error.WriteLine($" {message}");
    }

    /// <summary>
    /// Writes text to the console in the specified color and restores the original foreground color.
    /// </summary>
    /// <param name="text">The text to write.</param>
    /// <param name="color">The color to use for the text.</param>
    private static void WriteColored(string text, System.ConsoleColor color)
    {
        var originalColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write(text);
        System.Console.ForegroundColor = originalColor;
    }
}

