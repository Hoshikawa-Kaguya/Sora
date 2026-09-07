using System.Collections.Concurrent;
using System.Diagnostics;

namespace Sora.Entities.Filters;

/// <summary>
///     Pipeline context shared across all filters, commands, and event handlers within a single event processing cycle.
///     Created by the framework at the start of event processing and attached to <see cref="Events.BotEvent.PipelineContext" />.
///     This class is thread-safe.
/// </summary>
public sealed class PipelineContext
{
    private readonly ConcurrentDictionary<string, object> _items = new();

    /// <summary>
    ///     Sets a value in the pipeline context.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="key">The key to store the value under.</param>
    /// <param name="value">The value to store.</param>
    public void Set<T>(string key, T value) where T : notnull => _items[key] = value;

    /// <summary>
    ///     Attempts to retrieve a value from the pipeline context.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="key">The key to look up.</param>
    /// <param name="value">The retrieved value, if found and type-compatible.</param>
    /// <returns><c>true</c> if the key exists and the value is of type <typeparamref name="T" />; otherwise <c>false</c>.</returns>
    public bool TryGet<T>(string key, out T value) where T : notnull
    {
        if (_items.TryGetValue(key, out object? obj) && obj is T typed)
        {
            value = typed;
            return true;
        }

        value = default!;
        return false;
    }

    /// <summary>
    ///     Timestamp captured at the start of event processing (via <see cref="Stopwatch.GetTimestamp" />).
    ///     Use <c>Stopwatch.GetElapsedTime</c> to compute elapsed duration.
    /// </summary>
    public long StartTimestamp { get; init; }
}