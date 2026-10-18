using System.Diagnostics;
using System.Threading.Channels;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Sora.Recording;

/// <summary>
///     Service-owned message record sink. Each registered recorder owns a bounded queue consumed by a single
///     thread-pool consumer, so recorder code never runs on adapter receive or send paths. Writing never blocks:
///     a record is dropped for any recorder whose queue is full.
/// </summary>
internal sealed class MessageRecorderHub : IMessageRecordSink
{
#region Fields

    private static readonly TimeSpan DropWarningInterval = TimeSpan.FromSeconds(10);

    private readonly ILogger                    _logger        = SoraLogger.CreateLogger<MessageRecorderHub>();
    private readonly List<RecorderRegistration> _registrations = [];
    private          ActiveRecorders?           _active;
    private          ActiveRecorders?           _abandoned;

#endregion

#region Registration and Lifecycle

    /// <summary>Adds a recorder. The caller guarantees registration happens before <see cref="StartAsync" />.</summary>
    /// <param name="recorder">The recorder.</param>
    /// <param name="options">The validated queue options.</param>
    internal void Register(IMessageRecorder recorder, MessageRecorderOptions options) =>
        _registrations.Add(new RecorderRegistration { Recorder = recorder, Options = options });

    /// <summary>
    ///     Creates the queues and consumers for the registered recorders. When a previous stop abandoned its drain,
    ///     waits within <paramref name="ct" /> for those consumers to finish so that no recorder is called concurrently.
    /// </summary>
    /// <param name="ct">Bounds the wait for abandoned consumers.</param>
    internal async ValueTask StartAsync(CancellationToken ct)
    {
        if (_registrations.Count == 0 || Volatile.Read(ref _active) is not null) return;

        if (_abandoned is { } abandoned)
        {
            Task abandonedConsumers = Task.WhenAll(abandoned.Consumers);
            if (!abandonedConsumers.IsCompleted)
            {
                _logger.LogWarning(
                    "Waiting for message recorder calls abandoned by the previous stop to finish before restarting");
                await abandonedConsumers.WaitAsync(ct);
            }

            abandoned.ConsumerCts.Dispose();
            _abandoned = null;
        }

        CancellationTokenSource consumerCts = new();
        RecorderQueue[] queues =
        [
            .. _registrations.Select(r => new RecorderQueue
            {
                Registration = r,
                Channel = Channel.CreateBounded<MessageRecord>(
                    new BoundedChannelOptions(r.Options.Capacity)
                    {
                        // Reader continuations must never run on the writing adapter thread.
                        AllowSynchronousContinuations = false,
                        SingleReader                  = true,
                        SingleWriter                  = false,
                        // Only TryWrite is used: a full queue makes it return false, which is counted as a drop.
                        FullMode                      = BoundedChannelFullMode.Wait
                    })
            })
        ];
        Task[] consumers = [.. queues.Select(q => Task.Run(() => ConsumeAsync(q, consumerCts.Token)))];

        Volatile.Write(
            ref _active,
            new ActiveRecorders { Queues = queues, Consumers = consumers, ConsumerCts = consumerCts });
        _logger.LogDebug("Message recording started with {RecorderCount} recorder(s)", queues.Length);
    }

    /// <summary>
    ///     Stops accepting records, then drains the queues within <paramref name="ct" />. Remaining records are
    ///     dropped with a warning when the wait is canceled; the next start waits for the abandoned consumers.
    /// </summary>
    /// <param name="ct">Bounds the drain.</param>
    internal async ValueTask StopAsync(CancellationToken ct)
    {
        ActiveRecorders? active = Interlocked.Exchange(ref _active, null);
        if (active is null) return;

        foreach (RecorderQueue queue in active.Queues)
        {
            queue.Closed = true;
            queue.Channel.Writer.TryComplete();
        }

        try
        {
            await Task.WhenAll(active.Consumers).WaitAsync(ct);
            active.ConsumerCts.Dispose();
            _logger.LogDebug("Message recording stopped; all queues drained");
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct && ct.IsCancellationRequested)
        {
            int remaining = active.Queues.Sum(q => q.Channel.Reader.Count);
            _logger.LogWarning(
                "Message recorder drain was canceled; dropping {RemainingCount} queued record(s)",
                remaining);
            // Consumers observe the canceled token and exit; the source stays undisposed until they finish.
            await active.ConsumerCts.CancelAsync();
            _abandoned = active;
        }
    }

#endregion

#region IMessageRecordSink

    /// <inheritdoc />
    public bool IsEnabled => Volatile.Read(ref _active) is not null;

    /// <inheritdoc />
    public void Write(MessageRecord record)
    {
        if (Volatile.Read(ref _active) is not { } active) return;

        foreach (RecorderQueue queue in active.Queues)
            if (!queue.Channel.Writer.TryWrite(record))
                ReportDropped(queue);
    }

#endregion

#region Consumer

    private async Task ConsumeAsync(RecorderQueue queue, CancellationToken ct)
    {
        IMessageRecorder recorder = queue.Registration.Recorder;
        try
        {
            await foreach (MessageRecord record in queue.Channel.Reader.ReadAllAsync(ct))
                try
                {
                    await recorder.OnMessageAsync(record, ct);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == ct && ct.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Message recorder {Recorder} failed for {Direction} message [{MessageId}]",
                        recorder.GetType().Name,
                        record.Direction,
                        record.MessageId);
                    if (ct.IsCancellationRequested) return;
                }
        }
        catch (OperationCanceledException ex) when (ex.CancellationToken == ct && ct.IsCancellationRequested)
        {
            // Draining was abandoned by the service.
        }
    }

    private void ReportDropped(RecorderQueue queue)
    {
        if (queue.Closed) return;

        Interlocked.Increment(ref queue.DroppedSinceWarning);
        long now  = Stopwatch.GetTimestamp();
        long last = Volatile.Read(ref queue.LastWarningTimestamp);
        if (last != 0 && Stopwatch.GetElapsedTime(last, now) < DropWarningInterval) return;
        if (Interlocked.CompareExchange(ref queue.LastWarningTimestamp, now, last) != last) return;

        long dropped = Interlocked.Exchange(ref queue.DroppedSinceWarning, 0);
        _logger.LogWarning(
            "Message recorder {Recorder} queue is full (capacity {Capacity}); dropped {DroppedCount} record(s)",
            queue.Registration.Recorder.GetType().Name,
            queue.Registration.Options.Capacity,
            dropped);
    }

#endregion

#region Nested Types

    private sealed class RecorderRegistration
    {
        public required IMessageRecorder       Recorder { get; init; }
        public required MessageRecorderOptions Options  { get; init; }
    }

    private sealed class RecorderQueue
    {
        public required RecorderRegistration   Registration { get; init; }
        public required Channel<MessageRecord> Channel      { get; init; }

        public volatile bool Closed;
        public          long DroppedSinceWarning;
        public          long LastWarningTimestamp;
    }

    private sealed class ActiveRecorders
    {
        public required RecorderQueue[]         Queues      { get; init; }
        public required Task[]                  Consumers   { get; init; }
        public required CancellationTokenSource ConsumerCts { get; init; }
    }

#endregion
}