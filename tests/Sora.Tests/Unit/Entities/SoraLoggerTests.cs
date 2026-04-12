using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for <see cref="SoraLogger" />.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class SoraLoggerTests : IDisposable
{
#region CreateLogger Tests

    /// <see cref="SoraLogger.CreateLogger{T}" />
    [Fact]
    public void CreateLogger_Generic_ReturnsLogger()
    {
        ILogger logger = SoraLogger.CreateLogger<SoraLoggerTests>();
        Assert.NotNull(logger);
    }

    /// <see cref="SoraLogger.CreateLogger(string)" />
    [Fact]
    public void CreateLogger_WithCategoryName_ReturnsLogger()
    {
        ILogger logger = SoraLogger.CreateLogger("TestCategory");
        Assert.NotNull(logger);
    }

    /// <see cref="SoraLogger.CreateLogger{T}" />
    [Fact]
    public void CreateLogger_WithDefaultFactory_DoesNotThrowOnLog()
    {
        SoraLogger.Reset();
        ILogger logger = SoraLogger.CreateLogger<SoraLoggerTests>();

        // Should not throw — NullLogger silently discards
        logger.LogInformation("Test message");
        logger.LogError(new InvalidOperationException("test"), "Error message");
    }

#endregion

#region InternalInitFactory Tests

    /// <see cref="SoraLogger.InternalInitFactory" />
    [Fact]
    public void InternalInitFactory_WhenAlreadySealed_IsNoOp()
    {
        ILoggerFactory firstFactory = LoggerFactory.Create(_ => { });
        SoraLogger.InternalInitFactory(firstFactory, () => NullLoggerFactory.Instance);
        Assert.True(SoraLogger.IsSealed);

        // Second call should be a no-op
        bool secondCreated = false;
        SoraLogger.InternalInitFactory(
            null,
            () =>
            {
                secondCreated = true;
                return NullLoggerFactory.Instance;
            });

        Assert.False(secondCreated);

        // Reset before disposing to avoid race: parallel tests could hit the disposed factory
        SoraLogger.Reset();
        firstFactory.Dispose();
    }

    /// <see cref="SoraLogger.InternalInitFactory" />
    [Fact]
    public void InternalInitFactory_WithCustomFactory_UsesCustomFactory()
    {
        ILoggerFactory customFactory = LoggerFactory.Create(_ => { });

        SoraLogger.InternalInitFactory(
            customFactory,
            () =>
            {
                Assert.Fail("Should not create default factory when custom factory is provided");
                return NullLoggerFactory.Instance;
            });

        Assert.True(SoraLogger.IsSealed);

        // Reset before disposing to avoid race: parallel tests could hit the disposed factory
        SoraLogger.Reset();
        customFactory.Dispose();
    }

    /// <see cref="SoraLogger.InternalInitFactory" />
    [Fact]
    public void InternalInitFactory_WithoutCustomFactory_CreatesDefault()
    {
        bool defaultCreated = false;

        SoraLogger.InternalInitFactory(
            null,
            () =>
            {
                defaultCreated = true;
                return LoggerFactory.Create(_ => { });
            });

        Assert.True(defaultCreated);
        Assert.True(SoraLogger.IsSealed);
    }

#endregion

#region Reset Tests

    /// <see cref="SoraLogger.Reset" />
    [Fact]
    public void Reset_AfterSeal_Unseals()
    {
        SoraLogger.InternalInitFactory(null, () => LoggerFactory.Create(_ => { }));
        Assert.True(SoraLogger.IsSealed);

        SoraLogger.Reset();
        Assert.False(SoraLogger.IsSealed);
    }

    /// <see cref="SoraLogger.Reset" />
    [Fact]
    public void Reset_RestoresToNullLogger()
    {
        SoraLogger.InternalInitFactory(LoggerFactory.Create(_ => { }), () => NullLoggerFactory.Instance);

        SoraLogger.Reset();
        Assert.False(SoraLogger.IsSealed);

        // After reset, CreateLogger should still work (returns NullLogger)
        ILogger logger = SoraLogger.CreateLogger<SoraLoggerTests>();
        Assert.NotNull(logger);
    }

#endregion

    /// <inheritdoc />
    public void Dispose()
    {
        SoraLogger.Reset();
    }
}