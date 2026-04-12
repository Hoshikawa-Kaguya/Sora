using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for <see cref="BotConnection" />.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class BotConnectionTests
{
    /// <see cref="BotConnection" />
    [Fact]
    public void BotConnection_AllProperties()
    {
        Guid connId = Guid.NewGuid();
        BotConnection conn = new()
            {
                ConnectionId = connId,
                State        = ConnectionState.Connected
            };
        Assert.Equal(connId, conn.ConnectionId);
        Assert.Equal(ConnectionState.Connected, conn.State);
    }

    /// <see cref="BotConnection.State" />
    [Fact]
    public void BotConnection_DefaultState_IsIdle()
    {
        BotConnection conn = new() { ConnectionId = Guid.NewGuid() };
        Assert.Equal(ConnectionState.Idle, conn.State);
    }
}