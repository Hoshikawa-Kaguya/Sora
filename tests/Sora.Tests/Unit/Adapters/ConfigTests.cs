using Xunit;

namespace Sora.Tests.Unit.Adapters;

/// <summary>Tests for <see cref="OneBot11Config" /> and <see cref="MilkyConfig" />.</summary>
[Collection("Adapters.Unit")]
[Trait("Category", "Unit")]
public class ConfigTests
{
#region MilkyConfig Tests

    /// <summary>Verifies default property values for <see cref="MilkyConfig" />.</summary>
    [Fact]
    public void MilkyConfig_Defaults()
    {
        MilkyConfig config = new();
        Assert.Equal("127.0.0.1", config.Host);
        Assert.Equal(3000, config.Port);
        Assert.Equal("", config.Prefix);
        Assert.Equal(EventTransport.WebSocket, config.EventTransport);
        Assert.True(config.EnableCommandManager);
    }

    /// <summary>Verifies <see cref="MilkyConfig.GetApiBaseUrl" /> with no prefix.</summary>
    [Fact]
    public void MilkyConfig_GetApiBaseUrl_NoPrefix()
    {
        MilkyConfig config = new() { Host = "localhost", Port = 8080, Prefix = "" };
        Assert.Equal("http://localhost:8080/api", config.GetApiBaseUrl());
    }

    /// <summary>Verifies <see cref="MilkyConfig.GetApiBaseUrl" /> with a prefix.</summary>
    [Fact]
    public void MilkyConfig_GetApiBaseUrl_WithPrefix()
    {
        MilkyConfig config = new() { Host = "localhost", Port = 8080, Prefix = "milky" };
        Assert.Equal("http://localhost:8080/milky/api", config.GetApiBaseUrl());
    }

    /// <summary>Verifies <see cref="MilkyConfig.GetEventUrl" /> returns an HTTP URL.</summary>
    [Fact]
    public void MilkyConfig_GetEventUrl_Http()
    {
        MilkyConfig config = new() { Host = "localhost", Port = 8080, Prefix = "milky" };
        Assert.Equal("http://localhost:8080/milky/event", config.GetEventUrl());
    }

    /// <summary>Verifies <see cref="MilkyConfig.GetEventUrl" /> with no prefix.</summary>
    [Fact]
    public void MilkyConfig_GetEventUrl_NoPrefix()
    {
        MilkyConfig config = new() { Host = "localhost", Port = 8080, Prefix = "" };
        Assert.Equal("ws://localhost:8080/event", config.GetEventUrl(true));
    }

    /// <summary>Verifies <see cref="MilkyConfig.GetEventUrl" /> returns a WebSocket URL.</summary>
    [Fact]
    public void MilkyConfig_GetEventUrl_WebSocket()
    {
        MilkyConfig config = new() { Host = "localhost", Port = 8080, Prefix = "milky" };
        Assert.Equal("ws://localhost:8080/milky/event", config.GetEventUrl(true));
    }

#endregion

#region OneBot11Config Tests

    /// <summary>Verifies default property values for <see cref="OneBot11Config" />.</summary>
    [Fact]
    public void OneBot11Config_Defaults()
    {
        OneBot11Config config = new();
        Assert.Equal(ConnectionMode.ForwardWebSocket, config.Mode);
        Assert.Equal("127.0.0.1", config.Host);
        Assert.Equal(6700, config.Port);
        Assert.Equal("", config.AccessToken);
        Assert.True(config.EnableCommandManager);
        Assert.Empty(config.SuperUsers);
        Assert.Empty(config.BlockUsers);
    }

#endregion
}