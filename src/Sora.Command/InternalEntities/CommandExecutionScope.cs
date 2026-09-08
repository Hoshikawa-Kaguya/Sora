using System.Collections.Concurrent;

namespace Sora.Command.InternalEntities;

/// <summary>
///     Releases an acquired command re-entry slot when its execution scope ends.
/// </summary>
internal readonly struct CommandExecutionScope : IDisposable
{
    private readonly ConcurrentDictionary<ExecutionKey, byte>? _activeExecutions;
    private readonly ExecutionKey _executionKey;

    /// <summary>
    ///     Takes ownership of an already acquired slot. Dispose only from its owning execution scope.
    /// </summary>
    internal CommandExecutionScope(
        ConcurrentDictionary<ExecutionKey, byte> activeExecutions, ExecutionKey executionKey)
    {
        _activeExecutions = activeExecutions;
        _executionKey = executionKey;
    }

    /// <summary>
    ///     指令执行完成后自动移除
    /// </summary>
    public void Dispose() => _activeExecutions?.TryRemove(_executionKey, out _);
}
