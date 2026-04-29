namespace Sora.Entities.Utils;

/// <summary>
///     Fire-and-forget extension methods for <see cref="Task" /> and <see cref="ValueTask" />
///     that catch and forward exceptions to a caller-supplied handler.
/// </summary>
public static class TaskExtensions
{
    private static readonly Lazy<ILogger> LoggerLazy = new(() => SoraLogger.CreateLogger("Sora.TaskExtensions"));
    private static          ILogger       Logger => LoggerLazy.Value;

    /// <summary>
    ///     Awaits the <paramref name="task" /> and catches any exception,
    ///     forwarding it to <paramref name="onError" />.
    /// </summary>
    /// <param name="task">The task to await.</param>
    /// <param name="onError">The handler invoked when an exception is thrown.</param>
    public static async void RunCatch(this Task task, Action<Exception> onError)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            try
            {
                onError(ex);
            }
            catch (Exception innerEx)
            {
                Logger.LogError(innerEx, "RunCatch onError handler itself threw an exception");
            }
        }
    }

    /// <summary>
    ///     Awaits the <paramref name="task" /> and catches any exception,
    ///     forwarding it to <paramref name="onError" />.
    /// </summary>
    /// <param name="task">The value task to await.</param>
    /// <param name="onError">The handler invoked when an exception is thrown.</param>
    public static async void RunCatch(this ValueTask task, Action<Exception> onError)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            try
            {
                onError(ex);
            }
            catch (Exception innerEx)
            {
                Logger.LogError(innerEx, "RunCatch onError handler itself threw an exception");
            }
        }
    }

    /// <summary>
    ///     Awaits the <paramref name="task" />, discards the result,
    ///     and catches any exception, forwarding it to <paramref name="onError" />.
    /// </summary>
    /// <typeparam name="T">The result type (discarded).</typeparam>
    /// <param name="task">The task to await.</param>
    /// <param name="onError">The handler invoked when an exception is thrown.</param>
    public static async void RunCatch<T>(this Task<T> task, Action<Exception> onError)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            try
            {
                onError(ex);
            }
            catch (Exception innerEx)
            {
                Logger.LogError(innerEx, "RunCatch onError handler itself threw an exception");
            }
        }
    }

    /// <summary>
    ///     Awaits the <paramref name="task" />, discards the result,
    ///     and catches any exception, forwarding it to <paramref name="onError" />.
    /// </summary>
    /// <typeparam name="T">The result type (discarded).</typeparam>
    /// <param name="task">The value task to await.</param>
    /// <param name="onError">The handler invoked when an exception is thrown.</param>
    public static async void RunCatch<T>(this ValueTask<T> task, Action<Exception> onError)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            try
            {
                onError(ex);
            }
            catch (Exception innerEx)
            {
                Logger.LogError(innerEx, "RunCatch onError handler itself threw an exception");
            }
        }
    }

    /// <summary>
    ///     Awaits the <paramref name="task" /> and catches any exception,
    ///     returning the fallback value produced by <paramref name="onError" />.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="task">The task to await.</param>
    /// <param name="onError">The handler that produces a fallback value on failure.</param>
    /// <returns>The task result, or the fallback value if an exception was caught.</returns>
    public static async Task<T> RunCatch<T>(this Task<T> task, Func<Exception, T> onError)
    {
        try
        {
            return await task;
        }
        catch (Exception ex)
        {
            return onError(ex);
        }
    }

    /// <summary>
    ///     Awaits the <paramref name="task" /> and catches any exception,
    ///     returning the fallback value produced by <paramref name="onError" />.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="task">The value task to await.</param>
    /// <param name="onError">The handler that produces a fallback value on failure.</param>
    /// <returns>The task result, or the fallback value if an exception was caught.</returns>
    public static async Task<T> RunCatch<T>(this ValueTask<T> task, Func<Exception, T> onError)
    {
        try
        {
            return await task;
        }
        catch (Exception ex)
        {
            return onError(ex);
        }
    }
}