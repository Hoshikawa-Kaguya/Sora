using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging.Abstractions;
using Sora.Tests.Functional.Milky;
using Sora.Tests.Functional.OneBot11;
using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Verifies <see cref="SoraLogger" /> initialization in fresh processes without resetting global state.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public sealed class SoraLoggerTests
{
#region Process Lifecycle

    /// <summary>Checks a complete <see cref="SoraLogger" /> lifecycle with captured backend output.</summary>
    [Theory]
    [InlineData("default")]
    [InlineData("entities-first")]
    [InlineData("command-first")]
    [InlineData("factory")]
    [InlineData("serilog")]
    [InlineData("null")]
    [InlineData("repeat")]
    [InlineData("default-config")]
    [InlineData("failure-retry")]
    [InlineData("reentry")]
    [InlineData("concurrent")]
    [InlineData("concurrent-configure")]
    [InlineData("service-default")]
    [InlineData("service-lifecycle")]
    [InlineData("service-failure")]
    [InlineData("service-owned-factory")]
    [InlineData("arguments")]
    [InlineData("write-failure")]
    public async Task Initialization_RespectsProcessContract(string scenario)
    {
        await RunProbeAsync(scenario, "Warn");
    }

    /// <summary>Checks the actual test startup helper, including aliases, fallback, and read-once behavior.</summary>
    [Theory]
    [InlineData(null, 1)]
    [InlineData("", 1)]
    [InlineData("invalid", 1)]
    [InlineData("Trace", 0)]
    [InlineData("Debug", 1)]
    [InlineData("Info", 2)]
    [InlineData("wArN", 3)]
    [InlineData("Error", 4)]
    [InlineData("Fatal", 5)]
    [InlineData("None", 6)]
    public async Task TestStartup_AppliesLevelOnlyDuringInitialization(string? level, int minimumLevel)
    {
        await RunProbeAsync("test-startup", level, minimumLevel);
    }

    /// <summary>Checks that both functional fixtures share the module-initialized sink.</summary>
    [Fact]
    public async Task FunctionalFixtures_ShareProcessSink()
    {
        await using MilkyTestFixture    milky    = new();
        await using OneBot11TestFixture oneBot11 = new();
        Assert.Same(TestLogging.OutputSink, milky.OutputSink);
        Assert.Same(milky.OutputSink, oneBot11.OutputSink);
        Assert.Throws<InvalidOperationException>(() =>
                                                     SoraLogger.Configure(NullLoggerFactory.Instance));
    }

#endregion

    private static async Task RunProbeAsync(string scenario, string? level, int? minimumLevel = null)
    {
        string probe = Path.Combine(AppContext.BaseDirectory, "LoggingProbe", "Sora.LoggingProbe.dll");
        Assert.True(File.Exists(probe), $"Logging probe was not built: {probe}");
        ProcessStartInfo start = new("dotnet")
        {
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true
        };
        start.ArgumentList.Add(probe);
        start.ArgumentList.Add(scenario);
        if (minimumLevel is not null)
            start.ArgumentList.Add(minimumLevel.Value.ToString(CultureInfo.InvariantCulture));
        if (level is null)
            start.Environment.Remove("SORA_TEST_LOG_LEVEL_OVERRIDE");
        else
            start.Environment["SORA_TEST_LOG_LEVEL_OVERRIDE"] = level;

        using Process process = new() { StartInfo = start };
        Assert.True(process.Start(), $"Could not start logging scenario: {scenario}");
        Task<string>                  output  = process.StandardOutput.ReadToEndAsync();
        Task<string>                  error   = process.StandardError.ReadToEndAsync();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            await process.WaitForExitAsync();
            Assert.Fail($"Logging scenario timed out: {scenario}\n{await output}\n{await error}");
        }

        string capturedOutput = await output;
        string capturedError  = await error;
        Assert.True(
            process.ExitCode == 0,
            $"Logging scenario failed: {scenario} ({level ?? "unset"})\n{capturedOutput}\n{capturedError}");
        Assert.Contains($"PASS {scenario}", capturedOutput);
    }
}