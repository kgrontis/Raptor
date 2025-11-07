using System.Net.Http;

namespace Raptor.Cli.Infrastructure;

/// <summary>
/// Provides a configured, reusable HttpClient instance optimized for high-performance load testing.
/// Uses connection pooling and HTTP/2 support with appropriate timeouts and connection limits.
/// </summary>
internal sealed class RaptorHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="RaptorHttpClient"/> with optimized settings
    /// for load testing, including connection pooling, HTTP/2 support, and appropriate timeouts.
    /// </summary>
    public RaptorHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            MaxConnectionsPerServer = 1000,
            EnableMultipleHttp2Connections = true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    /// <summary>
    /// Gets the underlying <see cref="HttpClient"/> instance.
    /// </summary>
    public HttpClient HttpClient => _httpClient;

    /// <summary>
    /// Releases all resources used by the <see cref="RaptorHttpClient"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}

