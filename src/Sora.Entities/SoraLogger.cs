using Microsoft.Extensions.Logging.Abstractions;

namespace Sora.Entities;

/// <summary>
///     Global logger factory for the Sora framework.
///     Provides <see cref="ILogger" /> instances to all framework components without requiring dependency injection.
/// </summary>
/// <remarks>
///     <para>
///         By default, logging is silent (<see cref="NullLoggerFactory" />).
///         Set custom logger factory via service config to enable custom logging.
///         Must set logger factory before creating any services, or let the framework configure a default
///         Serilog-based console logger automatically.
///     </para>
///     <para>
///         This class is thread-safe. The <see cref="ILoggerFactory" /> reference swap is atomic.
///     </para>
/// </remarks>
public static class SoraLogger
{
    private static ILoggerFactory _factory = NullLoggerFactory.Instance;

    /// <summary>
    ///     Gets whether the logger has been sealed (a service has been created).
    /// </summary>
    internal static bool IsSealed { get; private set; }

    /// <summary>Creates a logger for the specified type.</summary>
    /// <typeparam name="T">The type whose name is used as the logger category.</typeparam>
    /// <returns>A new <see cref="ILogger" /> instance.</returns>
    public static ILogger CreateLogger<T>() => _factory.CreateLogger<T>();

    /// <summary>Creates a logger with the specified category name.</summary>
    /// <param name="categoryName">The category name for the logger.</param>
    /// <returns>A new <see cref="ILogger" /> instance.</returns>
    public static ILogger CreateLogger(string categoryName) => _factory.CreateLogger(categoryName);

    /// <summary>
    ///     Internal set logger factory for service creation and seals it.
    ///     Called internally by the framework when a service is created.
    ///     If already sealed, this method is a no-op.
    /// </summary>
    /// <param name="factory">The factory from config, or <c>null</c> to use default/existing.</param>
    /// <param name="defaultFactoryCreator">
    ///     Creates the default logger factory. Only called when <paramref name="factory" /> is <c>null</c>.
    /// </param>
    internal static void InternalInitFactory(ILoggerFactory? factory, Func<ILoggerFactory> defaultFactoryCreator)
    {
        if (IsSealed) return;
        _factory = factory ?? defaultFactoryCreator();
        IsSealed = true;
    }

    /// <summary>Resets to <see cref="NullLoggerFactory" /> (for testing).</summary>
    internal static void Reset()
    {
        _factory = NullLoggerFactory.Instance;
        IsSealed = false;
    }
}