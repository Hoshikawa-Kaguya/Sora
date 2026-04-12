using Serilog.Core;
using Serilog.Extensions.Logging;
using Sora.Entities.Utils;
using Xunit;

namespace Sora.Tests.Functional.OneBot11;

/// <summary>
///     Shared fixture for all OneBot11 functional tests.
///     Manages dual-bot connections: Primary (main test executor) and Secondary (interaction simulator / data validator).
/// </summary>
public sealed class OneBot11TestFixture : IAsyncLifetime
{
    private readonly TaskCompletionSource<IBotApi> _primaryReady   = new();
    private readonly TaskCompletionSource<IBotApi> _secondaryReady = new();
    private          int                           _failedTests;
    private          int                           _passedTests;
    private          int                           _totalTests;

    /// <summary>The connected primary <see cref="IBotApi" /> instance (main test executor).</summary>
    public IBotApi? PrimaryApi { get; private set; }

    /// <summary>The connected secondary <see cref="IBotApi" /> instance (interaction simulator).</summary>
    public IBotApi? SecondaryApi { get; private set; }

    /// <summary>The secondary bot's user ID, obtained at runtime via GetSelfInfoAsync.</summary>
    public UserId SecondaryUserId { get; private set; }

    /// <summary>Backward-compatible alias for <see cref="PrimaryApi" />.</summary>
    public IBotApi? Api => PrimaryApi;

    /// <summary>Serilog sink that forwards log events to subscribed <c>ITestOutputHelper</c> instances.</summary>
    public TestOutputSink OutputSink { get; } = new();

    /// <summary>The active primary <see cref="SoraService" /> instance, if started.</summary>
    public SoraService? Service { get; private set; }

    /// <summary>The active secondary <see cref="SoraService" /> instance, if started.</summary>
    public SoraService? SecondaryService { get; private set; }

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        TestTimingStore.StartTimer("Func", "OneBot11");
        if (TestConfig.SkipOb11Reason is not null) return;

        LogLevel currentLogLevel = SysUtils.GetEnvLogLevelOverride() ?? LogLevel.Debug;
        Logger serilogLogger = SoraService.CreateDefaultLoggerConfiguration(currentLogLevel)
                                          .WriteTo.Sink(OutputSink)
                                          .CreateLogger();
        ILoggerFactory factory = new SerilogLoggerFactory(serilogLogger, true);

        // ---- Primary Bot ----
        OneBot11Config primaryConfig = new()
            {
                Mode              = ConnectionMode.ForwardWebSocket,
                Host              = TestConfig.Ob11PrimaryHost,
                Port              = TestConfig.Ob11Port,
                AccessToken       = TestConfig.Ob11Token,
                HeartbeatInterval = TimeSpan.FromSeconds(5),
                ApiTimeout        = TimeSpan.FromSeconds(15),
                LoggerFactory     = factory
            };

        Service = SoraServiceFactory.Instance.CreateOneBot11Service(primaryConfig);
        Service.Events.OnConnected += e =>
        {
            _primaryReady.TrySetResult(e.Api);
            return ValueTask.CompletedTask;
        };

        try
        {
            await Service.StartAsync();
            await Task.WhenAny(_primaryReady.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            if (_primaryReady.Task.IsCompletedSuccessfully) PrimaryApi = await _primaryReady.Task;
        }
        catch
        {
            // Primary unreachable — leave PrimaryApi null; tests will skip via "API not available" guard
        }

        // ---- Secondary Bot (only if configured) ----
        if (TestConfig.IsOb11DualBotConfigured && PrimaryApi is not null)
        {
            Logger secondaryLogger = SoraService.CreateDefaultLoggerConfiguration(currentLogLevel)
                                                .WriteTo.Sink(OutputSink)
                                                .CreateLogger();
            ILoggerFactory secondaryFactory = new SerilogLoggerFactory(secondaryLogger, true);

            OneBot11Config secondaryConfig = new()
                {
                    Mode              = ConnectionMode.ForwardWebSocket,
                    Host              = TestConfig.Ob11SecondaryHost,
                    Port              = TestConfig.Ob11Port,
                    AccessToken       = TestConfig.Ob11Token,
                    HeartbeatInterval = TimeSpan.FromSeconds(5),
                    ApiTimeout        = TimeSpan.FromSeconds(15),
                    LoggerFactory     = secondaryFactory
                };

            SecondaryService = SoraServiceFactory.Instance.CreateOneBot11Service(secondaryConfig);
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
                    SecondaryApi = await _secondaryReady.Task;

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
        TestTimingStore.StopTimer("Func", "OneBot11");
        if (SecondaryService is not null) await SecondaryService.DisposeAsync();
        if (Service is not null) await Service.DisposeAsync();
    }
}

/// <summary>OneBot11 functional test collection.</summary>
[CollectionDefinition("OneBot11.Functional", DisableParallelization = true)]
public class OneBot11FunctionalCollection : ICollectionFixture<OneBot11TestFixture>
{
}