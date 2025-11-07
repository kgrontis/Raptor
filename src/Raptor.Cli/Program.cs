using System.Runtime.CompilerServices;
using System.Text;
using Raptor.Cli.Console;
using Raptor.Cli.Core;
using Raptor.Cli.Statistics;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 0 || IsHelpRequested(args))
{
    Arguments.PrintUsage();
    return 0;
}

try
{
    var config = Arguments.Parse(args);

    if (!config.IsValid)
    {
        Logger.Error("Invalid configuration.");
        Logger.Error("Required: --url, --concurrency, and either --duration or --requests");
        Arguments.PrintUsage();
        return 1;
    }

    var estimatedRequestCount = config.RequestCount ?? (config.DurationSeconds.HasValue ? config.DurationSeconds.Value * config.Concurrency * 10 : 1000);

    using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

    Logger.Info("Starting load test...");
    Logger.Info($"URL: {config.Url}");
    Logger.Info($"Method: {config.Method}");
    Logger.Info($"Concurrency: {config.Concurrency}");
    Logger.Info($"{(config.DurationSeconds.HasValue ? $"Duration: {config.DurationSeconds}s" : $"Requests: {config.RequestCount}")}");
    Console.WriteLine();

    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
    };

    using var spinnerCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
    var spinnerTask = Spinner.ShowAsync(spinnerCts.Token);

    try
    {
        await loadTester.RunAsync(cts.Token);
    }
    finally
    {
        spinnerCts.Cancel();
        try
        {
            await spinnerTask;
        }
        catch (OperationCanceledException)
        {
        }
        Console.Write("\r" + new string(' ', 50) + "\r");
    }

    var stats = loadTester.GetStats();
    var durationSeconds = loadTester.GetElapsedSeconds();

    ResultsReporter.Report(stats, durationSeconds);

    return 0;
}
catch (OperationCanceledException)
{
    Logger.Warning("Operation cancelled by user.");
    return 0;
}
catch (ArgumentException ex)
{
    Logger.Info(ex.Message);
    Arguments.PrintUsage();
    return 0;
}
catch (Exception ex)
{
    Logger.Error($"Unexpected error: {ex.Message}");
    Arguments.PrintUsage();
    return 1;
}

/// <summary>
/// Determines if the help option (--help or -h) is present in the command-line arguments.
/// Uses optimized span-based comparison to avoid allocations.
/// </summary>
/// <param name="args">Command-line arguments to check.</param>
/// <returns><c>true</c> if help is requested; otherwise, <c>false</c>.</returns>
[MethodImpl(MethodImplOptions.AggressiveInlining)]
static bool IsHelpRequested(string[] args)
{
    const string HelpLong = "--help";
    const string HelpShort = "-h";

    for (var i = 0; i < args.Length; i++)
    {
        var arg = args[i].AsSpan();
        if ((arg.Length == HelpLong.Length && arg.SequenceEqual(HelpLong.AsSpan())) ||
            (arg.Length == HelpShort.Length && arg.SequenceEqual(HelpShort.AsSpan())))
        {
            return true;
        }
    }
    return false;
}
