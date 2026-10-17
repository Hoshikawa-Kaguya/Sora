using System.Collections.Concurrent;

namespace Sora.Command.InternalEntities;

/// <summary>
///     Releases an acquired in-flight invocation and optional command re-entry slot when its execution scope ends.
/// </summary>
internal readonly struct CommandExecutionScope : IDisposable
{
    private readonly ConcurrentDictionary<ExecutionKey, byte>? _activeExecutions;
    private readonly ExecutionKey                              _executionKey;
    private readonly AssemblyRegistration?                     _registration;

    /// <summary>
    ///     Takes ownership of an already entered invocation and, when re-entry protection is enabled, an already
    ///     acquired slot. Dispose only from its owning execution scope.
    /// </summary>
    internal CommandExecutionScope(
        AssemblyRegistration                      registration,
        ConcurrentDictionary<ExecutionKey, byte>? activeExecutions,
        ExecutionKey                              executionKey)
    {
        _registration     = registration;
        _activeExecutions = activeExecutions;
        _executionKey     = executionKey;
    }

    /// <summary>
    ///     指令执行完成后自动移除
    /// </summary>
    public void Dispose()
    {
        _activeExecutions?.TryRemove(_executionKey, out _);
        _registration?.ExitInvocation();
    }
}
