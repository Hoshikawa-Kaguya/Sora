using System.Reflection;

namespace Sora.Tests.Helpers;

/// <summary>Logger that records warning messages, injected into a component's private <c>_logger</c> field.</summary>
internal sealed class WarningCaptureLogger : ILogger
{
    /// <summary>Gets the formatted warning messages in logging order.</summary>
    public List<string> Warnings { get; } = [];

    /// <summary>Replaces the private <c>_logger</c> field of <paramref name="target" /> with a new capture logger.</summary>
    /// <param name="target">The component whose logger is replaced.</param>
    /// <returns>The injected capture logger.</returns>
    public static WarningCaptureLogger InjectInto(object target)
    {
        WarningCaptureLogger logger = new();
        FieldInfo field = target.GetType().GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic)!;
        field.SetValue(target, logger);
        return logger;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel                         logLevel,
        EventId                          eventId,
        TState                           state,
        Exception?                       exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning) Warnings.Add(formatter(state, exception));
    }
}