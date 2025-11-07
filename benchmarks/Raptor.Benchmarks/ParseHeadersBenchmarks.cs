using BenchmarkDotNet.Attributes;
using Raptor.Cli.Console;

namespace Raptor.Benchmarks;

/// <summary>
/// Benchmarks for Arguments.ParseHeaders comparing optimized Span-based approach vs alternatives.
/// </summary>
[MemoryDiagnoser]
public class ParseHeadersBenchmarks
{
    private string _singleHeader = "Content-Type:application/json";
    private string _multipleHeaders = "Content-Type:application/json,Authorization:Bearer token123,Accept:text/html,X-Custom-Header:value with spaces";

    [Benchmark(Baseline = true)]
    public Dictionary<string, string> ParseHeaders_Optimized_Single()
    {
        var headers = new Dictionary<string, string>();
        Arguments.ParseHeaders(_singleHeader, headers);
        return headers;
    }

    [Benchmark]
    public Dictionary<string, string> ParseHeaders_Optimized_Multiple()
    {
        var headers = new Dictionary<string, string>();
        Arguments.ParseHeaders(_multipleHeaders, headers);
        return headers;
    }

    [Benchmark]
    public Dictionary<string, string> ParseHeaders_Alternative_Split_Single()
    {
        var headers = new Dictionary<string, string>();
        ArgumentsAlternatives.ParseHeadersSplit(_singleHeader, headers);
        return headers;
    }

    [Benchmark]
    public Dictionary<string, string> ParseHeaders_Alternative_Split_Multiple()
    {
        var headers = new Dictionary<string, string>();
        ArgumentsAlternatives.ParseHeadersSplit(_multipleHeaders, headers);
        return headers;
    }

    [Benchmark]
    public Dictionary<string, string> ParseHeaders_Alternative_Regex_Single()
    {
        var headers = new Dictionary<string, string>();
        ArgumentsAlternatives.ParseHeadersRegex(_singleHeader, headers);
        return headers;
    }

    [Benchmark]
    public Dictionary<string, string> ParseHeaders_Alternative_Regex_Multiple()
    {
        var headers = new Dictionary<string, string>();
        ArgumentsAlternatives.ParseHeadersRegex(_multipleHeaders, headers);
        return headers;
    }

    [Benchmark]
    public Dictionary<string, string> ParseHeaders_Alternative_LINQ_Single()
    {
        var headers = new Dictionary<string, string>();
        ArgumentsAlternatives.ParseHeadersLINQ(_singleHeader, headers);
        return headers;
    }

    [Benchmark]
    public Dictionary<string, string> ParseHeaders_Alternative_LINQ_Multiple()
    {
        var headers = new Dictionary<string, string>();
        ArgumentsAlternatives.ParseHeadersLINQ(_multipleHeaders, headers);
        return headers;
    }
}

