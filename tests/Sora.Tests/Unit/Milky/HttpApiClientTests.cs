using System.Net;
using System.Net.Sockets;
using System.Text;
using Sora.Adapter.Milky.Models;
using Sora.Adapter.Milky.Net;
using Xunit;

namespace Sora.Tests.Unit.Milky;

/// <summary>Tests preservation of server error details in the HTTP response envelope.</summary>
[Collection("Milky.Unit")]
[Trait("Category", "Unit")]
public class HttpApiClientTests
{
    /// <summary>A nonzero retcode preserves both the explanation and structured error payload.</summary>
    [Fact]
    public async Task NonzeroRetCode_PreservesMessageAndData()
    {
        using TcpListener server = new(IPAddress.Loopback, 0);
        server.Start();
        using CancellationTokenSource deadline = new(TimeSpan.FromSeconds(5));
        using MilkyHttpApiClient      client = new(new MilkyConfig { Port = ((IPEndPoint)server.LocalEndpoint).Port });
        Task<MilkyApiResponse>        call = client.CallApiAsync("test", ct: deadline.Token).AsTask();
        using TcpClient               peer = await server.AcceptTcpClientAsync(deadline.Token);
        using StreamReader            reader = new(peer.GetStream(), leaveOpen: true);
        while (await reader.ReadLineAsync(deadline.Token) is { Length: > 0 })
        {
        }

        const string body =
            "{\"status\":\"ok\",\"retcode\":123,\"message\":\"details\",\"data\":{\"reason\":\"restricted\"}}";
        string response =
            $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {Encoding.UTF8.GetByteCount(body)}\r\nConnection: close\r\n\r\n{body}";
        await peer.GetStream().WriteAsync(Encoding.UTF8.GetBytes(response), deadline.Token);

        MilkyApiResponse result = await call.WaitAsync(deadline.Token);
        Assert.Equal("failed", result.Status);
        Assert.Equal(123, result.RetCode);
        Assert.Equal("details", result.Message);
        Assert.Equal("restricted", (string?)result.Data?["reason"]);
    }
}