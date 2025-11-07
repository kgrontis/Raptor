using BenchmarkDotNet.Attributes;
using Raptor.Cli.Core;
using Raptor.Cli.Statistics;
using System.Diagnostics;

namespace Raptor.Benchmarks;

/// <summary>
/// Benchmarks for StatsCollector operations including Record() and GetLatencyStats().
/// </summary>
[MemoryDiagnoser]
public class StatsCollectorBenchmarks
{
    private StatsCollector _collector = null!;
    private long[] _testLatencies = null!;

    [Params(100, 1000, 10000)]
    public int Capacity { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _collector = new StatsCollector(Capacity);
        _testLatencies = new long[Capacity];
        var random = new Random(42);
        for (var i = 0; i < Capacity; i++)
        {
            _testLatencies[i] = random.Next(1, 1000);
        }

        for (var i = 0; i < Capacity; i++)
        {
            _collector.Record(
                Stopwatch.GetTimestamp(),
                i % 600,
                i % 10 == 0,
                _testLatencies[i]
            );
        }
    }

    [Benchmark(Baseline = true)]
    public void Record_Single()
    {
        _collector.Record(Stopwatch.GetTimestamp(), 200, false, 50);
    }

    [Benchmark]
    public object GetLatencyStats()
    {
        return _collector.GetLatencyStats();
    }

    [Benchmark]
    public Dictionary<int, int> GetStatusCodes()
    {
        return _collector.GetStatusCodes();
    }

    [Benchmark]
    public object GetResults()
    {
        return _collector.GetResults();
    }
}

