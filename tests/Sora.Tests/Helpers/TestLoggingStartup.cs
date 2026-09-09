using System.Runtime.CompilerServices;

namespace Sora.Tests.Helpers;

/// <summary>Initializes logging before any test fixture or framework call.</summary>
internal static class TestLoggingStartup
{
    /// <summary>Runs the shared logging configuration once when the test assembly loads.</summary>
    [ModuleInitializer]
    internal static void Initialize() => TestLogging.Initialize();
}