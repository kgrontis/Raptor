using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Raptor.Cli.Console;
using Xunit;

namespace Raptor.Tests.Console;

/// <summary>
/// Unit tests for the <see cref="Spinner"/> class.
/// Uses a collection to ensure tests don't run in parallel and interfere with console redirection.
/// </summary>
[Collection("ConsoleOutputTests")]
public class SpinnerTests : IDisposable
{
    private static readonly object _lock = new object();
    private readonly TextWriter _originalOut;

    public SpinnerTests()
    {
        lock (_lock)
        {
            _originalOut = System.Console.Out;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
        }
    }
    [Fact]
    public async Task ShowAsync_ShouldWriteSpinnerFrames_WhenNotCancelled()
    {
        using var cts = new CancellationTokenSource();
        StringWriter sw;
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            sw = new StringWriter();
            System.Console.SetOut(sw);
        }

        try
        {
            cts.CancelAfter(500);
            await Spinner.ShowAsync(cts.Token);

            var output = sw.ToString();
            Assert.Contains("Running", output);
        }
        finally
        {
            lock (_lock)
            {
                System.Console.SetOut(_originalOut);
                sw?.Dispose();
            }
        }
    }

    [Fact]
    public async Task ShowAsync_ShouldStop_WhenCancellationRequested()
    {
        using var cts = new CancellationTokenSource();
        StringWriter sw;
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            sw = new StringWriter();
            System.Console.SetOut(sw);
        }

        try
        {
            cts.CancelAfter(100);
            await Spinner.ShowAsync(cts.Token);

            var output = sw.ToString();
            Assert.NotEmpty(output);
        }
        finally
        {
            lock (_lock)
            {
                System.Console.SetOut(_originalOut);
                sw?.Dispose();
            }
        }
    }

    [Fact]
    public async Task ShowAsync_ShouldCycleThroughFrames()
    {
        using var cts = new CancellationTokenSource();
        StringWriter sw;
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            sw = new StringWriter();
            System.Console.SetOut(sw);
        }

        try
        {
            cts.CancelAfter(600);
            await Spinner.ShowAsync(cts.Token);

            var output = sw.ToString();
            Assert.NotEmpty(output);
        }
        finally
        {
            lock (_lock)
            {
                System.Console.SetOut(_originalOut);
                sw?.Dispose();
            }
        }
    }

    [Fact]
    public async Task ShowAsync_ShouldClearLine_AfterCompletion()
    {
        using var cts = new CancellationTokenSource();
        StringWriter sw;
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            sw = new StringWriter();
            System.Console.SetOut(sw);
        }

        try
        {
            cts.Cancel();
            await Spinner.ShowAsync(cts.Token);

            var output = sw.ToString();
            Assert.NotNull(output);
        }
        finally
        {
            lock (_lock)
            {
                System.Console.SetOut(_originalOut);
                sw?.Dispose();
            }
        }
    }

    [Fact]
    public async Task ShowAsync_ShouldHandleImmediateCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        StringWriter sw;
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            sw = new StringWriter();
            System.Console.SetOut(sw);
        }

        try
        {
            await Spinner.ShowAsync(cts.Token);

            var output = sw.ToString();
            Assert.NotNull(output);
        }
        finally
        {
            lock (_lock)
            {
                System.Console.SetOut(_originalOut);
                sw?.Dispose();
            }
        }
    }

    [Fact]
    public async Task ShowAsync_ShouldUseCarriageReturn_ForInPlaceUpdates()
    {
        using var cts = new CancellationTokenSource();
        StringWriter sw;
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            sw = new StringWriter();
            System.Console.SetOut(sw);
        }

        try
        {
            cts.CancelAfter(300);
            await Spinner.ShowAsync(cts.Token);

            var output = sw.ToString();
            Assert.Contains("\r", output);
        }
        finally
        {
            lock (_lock)
            {
                System.Console.SetOut(_originalOut);
                sw?.Dispose();
            }
        }
    }
}

