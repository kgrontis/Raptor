using System;
using System.IO;
using System.Linq;
using Raptor.Cli.Console;
using Xunit;

namespace Raptor.Tests.Console;

/// <summary>
/// Unit tests for the <see cref="Arguments"/> class.
/// </summary>
[Collection("ConsoleOutputTests")]
public class ArgumentsTests : IDisposable
{
    private static readonly object _lock = new object();
    private readonly TextWriter _originalOut;

    public ArgumentsTests()
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
    public void Parse_ShouldReturnDefaultConfig_WhenNoArgumentsProvided()
    {
        var args = Array.Empty<string>();

        var config = Arguments.Parse(args);
        Assert.Null(config.Url);
        Assert.Equal(System.Net.Http.HttpMethod.Get, config.Method);
        Assert.Equal(1, config.Concurrency);
        Assert.Null(config.Headers);
    }

    [Fact]
    public void Parse_ShouldParseUrl_WhenUrlOptionProvided()
    {
        var args = new[] { "--url", "https://api.example.com/users" };

        var config = Arguments.Parse(args);
        Assert.Equal("https://api.example.com/users", config.Url);
    }

    [Fact]
    public void Parse_ShouldParseMethod_WhenMethodOptionProvided()
    {
        var args = new[] { "--method", "POST" };

        var config = Arguments.Parse(args);

        Assert.Equal(System.Net.Http.HttpMethod.Post, config.Method);
    }

    [Fact]
    public void Parse_ShouldParseMethodCaseInsensitive()
    {
        Assert.Equal(System.Net.Http.HttpMethod.Get, Arguments.Parse(new[] { "--method", "get" }).Method);
        Assert.Equal(System.Net.Http.HttpMethod.Get, Arguments.Parse(new[] { "--method", "GET" }).Method);
        Assert.Equal(System.Net.Http.HttpMethod.Get, Arguments.Parse(new[] { "--method", "Get" }).Method);

        Assert.Equal(System.Net.Http.HttpMethod.Post, Arguments.Parse(new[] { "--method", "post" }).Method);
        Assert.Equal(System.Net.Http.HttpMethod.Put, Arguments.Parse(new[] { "--method", "put" }).Method);
        Assert.Equal(System.Net.Http.HttpMethod.Delete, Arguments.Parse(new[] { "--method", "delete" }).Method);
    }

    [Fact]
    public void Parse_ShouldParseConcurrency_WhenConcurrencyOptionProvided()
    {
        var args = new[] { "--concurrency", "10" };

        var config = Arguments.Parse(args);

        Assert.Equal(10, config.Concurrency);
    }

    [Fact]
    public void Parse_ShouldParseDuration_WhenDurationOptionProvided()
    {
        var args = new[] { "--duration", "30" };

        var config = Arguments.Parse(args);

        Assert.Equal((ushort?)30, config.DurationSeconds);
    }

    [Fact]
    public void Parse_ShouldParseRequests_WhenRequestsOptionProvided()
    {
        var args = new[] { "--requests", "100" };

        var config = Arguments.Parse(args);

        Assert.Equal((ushort?)100, config.RequestCount);
    }

    [Fact]
    public void Parse_ShouldParseBody_WhenBodyOptionProvided()
    {
        var args = new[] { "--body", "{\"key\":\"value\"}" };

        var config = Arguments.Parse(args);

        Assert.Equal("{\"key\":\"value\"}", config.Body);
    }

    [Fact]
    public void Parse_ShouldParseHeaders_WhenHeadersOptionProvided()
    {
        var args = new[] { "--headers", "Content-Type:application/json,Authorization:Bearer token123" };

        var config = Arguments.Parse(args);

        Assert.NotNull(config.Headers);
        Assert.Equal("application/json", config.Headers["Content-Type"]);
        Assert.Equal("Bearer token123", config.Headers["Authorization"]);
    }

