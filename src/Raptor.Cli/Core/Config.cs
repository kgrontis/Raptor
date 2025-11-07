namespace Raptor.Cli.Core;

/// <summary>
/// Represents the parsed configuration for a load test run.
/// </summary>
public struct Config
{
    /// <summary>
    /// Gets or sets the target URL to load test.
    /// </summary>
    public string Url;

    /// <summary>
    /// Gets or sets the HTTP method to use for requests (default: GET).
    /// </summary>
    public HttpMethod Method;

    /// <summary>
    /// Gets or sets the number of concurrent requests to execute.
    /// </summary>
    public ushort Concurrency;

    /// <summary>
    /// Gets or sets the duration in seconds to run the load test.
    /// Mutually exclusive with <see cref="RequestCount"/>.
    /// </summary>
    public ushort? DurationSeconds;

    /// <summary>
    /// Gets or sets the total number of requests to execute.
    /// Mutually exclusive with <see cref="DurationSeconds"/>.
    /// </summary>
    public ushort? RequestCount;

    /// <summary>
    /// Gets or sets the request body content (typically JSON for POST/PUT requests).
    /// </summary>
    public string? Body;

    /// <summary>
    /// Gets or sets custom HTTP headers to include in requests.
    /// </summary>
    public Dictionary<string, string>? Headers;

    /// <summary>
    /// Gets a value indicating whether the configuration is valid.
    /// Valid configuration requires: a non-empty URL, concurrency greater than 0,
    /// and exactly one of DurationSeconds or RequestCount (not both).
    /// </summary>
    public readonly bool IsValid => !string.IsNullOrEmpty(Url) && Concurrency > 0 &&
        (DurationSeconds.HasValue || RequestCount.HasValue) &&
        !(DurationSeconds.HasValue && RequestCount.HasValue);
}

