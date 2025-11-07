using BenchmarkDotNet.Attributes;
using Raptor.Cli.Console;
using Raptor.Cli.Core;

namespace Raptor.Benchmarks;

/// <summary>
/// Benchmarks for Arguments.Parse method comparing optimized Span-based approach vs alternatives.
/// </summary>
[MemoryDiagnoser]
public class ParseBenchmarks
{
    private string[] _simpleArgs = ["--url", "https://example.com", "--concurrency", "10", "--duration", "30"];
    private string[] _complexArgs =
    [
        "--url", "https://api.example.com/v1/users",
        "--method", "POST",
        "--concurrency", "50",
        "--requests", "1000",
        "--body", "{\"name\":\"John\",\"email\":\"john@example.com\"}",
        "--headers", "Content-Type:application/json,Authorization:Bearer token123,X-Custom-Header:value"
    ];

    [Benchmark(Baseline = true)]
    public Config Parse_Optimized_Simple()
    {
        return Arguments.Parse(_simpleArgs);
    }

    [Benchmark]
    public Config Parse_Optimized_Complex()
    {
        return Arguments.Parse(_complexArgs);
    }

    [Benchmark]
    public Config Parse_Alternative_StringBased_Simple()
    {
        return ArgumentsAlternatives.ParseStringBased(_simpleArgs);
    }

    [Benchmark]
    public Config Parse_Alternative_StringBased_Complex()
    {
        return ArgumentsAlternatives.ParseStringBased(_complexArgs);
    }

    [Benchmark]
    public Config Parse_Alternative_DictionaryBased_Simple()
    {
        return ArgumentsAlternatives.ParseDictionaryBased(_simpleArgs);
    }

    [Benchmark]
    public Config Parse_Alternative_DictionaryBased_Complex()
    {
        return ArgumentsAlternatives.ParseDictionaryBased(_complexArgs);
    }

    [Benchmark]
    public Config Parse_Alternative_LINQBased_Simple()
    {
        return ArgumentsAlternatives.ParseLINQBased(_simpleArgs);
    }

    [Benchmark]
    public Config Parse_Alternative_LINQBased_Complex()
    {
        return ArgumentsAlternatives.ParseLINQBased(_complexArgs);
    }
}

