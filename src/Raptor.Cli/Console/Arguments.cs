using System.Runtime.CompilerServices;
using Raptor.Cli.Core;

namespace Raptor.Cli.Console;

/// <summary>
/// Argument parser for the Raptor CLI.
/// </summary>
public static class Arguments
{
    private static readonly string OptionUrl = "url";
    private static readonly string OptionMethod = "method";
    private static readonly string OptionConcurrency = "concurrency";
    private static readonly string OptionDuration = "duration";
    private static readonly string OptionRequests = "requests";
    private static readonly string OptionBody = "body";
    private static readonly string OptionHeaders = "headers";
    private static readonly string MethodGet = "GET";
    private static readonly string MethodPost = "POST";
    private static readonly string MethodPut = "PUT";
    private static readonly string MethodDelete = "DELETE";

    /// <summary>
    /// Parses command-line arguments and returns a <see cref="Config"/> object.
    /// Supports both --option and -option style arguments with values.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <returns>A <see cref="Config"/> object containing the parsed configuration.</returns>
    /// <exception cref="ArgumentException">Thrown when an option is missing a value or contains invalid data.</exception>
    public static Config Parse(string[] args)
    {
        var config = new Config
        {
            Method = HttpMethod.Get,
            Concurrency = 1,
            Headers = null
        };

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i].AsSpan();

            if (arg.Length == 0) continue;

            if (arg.Length >= 2 && arg[0] == '-' && arg[1] == '-')
            {
                var optionName = arg[2..];

                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException($"Missing value for option: {args[i]}");
                }

                var value = args[i + 1];
                i++;

                if (optionName.Length == OptionUrl.Length && optionName.SequenceEqual(OptionUrl.AsSpan()))
                {
                    config.Url = value;
                }
                else if (optionName.Length == OptionBody.Length && optionName.SequenceEqual(OptionBody.AsSpan()))
                {
                    config.Body = value;
                }
                else if (optionName.Length == OptionMethod.Length && optionName.SequenceEqual(OptionMethod.AsSpan()))
                {
                    config.Method = ParseMethod(value.AsSpan());
                }
                else if (optionName.Length == OptionConcurrency.Length && optionName.SequenceEqual(OptionConcurrency.AsSpan()))
                {
                    if (!ushort.TryParse(value, out var concurrency) || concurrency <= 0)
                    {
                        throw new ArgumentException($"Invalid concurrency value: {value}. Must be a positive integer.");
                    }
                    config.Concurrency = concurrency;
                }
                else if (optionName.Length == OptionDuration.Length && optionName.SequenceEqual(OptionDuration.AsSpan()))
                {
                    if (!ushort.TryParse(value, out var duration) || duration <= 0)
                    {
                        throw new ArgumentException($"Invalid duration value: {value}. Must be a positive integer.");
                    }
                    config.DurationSeconds = duration;
                }
                else if (optionName.Length == OptionRequests.Length && optionName.SequenceEqual(OptionRequests.AsSpan()))
                {
                    if (!ushort.TryParse(value, out var requests) || requests <= 0)
                    {
                        throw new ArgumentException($"Invalid requests value: {value}. Must be a positive integer.");
                    }
                    config.RequestCount = requests;
                }
                else if (optionName.Length == OptionHeaders.Length && optionName.SequenceEqual(OptionHeaders.AsSpan()))
                {
                    config.Headers ??= [];
                    ParseHeaders(value.AsSpan(), config.Headers);
                }
                else
                {
                    throw new ArgumentException($"Unknown option: {args[i]}");
                }
            }
        }

        if (config.DurationSeconds.HasValue && config.RequestCount.HasValue)
        {
            throw new ArgumentException("Cannot specify both --duration and --requests. Please use either --duration or --requests, not both.");
        }

        return config;
    }

    /// <summary>
    /// Parses an HTTP method string and returns the corresponding <see cref="HttpMethod"/>.
    /// Supports GET, POST, PUT, and DELETE methods (case-insensitive).
    /// </summary>
    /// <param name="method">The HTTP method string to parse.</param>
    /// <returns>The corresponding <see cref="HttpMethod"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the method is not supported.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static HttpMethod ParseMethod(ReadOnlySpan<char> method)
    {
        if (method.Length == 3 && method.Equals(MethodGet.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return HttpMethod.Get;

        if (method.Length == 4 && method.Equals(MethodPost.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return HttpMethod.Post;

        if (method.Length == 3 && method.Equals(MethodPut.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return HttpMethod.Put;

        if (method.Length == 6 && method.Equals(MethodDelete.AsSpan(), StringComparison.OrdinalIgnoreCase))
            return HttpMethod.Delete;

        throw new ArgumentException($"Unsupported HTTP method: {new string(method)}. Supported: GET, POST, PUT, DELETE");
    }

    /// <summary>
    /// Parses header string in the format "key:value,key2:value2" and adds them to the headers dictionary.
    /// Supports comma or semicolon as delimiters between header pairs.
    /// </summary>
    /// <param name="headerString">The header string to parse.</param>
    /// <param name="headers">The dictionary to add parsed headers to.</param>
    internal static void ParseHeaders(ReadOnlySpan<char> headerString, Dictionary<string, string> headers)
    {
        var start = 0;

        for (var i = 0; i <= headerString.Length; i++)
        {
            if (i == headerString.Length || headerString[i] == ',' || headerString[i] == ';')
            {
                if (i > start)
                {
                    var pair = headerString[start..i];
                    var colonIndex = pair.IndexOf(':');

                    if (colonIndex > 0 && colonIndex < pair.Length - 1)
                    {
                        var keySpan = pair[0..colonIndex].Trim();
                        var valueSpan = pair[(colonIndex + 1)..].Trim();

                        if (keySpan.Length > 0)
                        {
                            headers[new string(keySpan)] = new string(valueSpan);
                        }
                    }
                }
                start = i + 1;
            }
        }
    }

    /// <summary>
    /// Prints usage information and available command-line options to the console.
    /// </summary>
    public static void PrintUsage()
    {
        System.Console.WriteLine("Usage: raptor [options]");
        System.Console.WriteLine();
        System.Console.WriteLine("Required:");
        System.Console.WriteLine("  --url <url>                     Target URL to load test");
        System.Console.WriteLine("  --concurrency <n>               Number of concurrent requests (default: 1)");
        System.Console.WriteLine("  --duration <seconds>            Run for specified duration in seconds");
        System.Console.WriteLine("  --requests <n>                  Run until specified number of requests complete");
        System.Console.WriteLine();
        System.Console.WriteLine("Optional:");
        System.Console.WriteLine("  --method <method>               HTTP method: GET, POST, PUT, DELETE (default: GET)");
        System.Console.WriteLine("  --body <json>                   Request body (for POST/PUT)");
        System.Console.WriteLine("  --headers <key:value,...>       Custom headers as key:value pairs");
        System.Console.WriteLine();
        System.Console.WriteLine("Note: Specify either --duration or --requests, not both.");
    }
}

