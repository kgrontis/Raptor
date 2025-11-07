using System;

namespace Raptor.Cli.Console;

/// <summary>
/// Provides animated console spinner functionality to indicate ongoing operations.
/// Uses emoji characters that alternate between dinosaur types to show progress.
/// </summary>
internal static class Spinner
{
    private static readonly string[] Frames = new[]
    {
        "🦖 Running...",
        "🦕 Running..",
        "🦖 Running.",
        "🦕 Running",
        "🦖 Running.",
        "🦕 Running..",
        "🦖 Running..."
    };

    /// <summary>
    /// Displays an animated spinner in the console while waiting for an operation to complete.
    /// The spinner cycles through emoji frames at 150ms intervals until cancelled.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the spinner animation.</param>
    public static async Task ShowAsync(CancellationToken cancellationToken)
    {
        var index = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            System.Console.Write($"\r{Frames[index]}");
            index = (index + 1) % Frames.Length;

            try
            {
                await Task.Delay(150, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        System.Console.Write("\r" + new string(' ', Frames[0].Length) + "\r");
    }
}

