using System.Collections.Concurrent;
using MatchType = Sora.Core.Enums.MatchType;

namespace Sora.Entities.MessageWaiting;

/// <summary>
///     Manages asynchronous message-waiting sessions.
///     Allows command handlers to wait for a follow-up message from the same user.
/// </summary>
internal sealed class MessageWaiter
{
    private readonly Lazy<ILogger>                              _loggerLazy = new(SoraLogger.CreateLogger<MessageWaiter>);
    private          ILogger                                    _logger => _loggerLazy.Value;
    private readonly ConcurrentDictionary<Guid, WaitingSession> _sessions = new();

#region Wait Message API

    /// <summary>
    ///     Waits for the next message from the same sender/group that matches the given patterns.
    /// </summary>
    /// <param name="source">The original event providing sender/group context.</param>
    /// <param name="patterns">Match expressions (interpreted according to <paramref name="matchType" />).</param>
    /// <param name="matchType">How to match patterns against message text.</param>
    /// <param name="timeout">Optional timeout; Default(null) value is 1 hour.</param>
    /// <param name="ct">Cancellation token to abort the wait.</param>
    /// <returns>The matched event, or null if timed out or canceled.</returns>
    public ValueTask<MessageReceivedEvent?> WaitForNextMessageAsync(
        MessageReceivedEvent source,
        string[]             patterns,
        MatchType            matchType = MatchType.Regex,
        TimeSpan?            timeout   = null,
        CancellationToken    ct        = default)
    {
        WaitingSession session = new()
            {
                ConnectionId     = source.ConnectionId,
                SenderId         = source.Message.SenderId,
                GroupId          = source.Message.GroupId,
                SourceType       = source.Message.SourceType,
                Patterns         = patterns,
                SessionMatchType = matchType
            };
        return EnqueueAndWaitAsync(session, timeout, ct);
    }

    /// <summary>
    ///     Waits for the next message from the same sender/group that satisfies the predicate.
    /// </summary>
    /// <param name="source">The original event providing sender/group context.</param>
    /// <param name="predicate">Custom match function applied to each incoming message.</param>
    /// <param name="timeout">Optional timeout; Default(null) value is 1 hour.</param>
    /// <param name="ct">Cancellation token to abort the wait.</param>
    /// <returns>The matched event, or null if timed out or canceled.</returns>
    public ValueTask<MessageReceivedEvent?> WaitForNextMessageAsync(
        MessageReceivedEvent             source,
        Func<MessageReceivedEvent, bool> predicate,
        TimeSpan?                        timeout = null,
        CancellationToken                ct      = default)
    {
        WaitingSession session = new()
            {
                ConnectionId = source.ConnectionId,
                SenderId     = source.Message.SenderId,
                GroupId      = source.Message.GroupId,
                SourceType   = source.Message.SourceType,
                Predicate    = predicate
            };
        return EnqueueAndWaitAsync(session, timeout, ct);
    }

    /// <summary>
    ///     Waits for the next message from the same sender/group (any content).
    /// </summary>
    /// <param name="source">The original event providing sender/group context.</param>
    /// <param name="timeout">Optional timeout; Default(null) value is 1 hour.</param>
    /// <param name="ct">Cancellation token to abort the wait.</param>
    /// <returns>The matched event, or null if timed out or canceled.</returns>
    public ValueTask<MessageReceivedEvent?> WaitForNextMessageAsync(
        MessageReceivedEvent source,
        TimeSpan?            timeout = null,
        CancellationToken    ct      = default)
    {
        WaitingSession session = new()
            {
                ConnectionId = source.ConnectionId,
                SenderId     = source.Message.SenderId,
                GroupId      = source.Message.GroupId,
                SourceType   = source.Message.SourceType
            };
        return EnqueueAndWaitAsync(session, timeout, ct);
    }

#endregion

#region Internal Management

    /// <summary>
    ///     Cancels and removes all waiting sessions.
    ///     Called during service shutdown.
    /// </summary>
    internal void DisposeAll()
    {
        if (!_sessions.IsEmpty)
            _logger.LogDebug("Disposing all message waiters ({Count} active sessions)", _sessions.Count);

        foreach (KeyValuePair<Guid, WaitingSession> kvp in _sessions)
            if (_sessions.TryRemove(kvp.Key, out WaitingSession? session))
                session.Completion.TrySetResult(null);
    }

