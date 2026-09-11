using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Sora.Adapter.Milky.Net;
using Xunit;

namespace Sora.Tests.Unit.Milky;

/// <summary>Exercises event transport ownership and retries against a local server without bot accounts.</summary>
[Collection("Milky.Unit")]
[Trait("Category", "Unit")]
public class EventClientLifecycleTests
{
#region Connection Lifetime

    /// <summary>Startup returns while the stream is open; either lifetime cancellation path releases it.</summary>
    [Theory]
    [InlineData(EventTransport.WebSocket, false)]
    [InlineData(EventTransport.WebSocket, true)]
    [InlineData(EventTransport.Sse, false)]
    [InlineData(EventTransport.Sse, true)]
    public async Task StartAndStop_OwnActiveStream(EventTransport transport, bool cancelCaller)
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        using CancellationTokenSource lifetime = new();
        MilkyConfig config = new()
        {
            Port              = ((IPEndPoint)server.LocalEndpoint).Port,
            ReconnectInterval = TimeSpan.FromSeconds(30)
        };
        TaskCompletionSource         connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        MilkyWsEventClient           wsClient  = new(config);
        MilkySseEventClient          sseClient = new(config);
        await using IAsyncDisposable client    = transport == EventTransport.WebSocket ? wsClient : sseClient;
        wsClient.OnConnected  += () => connected.TrySetResult();
        sseClient.OnConnected += () => connected.TrySetResult();
        Func<ValueTask> disconnect = transport == EventTransport.WebSocket
            ? wsClient.DisconnectAsync
            : sseClient.DisconnectAsync;

        Task start = (transport == EventTransport.WebSocket
            ? wsClient.ConnectAsync(lifetime.Token)
            : sseClient.ConnectAsync(lifetime.Token)).AsTask();
        await Task.WhenAny(start, Task.Delay(Timeout.Infinite, deadline.Token));
        Assert.True(start.IsCompletedSuccessfully, "Startup must return before the server completes its handshake.");

        using TcpClient    peer         = await server.AcceptTcpClientAsync(deadline.Token);
        NetworkStream      stream       = peer.GetStream();
        using StreamReader reader       = new(stream, leaveOpen: true);
        string?            webSocketKey = null;
        while (await reader.ReadLineAsync(deadline.Token) is { Length: > 0 } line)
            if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
                webSocketKey = line[18..].Trim();

        string response;
        if (transport == EventTransport.WebSocket)
        {
            Assert.NotNull(webSocketKey);
            // The WebSocket handshake requires SHA-1 of the client key and protocol GUID.
            string accept = Convert.ToBase64String(
                SHA1.HashData(Encoding.ASCII.GetBytes(webSocketKey + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
            response =
                $"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {accept}\r\n\r\n";
        }
        else
        {
            response = "HTTP/1.1 200 OK\r\nContent-Type: text/event-stream\r\nConnection: close\r\n\r\n";
        }

        await stream.WriteAsync(Encoding.ASCII.GetBytes(response), deadline.Token);
        await Task.WhenAny(connected.Task, Task.Delay(Timeout.Infinite, deadline.Token));
        Assert.True(connected.Task.IsCompletedSuccessfully);

        if (cancelCaller) await lifetime.CancelAsync();
        else await disconnect().AsTask().WaitAsync(deadline.Token);

        byte[] buffer = new byte[1];
        Assert.Equal(0, await stream.ReadAsync(buffer, deadline.Token));
        await disconnect().AsTask().WaitAsync(deadline.Token);
        Assert.False(server.Pending());
    }

#endregion

#region Reconnection

    /// <summary>Failed connections retry at the configured interval, while zero disables further attempts.</summary>
    [Theory]
    [InlineData(EventTransport.WebSocket, 0)]
    [InlineData(EventTransport.WebSocket, 300)]
    [InlineData(EventTransport.Sse, 0)]
    [InlineData(EventTransport.Sse, 300)]
    public async Task FailedConnection_UsesReconnectInterval(EventTransport transport, int intervalMilliseconds)
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        MilkyConfig config = new()
        {
            Port              = ((IPEndPoint)server.LocalEndpoint).Port,
            ReconnectInterval = TimeSpan.FromMilliseconds(intervalMilliseconds)
        };
        TaskCompletionSource         disconnected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        MilkyWsEventClient           wsClient     = new(config);
        MilkySseEventClient          sseClient    = new(config);
        await using IAsyncDisposable client       = transport == EventTransport.WebSocket ? wsClient : sseClient;
        wsClient.OnDisconnected  += _ => disconnected.TrySetResult();
        sseClient.OnDisconnected += _ => disconnected.TrySetResult();

        await (transport == EventTransport.WebSocket
            ? wsClient.ConnectAsync(deadline.Token)
            : sseClient.ConnectAsync(deadline.Token));
        using TcpClient    peer   = await server.AcceptTcpClientAsync(deadline.Token);
        using StreamReader reader = new(peer.GetStream(), leaveOpen: true);
        while (await reader.ReadLineAsync(deadline.Token) is { Length: > 0 })
        {
        }

        Stopwatch elapsed = Stopwatch.StartNew();
        await peer.GetStream().WriteAsync(
            "HTTP/1.1 503 Service Unavailable\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"u8.ToArray(),
            deadline.Token);
        await Task.WhenAny(disconnected.Task, Task.Delay(Timeout.Infinite, deadline.Token));
        Assert.True(disconnected.Task.IsCompletedSuccessfully);

        if (intervalMilliseconds == 0)
        {
            await Task.Delay(300, deadline.Token);
            Assert.False(server.Pending(), "A zero reconnect interval must not open another connection.");
        }
        else
        {
            using TcpClient retry = await server.AcceptTcpClientAsync(deadline.Token);
            Assert.True(
                elapsed.ElapsedMilliseconds >= intervalMilliseconds - 20,
                $"Reconnect arrived after {elapsed.ElapsedMilliseconds} ms.");
        }
    }

#endregion
}