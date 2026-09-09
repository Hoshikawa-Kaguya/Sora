using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Sora;
using Sora.Adapter.Milky;
using Sora.Adapter.OneBot11;
using Sora.Command;
using Sora.Core.Enums;
using Sora.Core.Types;
using Sora.Entities;
using Sora.Entities.Interfaces;
using Sora.Tests.Helpers;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using TaskExtensions = Sora.Entities.Utils.TaskExtensions;

namespace Sora.LoggingProbe;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        TextWriter         originalOutput = Console.Out;
        using StringWriter output         = new();
        Console.SetOut(TextWriter.Synchronized(output));
        try
        {
            await RunAsync(args, output);
            Console.SetOut(originalOutput);
            Console.WriteLine($"PASS {args[0]}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.SetOut(originalOutput);
            Console.Error.WriteLine(output.ToString());
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static async Task RunAsync(string[] args, StringWriter output)
    {
        switch (args[0])
        {
            case "default":
                WriteDefaultMessages();
                VerifyDefault(output);
                Throws<InvalidOperationException>(() => SoraLogger.Configure(NullLoggerFactory.Instance));
                break;
            case "entities-first":
                TaskExtensions.RunCatch(
                    Task.FromException(new Exception("source")),
                    (Action<Exception>)(_ => throw new Exception("ENTITIES_ERROR")));
                Verify(
                    output.ToString().Contains("ENTITIES_ERROR") && output.ToString().Contains("Sora.TaskExtensions"),
                    "Entities log was lost before service creation.");
                break;
            case "command-first":
                CommandManager manager = new();
                manager.RegisterDynamicCommand(_ => ValueTask.CompletedTask, ["probe"]);
                Verify(
                    output.ToString().Contains("Registered dynamic command")
                    && output.ToString().Contains("Sora.Command.CommandManager"),
                    "Command log was lost before service creation.");
                break;
            case "factory":
                RecordingFactory factory = new(LogLevel.Warning);
                SoraLogger.Configure(factory);
                SoraLogger.CreateLogger<RecordingFactory>().LogWarning("GENERIC");
                SoraLogger.CreateLogger("custom-category").LogWarning("STRING");
                SoraLogger.CreateLogger("custom-category").LogDebug("FILTERED");
                Verify(factory.Entries.Count == 2, "External factory filtering changed.");
                Verify(
                    factory.Entries.Any(e => e.Category.EndsWith("RecordingFactory") && e.Message == "GENERIC"),
                    "Generic category did not reach the configured backend.");
                Verify(
                    factory.Entries.Any(e => e.Category == "custom-category" && e.Message == "STRING"),
                    "String category did not reach the configured backend.");
                Verify(output.ToString().Length == 0, "Explicit factory unexpectedly created a console backend.");
                break;
            case "serilog":
                RecordingSink sink = new();
                SoraLogger.Configure(new LoggerConfiguration().MinimumLevel.Warning().WriteTo.Sink(sink));
                ILogger serilog = SoraLogger.CreateLogger("serilog-category");
                serilog.LogDebug("FILTERED");
                serilog.LogWarning("SERILOG");
                Verify(sink.Events.Count == 1, "Serilog minimum level was not respected.");
                LogEvent logged = sink.Events.Single();
                Verify(
                    logged.RenderMessage() == "SERILOG"
                    && logged.Properties["SourceContext"].ToString() == "\"serilog-category\"",
                    "Serilog category or sink changed.");
                break;
            case "null":
                SoraLogger.Configure(NullLoggerFactory.Instance);
                SoraLogger.CreateLogger("silent").LogCritical("SILENT");
                Throws<InvalidOperationException>(() => SoraLogger.Configure(new LoggerConfiguration()));
                Verify(output.ToString().Length == 0, "Explicit NullLoggerFactory was replaced.");
                break;
            case "repeat":
                RecordingFactory original = new();
                RecordingFactory rejected = new();
                SoraLogger.Configure(original);
                Throws<InvalidOperationException>(() => SoraLogger.Configure(original));
                Throws<InvalidOperationException>(() => SoraLogger.Configure(rejected));
                Throws<InvalidOperationException>(() => SoraLogger.Configure(new LoggerConfiguration()));
                SoraLogger.CreateLogger("retained").LogInformation("RETAINED");
                Verify(
                    original.Entries.Count == 1 && rejected.Entries.IsEmpty,
                    "Repeated configuration changed the factory.");
                Verify(original.DisposeCount == 0 && rejected.DisposeCount == 0, "Sora disposed an external factory.");
                break;
            case "default-config":
                LoggerConfiguration first  = SoraLogger.CreateDefaultLoggerConfiguration();
                LoggerConfiguration second = SoraLogger.CreateDefaultLoggerConfiguration(LogLevel.Debug);
                Verify(!ReferenceEquals(first, second), "Default configuration objects were shared.");
                RecordingSink defaultSink = new();
                SoraLogger.Configure(second.WriteTo.Sink(defaultSink));
                SoraLogger.CreateLogger("json").LogDebug("JSON {@Payload}", JObject.Parse("{\"answer\":42}"));
                Verify(
                    defaultSink.Events.Single().Properties["Payload"] is StructureValue structure
                    && structure.Properties.Any(p => p.Name == "answer" && p.Value.ToString() == "42"),
                    "Default configuration lost Debug customization or JsonNet destructuring.");
                break;
            case "failure-retry":
                Exception cause = new ApplicationException("construction failed");
                LoggerConfiguration failing = new LoggerConfiguration()
                                              .Destructure.AsScalar(new CallbackType(() => throw cause));
                InvalidOperationException failure =
                    Throws<InvalidOperationException>(() => SoraLogger.Configure(failing));
                Verify(
                    ReferenceEquals(failure.InnerException, cause),
                    "Initialization exception did not preserve its cause.");
                RecordingFactory retry = new();
                SoraLogger.Configure(retry);
                SoraLogger.CreateLogger("retry").LogInformation("RECOVERED");
                Verify(retry.Entries.Single().Message == "RECOVERED", "Failed initialization published a factory.");
                break;
            case "reentry":
                LoggerConfiguration recursive = new LoggerConfiguration()
                                                .Destructure.AsScalar(
                                                    new CallbackType(() => SoraLogger.CreateLogger("recursive")));
                InvalidOperationException recursion =
                    Throws<InvalidOperationException>(() => SoraLogger.Configure(recursive));
                Verify(recursion.InnerException is InvalidOperationException, "Re-entry was not rejected directly.");
                SoraLogger.Configure(new LoggerConfiguration().WriteTo.Console());
                SoraLogger.CreateLogger("recovered").LogInformation("RECOVERED");
                Verify(output.ToString().Contains("RECOVERED"), "Re-entry prevented a later initialization.");
                break;
            case "concurrent":
                await ConcurrentInitializationAsync(output);
                break;
            case "concurrent-configure":
                await ConcurrentConfigurationAsync();
                break;
            case "service-default":
                await using (SoraService service = SoraServiceFactory.Instance.CreateMilkyService(new MilkyConfig()))
                {
                    Throws<InvalidOperationException>(() => SoraLogger.Configure(NullLoggerFactory.Instance));
                    WriteDefaultMessages();
                    VerifyDefault(output);
                }

                break;
            case "service-lifecycle":
                await ServiceLifecycleAsync();
                break;
            case "service-failure":
                Throws<ArgumentException>(() => new SoraService(new InvalidAdapter(), new ServiceConfig()));
                Throws<InvalidOperationException>(() => SoraLogger.Configure(NullLoggerFactory.Instance));
                WriteDefaultMessages();
                VerifyDefault(output);
                break;
            case "service-owned-factory":
                RecordingSink ownedSink = new();
                SoraLogger.Configure(new LoggerConfiguration().WriteTo.Sink(ownedSink));
                SoraService owned = SoraServiceFactory.Instance.CreateMilkyService(new MilkyConfig());
                await owned.DisposeAsync();
                Throws<InvalidOperationException>(() => SoraLogger.Configure(NullLoggerFactory.Instance));
                SoraLogger.CreateLogger("after-dispose").LogInformation("ALIVE");
                Verify(
                    !ownedSink.Disposed && ownedSink.Events.Any(e => e.RenderMessage() == "ALIVE"),
                    "Disposing a service closed the shared Serilog backend.");
                break;
            case "arguments":
                Throws<ArgumentNullException>(() => SoraLogger.Configure((ILoggerFactory)null!));
                Throws<ArgumentNullException>(() => SoraLogger.Configure((LoggerConfiguration)null!));
                Throws<ArgumentNullException>(() => SoraLogger.CreateLogger(null!));
                SoraLogger.Configure(NullLoggerFactory.Instance);
                break;
            case "write-failure":
                ApplicationException writeFailure = new("sink failed");
                SoraLogger.Configure(new LoggerConfiguration().AuditTo.Sink(new ThrowingSink(writeFailure)));
                AggregateException observed = Throws<AggregateException>(() =>
                                                                             SoraLogger.CreateLogger("audit")
                                                                                       .LogInformation("FAIL"));
                Verify(
                    ReferenceEquals(writeFailure, observed.InnerException),
                    "A normal write failure was wrapped as initialization failure.");
                break;
            case "test-startup":
                TestStartup(int.Parse(args[1]));
                break;
            default:
                throw new ArgumentException($"Unknown scenario: {args[0]}");
        }
    }

    private static void WriteDefaultMessages()
    {
        ILogger logger = SoraLogger.CreateLogger("default-category");
        logger.LogInformation("DEFAULT_INFO");
        logger.LogDebug("DEFAULT_DEBUG");
    }

    private static void VerifyDefault(StringWriter output)
    {
        Verify(
            output.ToString().Contains("default-category") && output.ToString().Contains("DEFAULT_INFO"),
            "Automatic initialization did not emit Information.");
        Verify(
            !output.ToString().Contains("DEFAULT_DEBUG"),
            "The library default was affected by the test environment.");
    }

    private static async Task ConcurrentInitializationAsync(StringWriter output)
    {
        const int        readers    = 7;
        RecordingFactory factory    = new();
        using Barrier    barrier    = new(readers + 1);
        bool             configured = false;
        Task[] tasks = Enumerable.Range(0, readers).Select(index =>
                                                               Task.Factory.StartNew(
                                                                   () =>
                                                                   {
                                                                       barrier.SignalAndWait();
                                                                       SoraLogger.CreateLogger("race").LogInformation(
                                                                           "RACE_{Index}",
                                                                           index);
                                                                   },
                                                                   CancellationToken.None,
                                                                   TaskCreationOptions.LongRunning,
                                                                   TaskScheduler.Default)).ToArray();
        Task configure = Task.Factory.StartNew(
            () =>
            {
                barrier.SignalAndWait();
                try
                {
                    SoraLogger.Configure(factory);
                    configured = true;
                }
                catch (InvalidOperationException)
                {
                }
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        await Task.WhenAll(tasks.Append(configure));
        Verify(
            configured
                ? factory.Entries.Count == readers && output.ToString().Length == 0
                : factory.Entries.IsEmpty
                  && Enumerable.Range(0, readers).All(i => output.ToString().Contains($"RACE_{i}")),
            "Concurrent logs were split between backends or lost.");
        Throws<InvalidOperationException>(() => SoraLogger.Configure(factory));
    }

    private static async Task ConcurrentConfigurationAsync()
    {
        RecordingFactory[] factories  = Enumerable.Range(0, 6).Select(_ => new RecordingFactory()).ToArray();
        using Barrier      barrier    = new(factories.Length);
        int                successful = 0;
        Task[] tasks = factories.Select(factory => Task.Factory.StartNew(
                                            () =>
                                            {
                                                barrier.SignalAndWait();
                                                try
                                                {
                                                    SoraLogger.Configure(factory);
                                                    Interlocked.Increment(ref successful);
                                                }
                                                catch (InvalidOperationException)
                                                {
                                                }
                                            },
                                            CancellationToken.None,
                                            TaskCreationOptions.LongRunning,
                                            TaskScheduler.Default)).ToArray();
        await Task.WhenAll(tasks);
        SoraLogger.CreateLogger("winner").LogInformation("ONE");
        Verify(
            successful == 1 && factories.Count(factory => factory.Entries.Count == 1) == 1,
            "Concurrent Configure calls did not publish exactly one factory.");
        Verify(factories.All(factory => factory.DisposeCount == 0), "A rejected external factory was disposed.");
    }

    private static async Task ServiceLifecycleAsync()
    {
        RecordingFactory factory = new();
        SoraLogger.Configure(factory);
        SoraService[] services =
        [
            new(new MilkyAdapter(new MilkyConfig()), new MilkyConfig()),
            SoraServiceFactory.CreateService(new OneBot11Adapter(new OneBot11Config()), new OneBot11Config()),
            SoraServiceFactory.Instance.CreateMilkyService(new MilkyConfig()),
            SoraServiceFactory.Instance.CreateOneBot11Service(new OneBot11Config())
        ];
        Throws<InvalidOperationException>(() => SoraLogger.Configure(NullLoggerFactory.Instance));
        foreach (SoraService service in services)
            await service.DisposeAsync();
        Throws<InvalidOperationException>(() => SoraLogger.Configure(NullLoggerFactory.Instance));
        SoraLogger.CreateLogger("after-dispose").LogInformation("ALIVE");
        Verify(
            factory.DisposeCount == 0 && factory.Entries.Any(entry => entry.Message == "ALIVE"),
            "Service disposal released the external factory.");
        Verify(
            factory.Entries.Count(entry => entry.Category == "Sora.SoraService" && entry.Message.Contains("stopped"))
            == 4,
            "Both protocols and multiple services did not log through the shared factory.");
    }

    private static void TestStartup(int minimumLevel)
    {
        TestLogging.Initialize();
        List<string> messages = [];
        ILogger      logger   = SoraLogger.CreateLogger("test-level");
        logger.LogCritical("BEFORE_SUBSCRIPTION");
        using IDisposable subscription = TestLogging.OutputSink.Subscribe(messages.Add);
        Verify(
            messages.Any(message => message.Contains("BEFORE_SUBSCRIPTION")) == minimumLevel <= 5,
            "The shared test sink did not replay startup output.");
        for (int level = 0; level <= 5; level++)
            logger.Log((LogLevel)level, "LEVEL_{Level}", level);
        for (int level = 0; level <= 5; level++)
            Verify(
                messages.Any(message => message.Contains($"LEVEL_{level}")) == level >= minimumLevel,
                $"Test startup emitted the wrong level: {level}, minimum {minimumLevel}.");
        Environment.SetEnvironmentVariable("SORA_TEST_LOG_LEVEL_OVERRIDE", minimumLevel == 1 ? "None" : "Debug");
        logger.LogDebug("AFTER_CHANGE_DEBUG");
        logger.LogCritical("AFTER_CHANGE_CRITICAL");
        Verify(
            messages.Any(message => message.Contains("AFTER_CHANGE_DEBUG")) == minimumLevel <= 1
            && messages.Any(message => message.Contains("AFTER_CHANGE_CRITICAL")) == minimumLevel <= 5,
            "Changing the test variable reconfigured the process logger.");
        Throws<InvalidOperationException>(() => SoraLogger.Configure(NullLoggerFactory.Instance));
    }

    private static TException Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        throw new Exception($"Expected {typeof(TException).Name}.");
    }

    private static void Verify(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    private sealed class CallbackType : TypeDelegator
    {
        private readonly Action _callback;

        public CallbackType(Action callback)
            : base(typeof(Program))
        {
            _callback = callback;
        }

        public override int GetHashCode()
        {
            _callback();
            return base.GetHashCode();
        }
    }

    private sealed class RecordingSink : ILogEventSink, IDisposable
    {
        public ConcurrentQueue<LogEvent> Events                  { get; } = new();
        public bool                      Disposed                { get; private set; }
        public void                      Emit(LogEvent logEvent) => Events.Enqueue(logEvent);
        public void                      Dispose()               => Disposed = true;
    }

    private sealed class ThrowingSink : ILogEventSink
    {
        private readonly Exception _exception;

        public ThrowingSink(Exception exception)
        {
            _exception = exception;
        }

        public void Emit(LogEvent logEvent) => throw _exception;
    }

    private sealed class RecordingFactory : ILoggerFactory
    {
        private readonly ConcurrentDictionary<string, ILogger> _loggers = new();
        private readonly LogLevel                              _minimumLevel;

        public RecordingFactory(LogLevel minimumLevel = LogLevel.Debug)
        {
            _minimumLevel = minimumLevel;
        }

        public ConcurrentQueue<(string Category, string Message)> Entries      { get; } = new();
        public int                                                DisposeCount { get; private set; }

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, category => new RecordingLogger(this, category, _minimumLevel));

        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
        public void Dispose()                             => DisposeCount++;
    }

    private sealed class RecordingLogger : ILogger
    {
        private readonly RecordingFactory _factory;
        private readonly string           _category;
        private readonly LogLevel         _minimumLevel;

        public RecordingLogger(RecordingFactory factory, string category, LogLevel minimumLevel)
        {
            _factory      = factory;
            _category     = category;
            _minimumLevel = minimumLevel;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool         IsEnabled(LogLevel logLevel) => logLevel >= _minimumLevel && logLevel != LogLevel.None;

        public void Log<TState>(
            LogLevel                         logLevel,
            EventId                          eventId,
            TState                           state,
            Exception?                       exception,
            Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
                _factory.Entries.Enqueue((_category, formatter(state, exception)));
        }
    }

    private sealed class ServiceConfig : IBotServiceConfig
    {
        public UserId[] SuperUsers           => [];
        public UserId[] BlockUsers           => [];
        public bool     EnableCommandManager => false;
    }

    private sealed class InvalidAdapter : IBotAdapter
    {
        public string          ProtocolName                               => "Invalid";
        public AdapterState    State                                      => AdapterState.Stopped;
        public UserId          SelfId                                     => default;
        public IBotApi?        GetApi()                                   => null;
        public IBotConnection? GetConnection()                            => null;
        public ValueTask       StartAsync(CancellationToken ct = default) => ValueTask.CompletedTask;
        public ValueTask       StopAsync(CancellationToken  ct = default) => ValueTask.CompletedTask;
        public ValueTask       DisposeAsync()                             => ValueTask.CompletedTask;
    }
}