using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Core;
using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests the public logging configuration contract.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public sealed class SoraLoggerTests
{
#region Configuration

    /// <summary>Default and customized configurations filter actual sink output by the requested level.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CreateDefaultLoggerConfiguration_AppliesMinimumLevel(bool enableDebug)
    {
        TestOutputSink    sink         = new();
        List<string>      messages     = [];
        using IDisposable subscription = sink.Subscribe(messages.Add);
        LoggerConfiguration configuration = enableDebug
            ? SoraLogger.CreateDefaultLoggerConfiguration(LogLevel.Debug)
            : SoraLogger.CreateDefaultLoggerConfiguration();
        using Logger logger = configuration.WriteTo.Sink(sink).CreateLogger();

        logger.Debug("debug message");
        logger.Information("information message");

        Assert.Equal(enableDebug ? 2 : 1, messages.Count);
        Assert.Contains("information message", messages[^1]);
        if (enableDebug)
            Assert.Contains("debug message", messages[0]);
    }

    /// <summary>Both configuration overloads reject replacing the factory initialized by test startup.</summary>
    [Fact]
    public void Configure_AfterInitialization_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => SoraLogger.Configure(NullLoggerFactory.Instance));
        Assert.Throws<InvalidOperationException>(() =>
                                                     SoraLogger.Configure(
                                                         SoraLogger.CreateDefaultLoggerConfiguration()));
    }

#endregion
}