using System.Runtime.CompilerServices;
using Raptor.Cli.Console;
using Raptor.Cli.Core;

namespace Raptor.Benchmarks;

/// <summary>
/// Alternative implementations for comparison benchmarks.
/// </summary>
internal static class ArgumentsAlternatives
{
    #region Parse Alternatives

    /// <summary>
    /// String-based parsing (simpler but allocates more)
    /// </summary>
    public static Config ParseStringBased(string[] args)
    {
        var config = new Config
        {
            Method = HttpMethod.Get,
            Concurrency = 1,
            Headers = null
        };

        for (var i = 0; i < args.Length; i++)
        {
            if (string.IsNullOrEmpty(args[i])) continue;

            if (args[i].StartsWith("--"))
            {
                var optionName = args[i].Substring(2); // Substring allocates

                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException($"Missing value for option: {args[i]}");
                }

                var value = args[i + 1];
                i++;

                switch (optionName)
                {
                    case "url":
                        config.Url = value;
                        break;
                    case "method":
                        config.Method = ParseMethodString(value);
                        break;
                    case "concurrency":
                        if (!ushort.TryParse(value, out var concurrency) || concurrency <= 0)
                        {
                            throw new ArgumentException($"Invalid concurrency value: {value}");
                        }
                        config.Concurrency = concurrency;
                        break;
                    case "duration":
                        if (!ushort.TryParse(value, out var duration) || duration <= 0)
                        {
                            throw new ArgumentException($"Invalid duration value: {value}");
                        }
                        config.DurationSeconds = duration;
                        break;
                    case "requests":
                        if (!ushort.TryParse(value, out var requests) || requests <= 0)
                        {
                            throw new ArgumentException($"Invalid requests value: {value}");
                        }
                        config.RequestCount = requests;
                        break;
                    case "body":
                        config.Body = value;
                        break;
                    case "headers":
                        config.Headers ??= new Dictionary<string, string>();
                        ParseHeadersSplit(value, config.Headers);
                        break;
                    default:
                        throw new ArgumentException($"Unknown option: {args[i]}");
                }
            }
        }

