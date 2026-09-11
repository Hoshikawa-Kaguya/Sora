using Xunit;

namespace Sora.Tests.Functional.Milky;

/// <summary>
///     Shared fixture for all Milky functional tests.
///     Manages dual-bot connections: Primary (main test executor) and Secondary (interaction simulator / data validator).
/// </summary>
public sealed class MilkyTestFixture : IAsyncLifetime
{
    private readonly TaskCompletionSource<IBotApi> _primaryReady   = new();
    private readonly TaskCompletionSource<IBotApi> _secondaryReady = new();

    /// <summary>The connected primary <see cref="MilkyBotApi" /> instance (main test executor).</summary>
    public MilkyBotApi? PrimaryApi { get; private set; }

    /// <summary>The connected secondary <see cref="MilkyBotApi" /> instance (interaction simulator).</summary>
    public MilkyBotApi? SecondaryApi { get; private set; }

    /// <summary>The secondary bot's user ID, obtained at runtime via GetSelfInfoAsync.</summary>
    public UserId SecondaryUserId { get; private set; }

    /// <summary>Serilog sink that forwards log events to subscribed <c>ITestOutputHelper</c> instances.</summary>
    public TestOutputSink OutputSink => TestLogging.OutputSink;

    /// <summary>The active primary <see cref="SoraService" /> instance, if started.</summary>
    public SoraService? Service { get; private set; }

    /// <summary>The active secondary <see cref="SoraService" /> instance, if started.</summary>
    public SoraService? SecondaryService { get; private set; }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        if (TestConfig.SkipMilkyReason is not null) return;

        // ---- Primary Bot ----
        MilkyConfig primaryConfig = new()
        {
            Host              = TestConfig.MilkyPrimaryHost,
            Port              = TestConfig.MilkyPrimaryPort,
            Prefix            = TestConfig.MilkyPrefix,
            AccessToken       = TestConfig.MilkyToken,
            EventTransport    = EventTransport.WebSocket,
            ReconnectInterval = TimeSpan.Zero,
            ApiTimeout        = TimeSpan.FromSeconds(15)
        };

        Service = SoraServiceFactory.Instance.CreateMilkyService(primaryConfig);
        Service.Events.OnConnected += e =>
        {
            _primaryReady.TrySetResult(e.Api);
            return ValueTask.CompletedTask;
        };

        await Service.StartAsync();
        await Task.WhenAny(_primaryReady.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.True(
            _primaryReady.Task.IsCompletedSuccessfully,
            "Configured primary Milky connection did not become ready.");
        PrimaryApi = Assert.IsType<MilkyBotApi>(await _primaryReady.Task);

        // ---- Secondary Bot (only if configured) ----
        if (TestConfig.IsMilkyDualBotConfigured && PrimaryApi is not null)
        {
            MilkyConfig secondaryConfig = new()
            {
                Host              = TestConfig.MilkySecondaryHost,
                Port              = TestConfig.MilkySecondaryPort,
                Prefix            = TestConfig.MilkyPrefix,
                AccessToken       = TestConfig.MilkyToken,
                EventTransport    = EventTransport.WebSocket,
                ReconnectInterval = TimeSpan.Zero,
                ApiTimeout        = TimeSpan.FromSeconds(15)
            };

            SecondaryService = SoraServiceFactory.Instance.CreateMilkyService(secondaryConfig);
            SecondaryService.Events.OnConnected += e =>
            {
                _secondaryReady.TrySetResult(e.Api);
                return ValueTask.CompletedTask;
            };

            await SecondaryService.StartAsync();
            await Task.WhenAny(_secondaryReady.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.True(
                _secondaryReady.Task.IsCompletedSuccessfully,
                "Configured secondary Milky connection did not become ready.");
            SecondaryApi = Assert.IsType<MilkyBotApi>(await _secondaryReady.Task);

            ApiResult<BotIdentity> selfInfo = await SecondaryApi.GetSelfInfoAsync();
            SecondaryUserId = selfInfo.AssertSuccess().UserId;
            Assert.True(SecondaryUserId.Value > 0);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (SecondaryService is not null) await SecondaryService.DisposeAsync();
        if (Service is not null) await Service.DisposeAsync();
    }
}

/// <summary>Milky functional test collection.</summary>
[CollectionDefinition("Milky.Functional", DisableParallelization = true)]
public class MilkyFunctionalCollection : ICollectionFixture<MilkyTestFixture>
{
}