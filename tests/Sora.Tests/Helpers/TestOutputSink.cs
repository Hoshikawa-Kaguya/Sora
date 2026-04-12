using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace Sora.Tests.Helpers;

/// <summary>
///     Thread-safe Serilog <see cref="ILogEventSink" /> that forwards formatted log events
///     to dynamically registered subscribers. Designed for xUnit functional tests where
///     a shared collection fixture creates the Serilog logger, and individual test classes
///     subscribe their <c>ITestOutputHelper</c> to receive log output.
/// </summary>
/// <remarks>
///     <para>
///         Multiple test classes can subscribe concurrently via <see cref="Subscribe" />.
///         Each subscription returns an <see cref="IDisposable" /> handle; disposing it
///         removes the subscriber. Stale subscribers (whose <c>ITestOutputHelper</c> has
///         already been invalidated) are automatically removed on the next <see cref="Emit" />.
///     </para>
///     <para>
///         A bounded replay buffer retains recent log messages so that subscribers added
///         after fixture initialization still receive startup/connection logs.
///     </para>
/// </remarks>
public sealed class TestOutputSink : ILogEventSink
{
    private const    int                          DefaultReplayCapacity = 500;
    private readonly MessageTemplateTextFormatter _formatter;
    private readonly ConcurrentQueue<string>      _replayBuffer = new();
    private readonly int                          _replayCapacity;
    private readonly Lock                         _replayLock = new();

    private readonly ConcurrentDictionary<int, Action<string>> _subscribers = new();
    private          int                                       _nextId;
    private          int                                       _replayCount;

    /// <summary>Creates a new <see cref="TestOutputSink" /> with the specified Serilog output template.</summary>
    /// <param name="outputTemplate">
    ///     The Serilog message template used to format log events.
    ///     Defaults to the same template used by <c>SoraService.CreateDefaultLoggerConfiguration</c>.
    /// </param>
    /// <param name="replayCapacity">
    ///     Maximum number of recent log messages retained for replay to new subscribers.
    /// </param>
    public TestOutputSink(
        string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
        int    replayCapacity = DefaultReplayCapacity)
    {
        _formatter      = new MessageTemplateTextFormatter(outputTemplate);
        _replayCapacity = replayCapacity;
    }

    /// <inheritdoc />
    public void Emit(LogEvent logEvent)
    {
        using StringWriter writer = new();
        _formatter.Format(logEvent, writer);
        string formatted = writer.ToString().TrimEnd('\r', '\n');

        lock (_replayLock)
        {
            _replayBuffer.Enqueue(formatted);
            _replayCount++;
            while (_replayCount > _replayCapacity)
            {
                _replayBuffer.TryDequeue(out _);
                _replayCount--;
            }
        }

        foreach (KeyValuePair<int, Action<string>> kvp in _subscribers)
            try
            {
                kvp.Value(formatted);
            }
            catch (InvalidOperationException)
            {
                // ITestOutputHelper throws InvalidOperationException after the test completes;
                // auto-remove stale subscribers to prevent repeated failures.
                _subscribers.TryRemove(kvp.Key, out _);
            }
    }

    /// <summary>
    ///     Registers a callback to receive formatted log messages.
    ///     Buffered messages from before the subscription are replayed immediately.
    ///     Dispose the returned handle to unsubscribe.
    /// </summary>
    /// <param name="callback">
    ///     A delegate invoked for each log event (e.g. <c>ITestOutputHelper.WriteLine</c>).
    /// </param>
    /// <returns>An <see cref="IDisposable" /> that removes the subscription when disposed.</returns>
    public IDisposable Subscribe(Action<string> callback)
    {
        int id = Interlocked.Increment(ref _nextId);

        // Replay buffered messages under lock to ensure no gap between replay and live delivery.
        // Emit() also acquires _replayLock, so new messages arriving during replay are either
        // already in the buffer (replayed) or will see the subscriber in _subscribers (delivered live).
        lock (_replayLock)
        {
            _subscribers[id] = callback;
            foreach (string buffered in _replayBuffer)
                try
                {
                    callback(buffered);
                }
                catch (InvalidOperationException)
                {
                    _subscribers.TryRemove(id, out _);
                    return new Subscription(this, id);
                }
        }

        return new Subscription(this, id);
    }

    private void Unsubscribe(int id) => _subscribers.TryRemove(id, out _);

    /// <summary>Subscription handle returned by <see cref="TestOutputSink.Subscribe" />.</summary>
    private sealed class Subscription : IDisposable
    {
        private readonly int            _id;
        private readonly TestOutputSink _sink;

        public Subscription(TestOutputSink sink, int id)
        {
            _sink = sink;
            _id   = id;
        }

        /// <inheritdoc />
        public void Dispose() => _sink.Unsubscribe(_id);
    }
}