    [Fact]
    public void Parse_ShouldParseHeadersWithSemicolonDelimiter()
    {
        var args = new[] { "--headers", "Content-Type:application/json;Authorization:Bearer token123" };

        var config = Arguments.Parse(args);

        Assert.NotNull(config.Headers);
        Assert.Equal("application/json", config.Headers["Content-Type"]);
        Assert.Equal("Bearer token123", config.Headers["Authorization"]);
    }

    [Fact]
    public void Parse_ShouldParseHeadersWithSpaces()
    {
        var args = new[] { "--headers", "Content-Type: application/json , Authorization : Bearer token" };

        var config = Arguments.Parse(args);

        Assert.NotNull(config.Headers);
        Assert.Equal("application/json", config.Headers["Content-Type"]);
        Assert.Equal("Bearer token", config.Headers["Authorization"]);
    }

    [Fact]
    public void Parse_ShouldParseMultipleOptions()
    {
        var args = new[]
        {
            "--url", "https://api.example.com/users",
            "--method", "POST",
            "--concurrency", "5",
            "--duration", "60",
            "--body", "{\"test\":\"data\"}",
            "--headers", "Content-Type:application/json"
        };

        var config = Arguments.Parse(args);

        Assert.Equal("https://api.example.com/users", config.Url);
        Assert.Equal(System.Net.Http.HttpMethod.Post, config.Method);
        Assert.Equal(5, config.Concurrency);
        Assert.Equal((ushort?)60, config.DurationSeconds);
        Assert.Equal("{\"test\":\"data\"}", config.Body);
        Assert.NotNull(config.Headers);
        Assert.Equal("application/json", config.Headers["Content-Type"]);
    }

    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenOptionMissingValue()
    {
        var args = new[] { "--url" };

        var exception = Assert.Throws<ArgumentException>(() => Arguments.Parse(args));
        Assert.Contains("Missing value for option", exception.Message);
    }

    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenConcurrencyIsInvalid()
    {
        var exception1 = Assert.Throws<ArgumentException>(() => Arguments.Parse(new[] { "--concurrency", "0" }))!;
        Assert.Contains("Invalid concurrency value", exception1.Message);

        var exception2 = Assert.Throws<ArgumentException>(() => Arguments.Parse(new[] { "--concurrency", "-1" }));
        Assert.Contains("Invalid concurrency value", exception2.Message);

        var exception3 = Assert.Throws<ArgumentException>(() => Arguments.Parse(new[] { "--concurrency", "abc" }));
        Assert.Contains("Invalid concurrency value", exception3.Message);
    }

    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenDurationIsInvalid()
    {
        var exception1 = Assert.Throws<ArgumentException>(() => Arguments.Parse(new[] { "--duration", "0" }));
        Assert.Contains("Invalid duration value", exception1.Message);

        var exception2 = Assert.Throws<ArgumentException>(() => Arguments.Parse(new[] { "--duration", "abc" }));
        Assert.Contains("Invalid duration value", exception2.Message);
    }

    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenRequestsIsInvalid()
    {
        var exception1 = Assert.Throws<ArgumentException>(() => Arguments.Parse(new[] { "--requests", "0" }));
        Assert.Contains("Invalid requests value", exception1.Message);

        var exception2 = Assert.Throws<ArgumentException>(() => Arguments.Parse(new[] { "--requests", "abc" }));
        Assert.Contains("Invalid requests value", exception2.Message);
    }

    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenMethodIsUnsupported()
    {
        var exception = Assert.Throws<ArgumentException>(() => Arguments.Parse(new[] { "--method", "PATCH" }));
        Assert.Contains("Unsupported HTTP method", exception.Message);
    }

    [Fact]
    public void Parse_ShouldThrowArgumentException_WhenUnknownOptionProvided()
    {
        var exception = Assert.Throws<ArgumentException>(() => Arguments.Parse(new[] { "--unknown", "value" }));
        Assert.Contains("Unknown option", exception.Message);
    }

    [Fact]
    public void Parse_ShouldIgnoreEmptyArguments()
    {
        var args = new[] { "", "--url", "https://api.example.com", "", "--concurrency", "5", "" };

        var config = Arguments.Parse(args);

        Assert.Equal("https://api.example.com", config.Url);
        Assert.Equal(5, config.Concurrency);
    }

