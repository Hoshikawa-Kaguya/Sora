using Serilog.Core;
using Serilog.Extensions.Logging;
using Sora.Entities.Utils;
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
    private          int                           _failedTests;
    private          int                           _passedTests;
    private          int                           _totalTests;

    /// <summary>The connected primary <see cref="MilkyBotApi" /> instance (main test executor).</summary>
    public MilkyBotApi? PrimaryApi { get; private set; }

    /// <summary>The connected secondary <see cref="MilkyBotApi" /> instance (interaction simulator).</summary>
    public MilkyBotApi? SecondaryApi { get; private set; }

    /// <summary>The secondary bot's user ID, obtained at runtime via GetSelfInfoAsync.</summary>
    public UserId SecondaryUserId { get; private set; }

    /// <summary>Backward-compatible alias for <see cref="PrimaryApi" />.</summary>
    public MilkyBotApi? Api => PrimaryApi;

    /// <summary>Serilog sink that forwards log events to subscribed <c>ITestOutputHelper</c> instances.</summary>
    public TestOutputSink OutputSink { get; } = new();

    /// <summary>The active primary <see cref="SoraService" /> instance, if started.</summary>
    public SoraService? Service { get; private set; }

    /// <summary>The active secondary <see cref="SoraService" /> instance, if started.</summary>
    public SoraService? SecondaryService { get; private set; }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        TestTimingStore.StartTimer("Func", "Milky");
        if (TestConfig.SkipMilkyReason is not null) return;

        LogLevel? currentLogLevel = SysUtils.GetEnvLogLevelOverride();
        Logger serilogLogger = SoraService.CreateDefaultLoggerConfiguration(currentLogLevel ?? LogLevel.Debug)
                                          .WriteTo.Sink(OutputSink)
                                          .CreateLogger();
        ILoggerFactory factory = new SerilogLoggerFactory(serilogLogger, true);

        // ---- Primary Bot ----
        MilkyConfig primaryConfig = new()
            {
                Host           = TestConfig.MilkyPrimaryHost,
                Port           = TestConfig.MilkyPort,
                Prefix         = TestConfig.MilkyPrefix,
                AccessToken    = TestConfig.MilkyToken,
                EventTransport = EventTransport.WebSocket,
                ApiTimeout     = TimeSpan.FromSeconds(15),
                LoggerFactory  = factory
            };

        Service = SoraServiceFactory.Instance.CreateMilkyService(primaryConfig);
        Service.Events.OnConnected += e =>
        {
            _primaryReady.TrySetResult(e.Api);
            return ValueTask.CompletedTask;
        };

        try
        {
            await Service.StartAsync();
            await Task.WhenAny(_primaryReady.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            if (_primaryReady.Task.IsCompletedSuccessfully) PrimaryApi = await _primaryReady.Task as MilkyBotApi;
        }
        catch
        {
            // Primary unreachable — leave PrimaryApi null; tests will skip via "API not available" guard
        }

        // ---- Secondary Bot (only if configured) ----
        if (TestConfig.IsMilkyDualBotConfigured && PrimaryApi is not null)
        {
            Logger secondaryLogger = SoraService.CreateDefaultLoggerConfiguration(currentLogLevel ?? LogLevel.Debug)
                                                .WriteTo.Sink(OutputSink)
                                                .CreateLogger();
            ILoggerFactory secondaryFactory = new SerilogLoggerFactory(secondaryLogger, true);

            MilkyConfig secondaryConfig = new()
                {
                    Host           = TestConfig.MilkySecondaryHost,
                    Port           = TestConfig.MilkyPort,
                    Prefix         = TestConfig.MilkyPrefix,
                    AccessToken    = TestConfig.MilkyToken,
                    EventTransport = EventTransport.WebSocket,
                    ApiTimeout     = TimeSpan.FromSeconds(15),
                    LoggerFactory  = secondaryFactory
                };

            SecondaryService = SoraServiceFactory.Instance.CreateMilkyService(secondaryConfig);
            SecondaryService.Events.OnConnected += e =>
            {
                _secondaryReady.TrySetResult(e.Api);
                return ValueTask.CompletedTask;
            };

            try
            {
                await SecondaryService.StartAsync();
                await Task.WhenAny(_secondaryReady.Task, Task.Delay(TimeSpan.FromSeconds(10)));
                if (_secondaryReady.Task.IsCompletedSuccessfully)
                {
                    SecondaryApi = await _secondaryReady.Task as MilkyBotApi;

                    // Discover secondary bot's UserId at runtime
                    if (SecondaryApi is not null)
                    {
                        ApiResult<BotIdentity> selfInfo = await SecondaryApi.GetSelfInfoAsync();
                        if (selfInfo is { IsSuccess: true, Data: { } selfData }) SecondaryUserId = selfData.UserId;
                    }
                }
            }
            catch
            {
                // Secondary unreachable — leave SecondaryApi null; dual-bot tests will skip
            }
        }
    }

    /// <summary>Records a test result for reporting.</summary>
    public void RecordResult(bool passed)
    {
        Interlocked.Increment(ref _totalTests);
        if (passed)
            Interlocked.Increment(ref _passedTests);
        else
            Interlocked.Increment(ref _failedTests);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        TestTimingStore.StopTimer("Func", "Milky");
        if (SecondaryService is not null) await SecondaryService.DisposeAsync();
        if (Service is not null) await Service.DisposeAsync();
    }
}

/// <summary>Milky functional test collection.</summary>
[CollectionDefinition("Milky.Functional", DisableParallelization = true)]
public class MilkyFunctionalCollection : ICollectionFixture<MilkyTestFixture>
{
}