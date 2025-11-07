namespace Raptor.Cli.Core;

/// <summary>
/// Represents the result of a single HTTP request execution.
/// </summary>
internal readonly struct RequestResult(long timestampNs, int statusCode, bool isError, long durationMs)
{
    /// <summary>
    /// Gets the timestamp in nanoseconds when the request was recorded.
    /// </summary>
    public readonly long TimestampNs = timestampNs;

    /// <summary>
    /// Gets the HTTP status code of the response (0 if the request failed).
    /// </summary>
    public readonly int StatusCode = statusCode;

    /// <summary>
    /// Gets a value indicating whether the request resulted in an error (status code >= 400 or request failed).
    /// </summary>
    public readonly bool IsError = isError;

    /// <summary>
    /// Gets the duration of the request in milliseconds.
    /// </summary>
    public readonly long DurationMs = durationMs;
}

