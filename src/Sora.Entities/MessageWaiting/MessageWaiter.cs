using System.Collections.Concurrent;
using MatchType = Sora.Core.Enums.MatchType;

namespace Sora.Entities.MessageWaiting;

/// <summary>
///     Manages asynchronous message-waiting sessions.
///     Allows command handlers to wait for a follow-up message from the same user.
/// </summary>
internal sealed class MessageWaiter
{
    private readonly ILogger _logger = SoraLogger.CreateLogger<MessageWaiter>();

    private readonly ConcurrentDictionary<SessionKey, WaitingSession> _sessions = new();

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

        foreach (WaitingSession session in _sessions.Values)
            TryComplete(session, null);
    }

    /// <summary>
    ///     Cancels and removes all waiting sessions for the given service.
    ///     Called during service shutdown.
    /// </summary>
    /// <param name="connectionId">The connection whose sessions to dispose.</param>
    internal void DisposeConnection(Guid connectionId)
    {
        int disposed = 0;
        foreach (WaitingSession session in _sessions.Values)
            if (session.ConnectionId == connectionId && TryComplete(session, null))
                disposed++;

        if (disposed > 0)
            _logger.LogDebug(
                "Disposed {Count} message waiter(s) for connection {ConnectionId}",
                disposed,
                connectionId);
    }

    /// <summary>
    ///     Attempts to match an incoming message against all waiting sessions.
    ///     If a match is found, the waiting session is completed and the event chain is stopped.
    /// </summary>
    /// <param name="incoming">The incoming message event.</param>
    /// <returns>True if a waiter was matched and signaled; false otherwise.</returns>
    internal bool TryMatch(MessageReceivedEvent incoming)
    {
        SessionKey source = new(
            incoming.ConnectionId,
            incoming.Message.SenderId,
            incoming.Message.GroupId,
            incoming.Message.SourceType);
        if (!_sessions.TryGetValue(source, out WaitingSession? session)
            || !session.IsMatch(incoming)
            || !TryComplete(session, incoming))
            return false;

        _logger.LogInformation(
            "Message waiter {SessionId} matched message [{MessageId}] on connection {ConnectionId}",
            session.SessionId,
            incoming.Message.MessageId,
            incoming.ConnectionId);
        return true;
    }

#endregion

#region Message Enqueue Helper

    private async ValueTask<MessageReceivedEvent?> EnqueueAndWaitAsync(
        WaitingSession    session,
        TimeSpan?         timeout,
        CancellationToken ct)
    {
        TimeSpan                      effectiveTimeout = timeout ?? TimeSpan.FromHours(1);
        using CancellationTokenSource delayCts         = new();
        // Task.Delay validates its supported timeout range before the session owns a source.
        Task                          delayTask        = Task.Delay(effectiveTimeout, delayCts.Token);
        try
        {
            if (!_sessions.TryAdd(session.Source, session))
            {
                _logger.LogWarning(
                    "Rejected duplicate message waiter for connection {ConnectionId}, source {SourceType}, sender {SenderId}, group {GroupId}",
                    session.ConnectionId,
                    session.SourceType,
                    session.SenderId,
                    session.GroupId);
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

            await using CancellationTokenRegistration ctr = ct.Register(() =>
            {
                if (TryComplete(session, null))
                    _logger.LogDebug("Message waiter {SessionId} was canceled", session.SessionId);
            });

            Task<MessageReceivedEvent?> waitTask = session.Completion.Task;
            if (await Task.WhenAny(waitTask, delayTask) == delayTask && TryComplete(session, null))
                _logger.LogWarning(
                    "Message waiter {SessionId} timed out after {Timeout}s",
                    session.SessionId,
                    effectiveTimeout.TotalSeconds);

            return await waitTask;
        }
        finally
        {
            await delayCts.CancelAsync();
        }
    }

    private bool TryComplete(WaitingSession session, MessageReceivedEvent? result)
    {
        // The exact session owns completion; an old callback cannot remove a replacement wait.
        if (!_sessions.TryRemove(new KeyValuePair<SessionKey, WaitingSession>(session.Source, session))) return false;
        session.Completion.SetResult(result);
        return true;
    }

#endregion
}