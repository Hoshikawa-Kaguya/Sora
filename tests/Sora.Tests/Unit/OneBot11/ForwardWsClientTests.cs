using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Sora.Adapter.OneBot11.Net;
using Xunit;

namespace Sora.Tests.Unit.OneBot11;

/// <summary>Verifies that failed forward WebSocket connections use the configured retry interval.</summary>
[Collection("OneBot11.Unit")]
[Trait("Category", "Unit")]
public class ForwardWsClientTests
{
    /// <summary>A rejected handshake retries using ReconnectInterval instead of the library default.</summary>
    [Fact]
    public async Task FailedConnection_UsesReconnectInterval()
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        await using ForwardWsClient client = new(
            new OneBot11Config
            {
                Port              = ((IPEndPoint)server.LocalEndpoint).Port,
                ReconnectInterval = TimeSpan.FromMilliseconds(300)
            });
        Task               start  = client.ConnectAsync(deadline.Token).AsTask();
        using TcpClient    peer   = await server.AcceptTcpClientAsync(deadline.Token);
        using StreamReader reader = new(peer.GetStream(), leaveOpen: true);
        while (await reader.ReadLineAsync(deadline.Token) is { Length: > 0 })
        {
        }

        Stopwatch elapsed = Stopwatch.StartNew();
        await peer.GetStream().WriteAsync(
            "HTTP/1.1 503 Service Unavailable\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(),
            deadline.Token);

        using TcpClient retry = await server.AcceptTcpClientAsync(deadline.Token);
        Assert.True(elapsed.ElapsedMilliseconds >= 280, $"Reconnect arrived after {elapsed.ElapsedMilliseconds} ms.");
        await start.WaitAsync(deadline.Token);
    }
}