using BenchmarkDotNet.Attributes;
using Raptor.Cli.Console;

namespace Raptor.Benchmarks;

/// <summary>
/// Benchmarks for Arguments.ParseMethod comparing optimized Span-based approach vs alternatives.
/// </summary>
[MemoryDiagnoser]
public class ParseMethodBenchmarks
{
    private readonly string _getMethod = "GET";
    private readonly string _postMethod = "POST";

    [Benchmark(Baseline = true)]
    public HttpMethod ParseMethod_Optimized_GET()
    {
        return Arguments.ParseMethod(_getMethod.AsSpan());
    }

    [Benchmark]
    public HttpMethod ParseMethod_Optimized_POST()
    {
        return Arguments.ParseMethod(_postMethod.AsSpan());
    }

    [Benchmark]
    public HttpMethod ParseMethod_Alternative_String_GET()
    {
        return ArgumentsAlternatives.ParseMethodString("GET");
    }

    [Benchmark]
    public HttpMethod ParseMethod_Alternative_String_POST()
    {
        return ArgumentsAlternatives.ParseMethodString("POST");
    }

    [Benchmark]
    public HttpMethod ParseMethod_Alternative_Switch_GET()
    {
        return ArgumentsAlternatives.ParseMethodSwitch("GET");
    }

    [Benchmark]
    public HttpMethod ParseMethod_Alternative_Switch_POST()
    {
        return ArgumentsAlternatives.ParseMethodSwitch("POST");
    }
}