    /// <summary>
    ///     Cancels and removes all waiting sessions for the given service.
    ///     Called during service shutdown.
    /// </summary>
    /// <param name="connectionId">The connection whose sessions to dispose.</param>
    internal void DisposeConnection(Guid connectionId)
    {
        int disposed = 0;
        foreach (KeyValuePair<Guid, WaitingSession> kvp in _sessions)
        {
            if (kvp.Value.ConnectionId != connectionId) continue;
            if (_sessions.TryRemove(kvp.Key, out WaitingSession? session))
            {
                session.Completion.TrySetResult(null);
                disposed++;
            }
        }

        if (disposed > 0)
            _logger.LogDebug("Disposed {Count} message waiter(s) for connection {ConnectionId}", disposed, connectionId);
    }

    /// <summary>
    ///     Attempts to match an incoming message against all waiting sessions.
    ///     If a match is found, the waiting session is completed and the event chain is stopped.
    /// </summary>
    /// <param name="incoming">The incoming message event.</param>
    /// <returns>True if a waiter was matched and signaled; false otherwise.</returns>
    internal bool TryMatch(MessageReceivedEvent incoming)
    {
        foreach (KeyValuePair<Guid, WaitingSession> kvp in _sessions)
        {
            if (!kvp.Value.IsMatch(incoming)) continue;
            // Remove and signal the waiter
            if (!_sessions.TryRemove(kvp.Key, out WaitingSession? session)) continue;

            _logger.LogInformation(
                "Message waiter {SessionId} matched message [{MessageId}] on connection {ConnectionId}",
                session.SessionId,
                incoming.Message.MessageId,
                incoming.ConnectionId);

            // Set result message for watting thread
            session.Completion.TrySetResult(incoming);
            return true;
        }

        return false;
    }

#endregion

#region Message Enqueue Helper

    private async ValueTask<MessageReceivedEvent?> EnqueueAndWaitAsync(
        WaitingSession    session,
        TimeSpan?         timeout,
        CancellationToken ct)
    {
        // Reject duplicate waits from the same source
        if (_sessions.Values.Any(s => s.IsSameSource(session.SenderId, session.GroupId, session.ConnectionId, session.SourceType)))
        {
            _logger.LogWarning(
                "Rejected duplicate message waiter for connection {ConnectionId}, source {SourceType}, sender {SenderId}, group {GroupId}",
                session.ConnectionId,
                session.SourceType,
                session.SenderId,
                session.GroupId);
            return null;
        }

        if (!_sessions.TryAdd(session.SessionId, session))
        {
            _logger.LogWarning("Failed to register message waiter {SessionId}", session.SessionId);
            return null;
        }

        _logger.LogInformation(
            "Registered message waiter {SessionId} (connection: {ConnectionId}, source: {SourceType}, sender: {SenderId}, group: {GroupId}, patterns: {PatternCount}, matchType: {MatchType})",
            session.SessionId,
            session.ConnectionId,
            session.SourceType,
            session.SenderId,
            session.GroupId,
            session.Patterns?.Length ?? 0,
            session.SessionMatchType);

        // Register cancellation callback to clean up on cancel
        CancellationTokenRegistration ctr = ct.Register(() =>
        {
            if (_sessions.TryRemove(session.SessionId, out WaitingSession? s))
                s.Completion.TrySetCanceled(ct);
        });

        timeout ??= TimeSpan.FromHours(1);
        try
        {
            // Waiting task
            Task<MessageReceivedEvent?> waitTask = session.Completion.Task;
            // Timeout task with linked CTS to cancel delay when message arrives
            using CancellationTokenSource delayCts  = CancellationTokenSource.CreateLinkedTokenSource(ct);
            Task                          delayTask = Task.Delay(timeout.Value, delayCts.Token);

            Task completed = await Task.WhenAny(waitTask, delayTask);
            // Message received — cancel the orphaned delay timer
            if (completed == waitTask)
            {
                await delayCts.CancelAsync();
                return await waitTask;
            }

            // Timeout: remove session and return null
            if (_sessions.TryRemove(session.SessionId, out WaitingSession? s))
            {
                s.Completion.TrySetResult(null);
                _logger.LogWarning(
                    "Message waiter {SessionId} timed out after {Timeout}s",
                    session.SessionId,
                    timeout.Value.TotalSeconds);
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            _sessions.TryRemove(session.SessionId, out _);
            _logger.LogDebug("Message waiter {SessionId} was canceled", session.SessionId);
            return null;
        }
        finally
        {
            await ctr.DisposeAsync();
        }
    }

#endregion
}