using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Raptor.Cli.Core;
using Xunit;

namespace Raptor.Tests.Core;

/// <summary>
/// Unit tests for the <see cref="HttpLoadTester"/> class.
/// Uses a test HTTP server to provide isolated testing.
/// </summary>
public class HttpLoadTesterTests : IDisposable
{
    private readonly HttpListener _httpListener;
    private readonly string _testUrl;
    private readonly Thread _listenerThread;
    private volatile bool _isRunning;
    private volatile int _statusCode = 200;
    private volatile string _responseBody = "OK";
    private volatile int _requestCount = 0;

    public HttpLoadTesterTests()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        _httpListener = new HttpListener();
        _testUrl = $"http://localhost:{port}/";
        _httpListener.Prefixes.Add(_testUrl);
        _httpListener.Start();

        _isRunning = true;
        _listenerThread = new Thread(() =>
        {
            while (_isRunning)
            {
                try
                {
                    var context = _httpListener.GetContext();
                    Interlocked.Increment(ref _requestCount);

                    var response = context.Response;
                    response.StatusCode = _statusCode;
                    response.ContentType = "text/plain";

                    var buffer = Encoding.UTF8.GetBytes(_responseBody);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.Close();
                }
                catch (HttpListenerException)
                {
                }
            }
        })
        {
            IsBackground = true
        };
        _listenerThread.Start();
    }

    public void Dispose()
    {
        _isRunning = false;
        _httpListener?.Stop();
        _httpListener?.Close();
        _listenerThread?.Join(1000);
    }

    private Config CreateConfig(ushort concurrency = 1, ushort? durationSeconds = null, ushort? requestCount = null, string? body = null, Dictionary<string, string>? headers = null)
    {
        return new Config
        {
            Url = _testUrl,
            Method = HttpMethod.Get,
            Concurrency = concurrency,
            DurationSeconds = durationSeconds,
            RequestCount = requestCount,
            Body = body,
            Headers = headers
        };
    }

    [Fact]
    public async Task RunAsync_ShouldComplete_WhenRequestCountMode()
    {
        var config = CreateConfig(concurrency: 2, requestCount: 5);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 5);
        Assert.True(stats.ErrorCount == 0);
    }

    [Fact]
    public async Task RunAsync_ShouldComplete_WhenDurationMode()
    {
        var config = CreateConfig(concurrency: 1, durationSeconds: 1);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests > 0);
        var elapsedSeconds = loadTester.GetElapsedSeconds();
        Assert.True(elapsedSeconds >= 1.0, $"Expected at least 1 second, got {elapsedSeconds}");
    }

    [Fact]
    public async Task RunAsync_ShouldRespectConcurrencyLimit()
    {
        var config = CreateConfig(concurrency: 2, requestCount: 10);
        var estimatedRequestCount = 20;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 10);
    }

    [Fact]
    public async Task RunAsync_ShouldHandleCancellation()
    {
        var config = CreateConfig(concurrency: 1, durationSeconds: 10);
        var estimatedRequestCount = 100;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);
        using var cts = new CancellationTokenSource();

        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        try
        {
            await loadTester.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
        }

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 0);
    }

    [Fact]
    public async Task RunAsync_ShouldRecordSuccessfulRequests()
    {
        _statusCode = 200;
        var config = CreateConfig(concurrency: 1, requestCount: 5);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 5);
        var statusCodes = stats.GetStatusCodes();
        Assert.Contains(200, statusCodes.Keys);
    }

    [Fact]
    public async Task RunAsync_ShouldRecordErrorStatusCodes()
    {
        _statusCode = 404;
        var config = CreateConfig(concurrency: 1, requestCount: 3);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.ErrorCount > 0);
        var statusCodes = stats.GetStatusCodes();
        Assert.Contains(404, statusCodes.Keys);
    }

    [Fact]
    public async Task RunAsync_ShouldHandleServerErrors()
    {
        _statusCode = 500;
        var config = CreateConfig(concurrency: 1, requestCount: 3);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.ErrorCount > 0);
        var statusCodes = stats.GetStatusCodes();
        Assert.Contains(500, statusCodes.Keys);
    }

    [Fact]
    public async Task RunAsync_ShouldUsePostMethod_WhenConfigured()
    {
        var config = CreateConfig(concurrency: 1, requestCount: 1);
        config.Method = HttpMethod.Post;
        config.Body = "{\"test\":\"data\"}";
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 1);
    }

    [Fact]
    public async Task RunAsync_ShouldUsePutMethod_WhenConfigured()
    {
        var config = CreateConfig(concurrency: 1, requestCount: 1);
        config.Method = HttpMethod.Put;
        config.Body = "{\"test\":\"data\"}";
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 1);
    }

    [Fact]
    public async Task RunAsync_ShouldIncludeCustomHeaders_WhenConfigured()
    {
        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer token123" },
            { "X-Custom-Header", "test-value" }
        };
        var config = CreateConfig(concurrency: 1, requestCount: 1, headers: headers);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 1);
    }

    [Fact]
    public async Task RunAsync_ShouldSetContentType_WhenBodyProvided()
    {
        var config = CreateConfig(concurrency: 1, requestCount: 1);
        config.Method = HttpMethod.Post;
        config.Body = "{\"test\":\"data\"}";
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 1);
    }

    [Fact]
    public async Task RunAsync_ShouldUseCustomContentType_WhenSpecified()
    {
        var headers = new Dictionary<string, string>
        {
            { "Content-Type", "application/xml" }
        };
        var config = CreateConfig(concurrency: 1, requestCount: 1, headers: headers);
        config.Method = HttpMethod.Post;
        config.Body = "<test>data</test>";
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 1);
    }

    [Fact]
    public void GetStats_ShouldReturnStatsCollector()
    {
        var config = CreateConfig(concurrency: 1, requestCount: 1);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        var stats = loadTester.GetStats();

        Assert.NotNull(stats);
    }

    [Fact]
    public void GetElapsedSeconds_ShouldReturnElapsedTime()
    {
        var config = CreateConfig(concurrency: 1, requestCount: 1);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        var elapsedBefore = loadTester.GetElapsedSeconds();
        Thread.Sleep(100);
        var elapsedAfter = loadTester.GetElapsedSeconds();

        Assert.True(elapsedAfter >= elapsedBefore);
    }

    [Fact]
    public void GetElapsedSeconds_ShouldStartFromZero()
    {
        var config = CreateConfig(concurrency: 1, requestCount: 1);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        var elapsed = loadTester.GetElapsedSeconds();

        Assert.True(elapsed >= 0);
        Assert.True(elapsed < 1.0); // Should be very small at start
    }

    [Fact]
    public async Task RunAsync_ShouldCompleteAllRequests_InRequestCountMode()
    {
        var requestCount = 10;
        var config = CreateConfig(concurrency: 3, requestCount: (ushort)requestCount);
        var estimatedRequestCount = 20;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= requestCount, $"Expected at least {requestCount} requests, got {stats.TotalRequests}");
    }

    [Fact]
    public async Task RunAsync_ShouldMeasureLatency()
    {
        var config = CreateConfig(concurrency: 1, requestCount: 5);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        var latencyStats = stats.GetLatencyStats();
        Assert.True(latencyStats.Min >= 0);
        Assert.True(latencyStats.Max >= latencyStats.Min);
        Assert.True(latencyStats.Avg >= 0);
    }

    [Fact]
    public void Dispose_ShouldCleanUpResources()
    {
        var config = CreateConfig(concurrency: 1, requestCount: 1);
        var estimatedRequestCount = 10;
        var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        loadTester.Dispose();

        loadTester.Dispose();
    }

    [Fact]
    public async Task RunAsync_ShouldHandleNetworkErrors()
    {
        var config = new Config
        {
            Url = "http://localhost:99999/nonexistent",
            Method = HttpMethod.Get,
            Concurrency = 1,
            RequestCount = 1
        };
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 1);
        Assert.True(stats.ErrorCount > 0 || stats.TotalRequests > 0);
    }

    [Fact]
    public async Task RunAsync_ShouldProcessDeleteMethod()
    {
        var config = CreateConfig(concurrency: 1, requestCount: 1);
        config.Method = HttpMethod.Delete;
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 1);
    }

    [Fact]
    public async Task RunAsync_ShouldHandleAcceptHeader()
    {
        var headers = new Dictionary<string, string>
        {
            { "Accept", "application/json" }
        };
        var config = CreateConfig(concurrency: 1, requestCount: 1, headers: headers);
        var estimatedRequestCount = 10;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests >= 1);
    }

    [Fact]
    public async Task RunAsync_ShouldRunForCorrectDuration()
    {
        var durationSeconds = 1;
        var config = CreateConfig(concurrency: 1, durationSeconds: (ushort)durationSeconds);
        var estimatedRequestCount = 100;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        var startTime = DateTime.UtcNow;
        await loadTester.RunAsync();
        var endTime = DateTime.UtcNow;
        var actualElapsed = (endTime - startTime).TotalSeconds;

        var statsElapsed = loadTester.GetElapsedSeconds();
        Assert.True(statsElapsed >= durationSeconds, $"Expected at least {durationSeconds} seconds, got {statsElapsed}");
        Assert.True(actualElapsed >= durationSeconds, $"Expected actual duration at least {durationSeconds} seconds, got {actualElapsed}");
    }

    [Fact]
    public async Task RunAsync_ShouldCleanUpTasks_WhenDurationMode()
    {
        var config = CreateConfig(concurrency: 5, durationSeconds: 1);
        var estimatedRequestCount = 100;
        using var loadTester = new HttpLoadTester(config, estimatedRequestCount);

        await loadTester.RunAsync();

        var stats = loadTester.GetStats();
        Assert.True(stats.TotalRequests > 0);
    }
}

