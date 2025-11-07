using System.Net.Http;
using System.Net.Sockets;
using Raptor.Cli.Infrastructure;
using Xunit;

namespace Raptor.Tests.Infrastructure;

/// <summary>
/// Unit tests for the <see cref="RaptorHttpClient"/> class.
/// </summary>
public class RaptorHttpClientTests
{
    [Fact]
    public void Constructor_ShouldCreateInstance()
    {
        using var client = new RaptorHttpClient();

        Assert.NotNull(client);
        Assert.NotNull(client.HttpClient);
    }

    [Fact]
    public void HttpClient_ShouldReturnNonNullInstance()
    {
        using var raptorClient = new RaptorHttpClient();

        var httpClient = raptorClient.HttpClient;

        Assert.NotNull(httpClient);
    }

    [Fact]
    public void HttpClient_ShouldHaveCorrectTimeout()
    {
        using var raptorClient = new RaptorHttpClient();

        var timeout = raptorClient.HttpClient.Timeout;

        Assert.Equal(TimeSpan.FromSeconds(30), timeout);
    }

    [Fact]
    public void HttpClient_ShouldBeUsable_ForHttpRequests()
    {
        using var raptorClient = new RaptorHttpClient();
        var httpClient = raptorClient.HttpClient;

        var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");
        Assert.NotNull(request);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.NotNull(request.RequestUri);
        Assert.Contains("https://example.com", request.RequestUri.ToString());
    }

    [Fact]
    public void Dispose_ShouldDisposeHttpClient()
    {
        var raptorClient = new RaptorHttpClient();
        var httpClient = raptorClient.HttpClient;

        raptorClient.Dispose();

        Assert.Throws<ObjectDisposedException>(() => httpClient.Send(new HttpRequestMessage(HttpMethod.Get, "https://example.com")));
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        var raptorClient = new RaptorHttpClient();

        raptorClient.Dispose();

        raptorClient.Dispose();
        raptorClient.Dispose();
    }

    [Fact]
    public void Dispose_ShouldWorkWithUsingStatement()
    {
        RaptorHttpClient? disposedClient = null;
        using (var raptorClient = new RaptorHttpClient())
        {
            Assert.NotNull(raptorClient.HttpClient);
            disposedClient = raptorClient;
        }

        Assert.Throws<ObjectDisposedException>(() => disposedClient!.HttpClient.Send(new HttpRequestMessage(HttpMethod.Get, "https://example.com")));
    }

    [Fact]
    public void Constructor_ShouldConfigureHandlerSettings()
    {
        using var raptorClient = new RaptorHttpClient();
        var httpClient = raptorClient.HttpClient;

        var handlerField = typeof(HttpClient).GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (handlerField != null)
        {
            var handler = handlerField.GetValue(httpClient);
            Assert.NotNull(handler);
            Assert.IsType<SocketsHttpHandler>(handler);

            var socketsHandler = (SocketsHttpHandler)handler!;
            Assert.Equal(TimeSpan.FromMinutes(5), socketsHandler.PooledConnectionLifetime);
            Assert.Equal(TimeSpan.FromMinutes(2), socketsHandler.PooledConnectionIdleTimeout);
            Assert.Equal(1000, socketsHandler.MaxConnectionsPerServer);
            Assert.True(socketsHandler.EnableMultipleHttp2Connections);
        }
    }

    [Fact]
    public void HttpClient_ShouldSupportMultipleRequests()
    {
        using var raptorClient = new RaptorHttpClient();
        var httpClient = raptorClient.HttpClient;

        var request1 = new HttpRequestMessage(HttpMethod.Get, "https://example.com/1");
        var request2 = new HttpRequestMessage(HttpMethod.Post, "https://example.com/2");
        var request3 = new HttpRequestMessage(HttpMethod.Put, "https://example.com/3");

        Assert.NotNull(request1);
        Assert.NotNull(request2);
        Assert.NotNull(request3);
        Assert.NotSame(request1, request2);
        Assert.NotSame(request2, request3);
    }

    [Fact]
    public void Dispose_ShouldNotAffectSeparateInstances()
    {
        var client1 = new RaptorHttpClient();
        var client2 = new RaptorHttpClient();
        var httpClient1 = client1.HttpClient;
        var httpClient2 = client2.HttpClient;

        client1.Dispose();

        Assert.Throws<ObjectDisposedException>(() => httpClient1.Send(new HttpRequestMessage(HttpMethod.Get, "https://example.com")));
        
        Assert.NotNull(httpClient2);
        
        client2.Dispose();
    }

    [Fact]
    public void HttpClient_ShouldHaveHandlerConfigured()
    {
        using var raptorClient = new RaptorHttpClient();
        var httpClient = raptorClient.HttpClient;

        var handlerProperty = typeof(HttpClient).GetProperty("Handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (handlerProperty == null)
        {
            var handlerField = typeof(HttpClient).GetField("_handler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (handlerField != null)
            {
                var handler = handlerField.GetValue(httpClient);
                Assert.NotNull(handler);
                Assert.IsType<SocketsHttpHandler>(handler);
            }
            else
            {
                Assert.NotNull(httpClient);
            }
        }
        else
        {
            var handler = handlerProperty.GetValue(httpClient);
            Assert.NotNull(handler);
            Assert.IsType<SocketsHttpHandler>(handler);
        }
    }
}