        return config;
    }

    /// <summary>
    /// Dictionary-based parsing with pre-populated lookup
    /// </summary>
    public static Config ParseDictionaryBased(string[] args)
    {
        var config = new Config
        {
            Method = HttpMethod.Get,
            Concurrency = 1,
            Headers = null
        };

        var optionHandlers = new Dictionary<string, Action<string, Config, string>>
        {
            { "url", (value, cfg, _) => cfg.Url = value },
            { "method", (value, cfg, _) => cfg.Method = ParseMethodString(value) },
            { "concurrency", (value, cfg, _) => {
                if (!ushort.TryParse(value, out var c) || c <= 0)
                    throw new ArgumentException($"Invalid concurrency: {value}");
                cfg.Concurrency = c;
            }},
            { "duration", (value, cfg, _) => {
                if (!ushort.TryParse(value, out var d) || d <= 0)
                    throw new ArgumentException($"Invalid duration: {value}");
                cfg.DurationSeconds = d;
            }},
            { "requests", (value, cfg, _) => {
                if (!ushort.TryParse(value, out var r) || r <= 0)
                    throw new ArgumentException($"Invalid requests: {value}");
                cfg.RequestCount = r;
            }},
            { "body", (value, cfg, _) => cfg.Body = value },
            { "headers", (value, cfg, arg) => {
                cfg.Headers ??= new Dictionary<string, string>();
                ParseHeadersSplit(value, cfg.Headers);
            }}
        };

        for (var i = 0; i < args.Length; i++)
        {
            if (string.IsNullOrEmpty(args[i])) continue;

            if (args[i].StartsWith("--"))
            {
                var optionName = args[i].Substring(2);

                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException($"Missing value for option: {args[i]}");
                }

                var value = args[i + 1];
                i++;

                if (optionHandlers.TryGetValue(optionName, out var handler))
                {
                    handler(value, config, args[i]);
                }
                else
                {
                    throw new ArgumentException($"Unknown option: {args[i]}");
                }
            }
        }

        return config;
    }

    /// <summary>
    /// LINQ-based parsing (more allocations)
    /// </summary>
    public static Config ParseLINQBased(string[] args)
    {
        var config = new Config
        {
            Method = HttpMethod.Get,
            Concurrency = 1,
            Headers = null
        };

        var options = args
            .Where((arg, index) => index % 2 == 0 && arg.StartsWith("--"))
            .Select((arg, index) => new { Key = arg.Substring(2), Value = args[index * 2 + 1] })
            .ToList();

        foreach (var option in options)
        {
            switch (option.Key)
            {
                case "url":
                    config.Url = option.Value;
                    break;
                case "method":
                    config.Method = ParseMethodString(option.Value);
                    break;
                case "concurrency":
                    if (!ushort.TryParse(option.Value, out var c) || c <= 0)
                        throw new ArgumentException($"Invalid concurrency: {option.Value}");
                    config.Concurrency = c;
                    break;
                case "duration":
                    if (!ushort.TryParse(option.Value, out var d) || d <= 0)
                        throw new ArgumentException($"Invalid duration: {option.Value}");
                    config.DurationSeconds = d;
                    break;
                case "requests":
                    if (!ushort.TryParse(option.Value, out var r) || r <= 0)
                        throw new ArgumentException($"Invalid requests: {option.Value}");
                    config.RequestCount = r;
                    break;
                case "body":
                    config.Body = option.Value;
                    break;
                case "headers":
                    config.Headers ??= new Dictionary<string, string>();
                    ParseHeadersLINQ(option.Value, config.Headers);
                    break;
                default:
                    throw new ArgumentException($"Unknown option: {option.Key}");
            }
        }

        return config;
    }

    #endregion

    #region ParseMethod Alternatives

    /// <summary>
    /// String-based method parsing
    /// </summary>
    public static HttpMethod ParseMethodString(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            _ => throw new ArgumentException($"Unsupported HTTP method: {method}")
        };
    }

    /// <summary>
    /// Switch-based method parsing
    /// </summary>
    public static HttpMethod ParseMethodSwitch(string method)
    {
        switch (method.ToUpperInvariant())
        {
            case "GET":
                return HttpMethod.Get;
            case "POST":
                return HttpMethod.Post;
            case "PUT":
                return HttpMethod.Put;
            case "DELETE":
                return HttpMethod.Delete;
            default:
                throw new ArgumentException($"Unsupported HTTP method: {method}");
        }
    }

    #endregion

    #region ParseHeaders Alternatives

    /// <summary>
    /// String.Split based parsing
    /// </summary>
    public static void ParseHeadersSplit(string headerString, Dictionary<string, string> headers)
    {
        var pairs = headerString.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var parts = pair.Split(':', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    headers[key] = value;
                }
            }
        }
    }

    /// <summary>
    /// Regex-based parsing
    /// </summary>
    public static void ParseHeadersRegex(string headerString, Dictionary<string, string> headers)
    {
        var regex = new System.Text.RegularExpressions.Regex(@"(\w+(?:-\w+)*)\s*:\s*([^,;]+)", System.Text.RegularExpressions.RegexOptions.Compiled);
        var matches = regex.Matches(headerString);

        foreach (System.Text.RegularExpressions.Match match in matches)
        {
            if (match.Groups.Count == 3)
            {
                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value.Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    headers[key] = value;
                }
            }
        }
    }

    /// <summary>
    /// LINQ-based parsing
    /// </summary>
    public static void ParseHeadersLINQ(string headerString, Dictionary<string, string> headers)
    {
        headerString
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .Select(parts => new { Key = parts[0].Trim(), Value = parts[1].Trim() })
            .Where(kv => !string.IsNullOrEmpty(kv.Key))
            .ToList()
            .ForEach(kv => headers[kv.Key] = kv.Value);
    }

    #endregion
}

