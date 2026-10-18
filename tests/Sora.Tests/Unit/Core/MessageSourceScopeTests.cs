using Xunit;

namespace Sora.Tests.Unit.Core;

/// <summary>Tests for <see cref="MessageSourceScope" /> and <see cref="MessageSourceScopeExtensions" />.</summary>
[Collection("Core.Unit")]
[Trait("Category", "Unit")]
public class MessageSourceScopeTests
{
    /// <see cref="MessageSourceScopeExtensions.Contains" />
    [Theory]
    [InlineData(MessageSourceScope.Friend, MessageSourceType.Friend, true)]
    [InlineData(MessageSourceScope.Friend, MessageSourceType.Group, false)]
    [InlineData(MessageSourceScope.Group, MessageSourceType.Group, true)]
    [InlineData(MessageSourceScope.Group, MessageSourceType.Temp, false)]
    [InlineData(MessageSourceScope.Temp, MessageSourceType.Temp, true)]
    [InlineData(MessageSourceScope.Temp, MessageSourceType.Friend, false)]
    [InlineData(MessageSourceScope.Friend | MessageSourceScope.Temp, MessageSourceType.Friend, true)]
    [InlineData(MessageSourceScope.Friend | MessageSourceScope.Temp, MessageSourceType.Temp, true)]
    [InlineData(MessageSourceScope.Friend | MessageSourceScope.Temp, MessageSourceType.Group, false)]
    [InlineData(MessageSourceScope.Any, MessageSourceType.Friend, true)]
    [InlineData(MessageSourceScope.Any, MessageSourceType.Group, true)]
    [InlineData(MessageSourceScope.Any, MessageSourceType.Temp, true)]
    public void Contains_ConcreteSource(MessageSourceScope scope, MessageSourceType source, bool expected) =>
        Assert.Equal(expected, scope.Contains(source));

    /// <see cref="MessageSourceScopeExtensions.Contains" />
    [Fact]
    public void Contains_UndefinedSource_ReturnsFalse()
    {
        Assert.False(MessageSourceScope.Any.Contains((MessageSourceType)(-1)));
        Assert.False(MessageSourceScope.Any.Contains(0));
    }

    /// <see cref="MessageSourceScopeExtensions.Contains" />
    [Fact]
    public void Contains_ScopeWithoutSource_ReturnsFalse()
    {
        Assert.False(((MessageSourceScope)0).Contains(MessageSourceType.Friend));
        Assert.False(((MessageSourceScope)8).Contains(MessageSourceType.Group));
    }
}
