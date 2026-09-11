using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for <see cref="BotConnection" />.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class BotConnectionTests
{
    /// <see cref="BotConnection.State" />
    [Fact]
    public void BotConnection_DefaultState_IsIdle()
    {
        BotConnection conn = new() { ConnectionId = Guid.NewGuid() };
        Assert.Equal(ConnectionState.Idle, conn.State);
    }
}