using Microsoft.Extensions.Logging;
using Sora.Entities;

namespace Sora.Tests.Helpers;

/// <summary>Configures the shared test-process log backend before framework use.</summary>
internal static class TestLogging
{
    /// <summary>Gets the process-wide sink shared by unit and functional tests.</summary>
    internal static TestOutputSink OutputSink { get; } = new();

    /// <summary>Reads the test level once at process startup and configures Sora logging.</summary>
    internal static void Initialize()
    {
        string? configuredLevel = Environment.GetEnvironmentVariable("SORA_TEST_LOG_LEVEL_OVERRIDE");
        LogLevel level = configuredLevel?.ToUpperInvariant() switch
                         {
                             "TRACE" => LogLevel.Trace,
                             "DEBUG" => LogLevel.Debug,
                             "INFO"  => LogLevel.Information,
                             "WARN"  => LogLevel.Warning,
                             "ERROR" => LogLevel.Error,
                             "FATAL" => LogLevel.Critical,
                             "NONE"  => LogLevel.None,
                             _       => LogLevel.Debug
                         };

        SoraLogger.Configure(SoraLogger.CreateDefaultLoggerConfiguration(level).WriteTo.Sink(OutputSink));
    }
}