    [Fact]
    public void ParseMethod_ShouldReturnGet_ForGetMethod()
    {
        var result = Arguments.ParseMethod("GET".AsSpan());

        Assert.Equal(System.Net.Http.HttpMethod.Get, result);
    }

    [Fact]
    public void ParseMethod_ShouldReturnPost_ForPostMethod()
    {
        var result = Arguments.ParseMethod("POST".AsSpan());

        Assert.Equal(System.Net.Http.HttpMethod.Post, result);
    }

    [Fact]
    public void ParseMethod_ShouldReturnPut_ForPutMethod()
    {
        var result = Arguments.ParseMethod("PUT".AsSpan());

        Assert.Equal(System.Net.Http.HttpMethod.Put, result);
    }

    [Fact]
    public void ParseMethod_ShouldReturnDelete_ForDeleteMethod()
    {
        var result = Arguments.ParseMethod("DELETE".AsSpan());

        Assert.Equal(System.Net.Http.HttpMethod.Delete, result);
    }

    [Fact]
    public void ParseMethod_ShouldThrowArgumentException_ForUnsupportedMethod()
    {
        var exception = Assert.Throws<ArgumentException>(() => Arguments.ParseMethod("PATCH".AsSpan()));
        Assert.Contains("Unsupported HTTP method", exception.Message);
    }

    [Fact]
    public void ParseHeaders_ShouldParseSingleHeader()
    {
        var headers = new Dictionary<string, string>();

        Arguments.ParseHeaders("Content-Type:application/json".AsSpan(), headers);

        Assert.Single(headers);
        Assert.Equal("application/json", headers["Content-Type"]);
    }

    [Fact]
    public void ParseHeaders_ShouldParseMultipleHeaders()
    {
        var headers = new System.Collections.Generic.Dictionary<string, string>();

        Arguments.ParseHeaders("Content-Type:application/json,Authorization:Bearer token".AsSpan(), headers);

        Assert.Equal(2, headers.Count);
        Assert.Equal("application/json", headers["Content-Type"]);
        Assert.Equal("Bearer token", headers["Authorization"]);
    }

    [Fact]
    public void ParseHeaders_ShouldHandleHeadersWithSemicolonDelimiter()
    {
        var headers = new System.Collections.Generic.Dictionary<string, string>();

        Arguments.ParseHeaders("Content-Type:application/json;Authorization:Bearer token".AsSpan(), headers);

        Assert.Equal(2, headers.Count);
        Assert.Equal("application/json", headers["Content-Type"]);
        Assert.Equal("Bearer token", headers["Authorization"]);
    }

    [Fact]
    public void ParseHeaders_ShouldTrimSpaces()
    {
        var headers = new System.Collections.Generic.Dictionary<string, string>();

        Arguments.ParseHeaders(" Content-Type : application/json ".AsSpan(), headers);

        Assert.Single(headers);
        Assert.Equal("application/json", headers["Content-Type"]);
    }

    [Fact]
    public void ParseHeaders_ShouldIgnoreInvalidPairs()
    {
        var headers = new System.Collections.Generic.Dictionary<string, string>();

        Arguments.ParseHeaders(":value,key:,invalid".AsSpan(), headers);

        Assert.Empty(headers);
    }

    [Fact]
    public void PrintUsage_ShouldWriteUsageToConsole()
    {
        StringWriter sw;
        lock (_lock)
        {
            System.Console.SetOut(_originalOut);
            sw = new StringWriter();
            System.Console.SetOut(sw);
        }

        try
        {
            Arguments.PrintUsage();

            var output = sw.ToString();
            Assert.Contains("Usage: raptor", output);
            Assert.Contains("--url", output);
            Assert.Contains("--concurrency", output);
            Assert.Contains("--duration", output);
            Assert.Contains("--requests", output);
            Assert.Contains("--method", output);
            Assert.Contains("--body", output);
            Assert.Contains("--headers", output);
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

