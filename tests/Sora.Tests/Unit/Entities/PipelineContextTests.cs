using Xunit;

namespace Sora.Tests.Unit.Entities;

/// <summary>Tests for <see cref="PipelineContext" />.</summary>
[Collection("Entities.Unit")]
[Trait("Category", "Unit")]
public class PipelineContextTests
{
#region Set and TryGet Tests

    /// <see cref="PipelineContext.Set{T}" />
    [Fact]
    public void Set_TryGet_RoundTrips_Value()
    {
        PipelineContext ctx = new() { StartTimestamp = 0 };
        ctx.Set("key1", "hello");

        bool found = ctx.TryGet("key1", out string value);

        Assert.True(found);
        Assert.Equal("hello", value);
    }

    /// <see cref="PipelineContext.TryGet{T}" />
    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        PipelineContext ctx = new() { StartTimestamp = 0 };

        bool found = ctx.TryGet("nonexistent", out string value);

        Assert.False(found);
        Assert.Equal(default!, value);
    }

    /// <see cref="PipelineContext.TryGet{T}" />
    [Fact]
    public void TryGet_TypeMismatch_ReturnsFalse()
    {
        PipelineContext ctx = new() { StartTimestamp = 0 };
        ctx.Set("key1", 42);

        bool found = ctx.TryGet("key1", out string value);

        Assert.False(found);
        Assert.Equal(default!, value);
    }

    /// <see cref="PipelineContext.Set{T}" />
    [Fact]
    public void Set_SameKey_OverwritesPreviousValue()
    {
        PipelineContext ctx = new() { StartTimestamp = 0 };
        ctx.Set("key1", "first");
        ctx.Set("key1", "second");

        bool found = ctx.TryGet("key1", out string value);

        Assert.True(found);
        Assert.Equal("second", value);
    }

    /// <see cref="PipelineContext.Set{T}" />
    [Fact]
    public void Set_MultipleKeys_IndependentStorage()
    {
        PipelineContext ctx = new() { StartTimestamp = 0 };
        ctx.Set("str", "value");
        ctx.Set("num", 123);

        Assert.True(ctx.TryGet("str", out string strVal));
        Assert.Equal("value", strVal);
        Assert.True(ctx.TryGet("num", out int numVal));
        Assert.Equal(123, numVal);
    }

    /// <see cref="PipelineContext.StartTimestamp" />
    [Fact]
    public void StartTimestamp_PreservesValue()
    {
        PipelineContext ctx = new() { StartTimestamp = 12345L };
        Assert.Equal(12345L, ctx.StartTimestamp);
    }

#endregion
}