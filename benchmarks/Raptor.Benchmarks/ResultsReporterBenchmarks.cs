using BenchmarkDotNet.Attributes;
using Raptor.Cli.Core;
using Raptor.Cli.Statistics;
using System.Diagnostics;

namespace Raptor.Benchmarks;

/// <summary>
/// Benchmarks for ResultsReporter string formatting operations.
/// </summary>
[MemoryDiagnoser]
public class ResultsReporterBenchmarks
{
    private StatsCollector _collector = null!;

    [Params(100, 1000, 10000)]
    public int Capacity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _collector = new StatsCollector(Capacity);
        var random = new Random(42);
        for (var i = 0; i < Capacity; i++)
        {
            _collector.Record(
                Stopwatch.GetTimestamp(),
                random.Next(200, 600),
                random.Next(100) < 5,
                random.Next(1, 1000)
            );
        }
    }

    [Benchmark(Baseline = true)]
    public void Report_Small()
    {
        var smallCollector = new StatsCollector(100);
        var random = new Random(42);
        for (var i = 0; i < 100; i++)
        {
            smallCollector.Record(
                Stopwatch.GetTimestamp(),
                random.Next(200, 600),
                random.Next(100) < 5,
                random.Next(1, 1000)
            );
        }
        ResultsReporter.Report(smallCollector, 10.0);
    }

    [Benchmark]
    public void Report_Medium()
    {
        var mediumCollector = new StatsCollector(1000);
        var random = new Random(42);
        for (var i = 0; i < 1000; i++)
        {
            mediumCollector.Record(
                Stopwatch.GetTimestamp(),
                random.Next(200, 600),
                random.Next(100) < 5,
                random.Next(1, 1000)
            );
        }
        ResultsReporter.Report(mediumCollector, 30.0);
    }

    [Benchmark]
    public void Report_Large()
    {
        ResultsReporter.Report(_collector, 60.0);
    }
}

