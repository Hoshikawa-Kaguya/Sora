using MatchType = Sora.Core.Enums.MatchType;

namespace Sora.Entities.MessageWaiting;

/// <summary>
///     Extension methods on <see cref="MessageReceivedEvent" /> for waiting on follow-up messages.
///     Usage: <c>MessageReceivedEvent? reply = await event.WaitForNextMessageAsync(timeout: TimeSpan.FromSeconds(30));</c>
/// </summary>
public static class MessageWaiterExtensions
{
    /// <param name="source">The current message event.</param>
    extension(MessageReceivedEvent source)
    {
        /// <summary>
        ///     Waits for the next message from the same sender/group that matches the given patterns.
        /// </summary>
        /// <param name="patterns">Match expressions (interpreted according to <paramref name="matchType" />).</param>
        /// <param name="matchType">How to match patterns against message text.</param>
        /// <param name="timeout">Optional timeout; Default(null) value is 1 hour.</param>
        /// <param name="ct">Cancellation token to abort the wait.</param>
        /// <returns>The matched event, or null if timed out or canceled.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the event was not dispatched through SoraService.</exception>
        public ValueTask<MessageReceivedEvent?> WaitForNextMessageAsync(
            string[]          patterns,
            MatchType         matchType = MatchType.Regex,
            TimeSpan?         timeout   = null,
            CancellationToken ct        = default)
        {
            MessageWaiter waiter = GetWaiterOrThrow(source);
            if (!Enum.IsDefined(matchType))
                throw new NotSupportedException($"Unknown match type:{matchType}");
            return waiter.WaitForNextMessageAsync(source, patterns, matchType, timeout, ct);
        }

        /// <summary>
        ///     Waits for the next message from the same sender/group that satisfies the predicate.
        /// </summary>
        /// <param name="predicate">Custom match function applied to each incoming message.</param>
        /// <param name="timeout">Optional timeout; Default(null) value is 1 hour.</param>
        /// <param name="ct">Cancellation token to abort the wait.</param>
        /// <returns>The matched event, or null if timed out or canceled.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the event was not dispatched through SoraService.</exception>
        public ValueTask<MessageReceivedEvent?> WaitForNextMessageAsync(
            Func<MessageReceivedEvent, bool> predicate,
            TimeSpan?                        timeout = null,
            CancellationToken                ct      = default)
        {
            MessageWaiter waiter = GetWaiterOrThrow(source);
            return waiter.WaitForNextMessageAsync(source, predicate, timeout, ct);
        }

        /// <summary>
        ///     Waits for the next message from the same sender/group (any content).
        /// </summary>
        /// <param name="timeout">Optional timeout; Default(null) value is 1 hour.</param>
        /// <param name="ct">Cancellation token to abort the wait.</param>
        /// <returns>The matched event, or null if timed out or canceled.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the event was not dispatched through SoraService.</exception>
        public ValueTask<MessageReceivedEvent?> WaitForNextMessageAsync(
            TimeSpan?         timeout = null,
            CancellationToken ct      = default)
        {
            MessageWaiter waiter = GetWaiterOrThrow(source);
            return waiter.WaitForNextMessageAsync(source, timeout, ct);
        }
    }

    private static MessageWaiter GetWaiterOrThrow(BotEvent source) =>
        source.Waiter
        ?? throw new InvalidOperationException(
            "MessageWaiter is not available on this event. "
            + "Ensure the event was dispatched through SoraService with EnableCommandManager enabled.");
}