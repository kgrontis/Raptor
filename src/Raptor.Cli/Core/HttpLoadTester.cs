using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Raptor.Cli.Core;
using Raptor.Cli.Infrastructure;
using Raptor.Cli.Statistics;

namespace Raptor.Cli.Core;

/// <summary>
/// High-performance HTTP load tester using typed HttpClient, ArrayPool buffers, and zero-allocation hot paths.
/// </summary>
internal sealed class HttpLoadTester(Config config, int estimatedRequestCount) : IDisposable
{
    private readonly RaptorHttpClient _httpClient = new();
    private readonly Config _config = config;
    private readonly SemaphoreSlim _concurrencyLimiter = new(config.Concurrency, config.Concurrency);
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly StatsCollector _stats = new(estimatedRequestCount);

    /// <summary>
    /// Runs the load test based on the configured mode (duration or request count).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the load test operation.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_config.DurationSeconds.HasValue)
        {
            await RunDurationModeAsync(cancellationToken);
        }
        else if (_config.RequestCount.HasValue)
        {
            await RunRequestCountModeAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Runs the load test for a specified duration, continuously launching requests
    /// until the duration expires or cancellation is requested.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task RunDurationModeAsync(CancellationToken cancellationToken)
    {
        var durationSeconds = _config.DurationSeconds!.Value;
        var endTimeMs = _stopwatch.ElapsedMilliseconds + ((int)durationSeconds * 1000L);
        var tasks = new List<Task>(_config.Concurrency * 2);

        while (_stopwatch.ElapsedMilliseconds < endTimeMs && !cancellationToken.IsCancellationRequested)
        {
            await _concurrencyLimiter.WaitAsync(cancellationToken);

            var task = ExecuteRequestAsync(cancellationToken)
                .ContinueWith(t =>
                {
                    _concurrencyLimiter.Release();
                }, TaskScheduler.Default);
            tasks.Add(task);

            if (tasks.Count > _config.Concurrency * 10)
            {
                var writeIndex = 0;
                for (var readIndex = 0; readIndex < tasks.Count; readIndex++)
                {
                    if (!tasks[readIndex].IsCompleted)
                    {
                        if (writeIndex != readIndex)
                        {
                            tasks[writeIndex] = tasks[readIndex];
                        }
                        writeIndex++;
                    }
                }
                tasks.RemoveRange(writeIndex, tasks.Count - writeIndex);
            }
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Runs the load test until a specified number of requests have been completed.
    /// Launches requests up to the concurrency limit and waits for all to complete.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    private async Task RunRequestCountModeAsync(CancellationToken cancellationToken)
    {
        var requestCount = _config.RequestCount!.Value;
        var remaining = (int)requestCount;
        var tasks = new List<Task>(remaining);

        while (remaining > 0 && !cancellationToken.IsCancellationRequested)
        {
            await _concurrencyLimiter.WaitAsync(cancellationToken);
            remaining--;

            var task = ExecuteRequestAsync(cancellationToken)
                .ContinueWith(t =>
                {
                    _concurrencyLimiter.Release();
                }, TaskScheduler.Default);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes a single HTTP request and records the result in the statistics collector.
    /// Measures request duration and handles errors gracefully.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the request.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private async Task ExecuteRequestAsync(CancellationToken cancellationToken)
    {
        var startMs = _stopwatch.ElapsedMilliseconds;

        try
        {
            using var request = CreateRequest();
            using var response = await _httpClient.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            var statusCode = (int)response.StatusCode;
            var isError = statusCode >= 400;

            await response.Content.CopyToAsync(Stream.Null, cancellationToken);

            var endMs = _stopwatch.ElapsedMilliseconds;
            var durationMs = endMs - startMs;
            var timestampNs = Stopwatch.GetTimestamp();

            _stats.Record(timestampNs, statusCode, isError, durationMs);
        }
        catch
        {
            var endMs = _stopwatch.ElapsedMilliseconds;
            var durationMs = endMs - startMs;
            _stats.Record(Stopwatch.GetTimestamp(), 0, true, durationMs);
        }
    }

    /// <summary>
    /// Creates an HTTP request message configured with the method, URL, headers, and body.
    /// Handles special headers like Content-Type and Accept appropriately.
    /// </summary>
    /// <returns>A configured <see cref="HttpRequestMessage"/> ready to be sent.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private HttpRequestMessage CreateRequest()
    {
        var request = new HttpRequestMessage(_config.Method, _config.Url);
        var body = _config.Body;
        var hasBody = !string.IsNullOrEmpty(body) && (request.Method == HttpMethod.Post || request.Method == HttpMethod.Put);
        string? contentTypeValue = null;

        if (_config.Headers != null)
        {
            foreach (var header in _config.Headers)
            {
                if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    if (!hasBody)
                    {
                        if (request.Content != null)
                        {
                            request.Content.Headers.ContentType = new MediaTypeHeaderValue(header.Value);
                        }
                    }
                    else
                    {
                        contentTypeValue = header.Value;
                    }
                }
                else if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
                {
                    request.Headers.Accept.ParseAdd(header.Value);
                }
                else
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
        }

        if (hasBody)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body!);
            request.Content = new ByteArrayContent(bodyBytes);

            if (contentTypeValue != null)
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentTypeValue);
            }
            else
            {
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }
        }

        return request;
    }

    /// <summary>
    /// Gets the statistics collector containing all request results and metrics.
    /// </summary>
    /// <returns>The <see cref="StatsCollector"/> instance.</returns>
    public StatsCollector GetStats() => _stats;

    /// <summary>
    /// Gets the elapsed time since the load test started in seconds.
    /// </summary>
    /// <returns>The elapsed time in seconds.</returns>
    public double GetElapsedSeconds() => _stopwatch.Elapsed.TotalSeconds;

    public void Dispose()
    {
        _concurrencyLimiter?.Dispose();
        _httpClient?.Dispose();
    }
}

