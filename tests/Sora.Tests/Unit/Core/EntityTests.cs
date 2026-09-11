using Xunit;

namespace Sora.Tests.Unit.Core;

/// <summary>Tests for entity info types in <see cref="Sora.Entities.Info" />.</summary>
[Collection("Core.Unit")]
[Trait("Category", "Unit")]
public class EntityTests
{
#region Friend Info Tests

    /// <see cref="FriendInfo" />
    [Fact]
    public void FriendInfo_DefaultValues()
    {
        FriendInfo info = new() { UserId = 1L };
        Assert.Equal(Sex.Unknown, info.Sex);
        Assert.Equal("", info.Qid);
        Assert.Equal("", info.Remark);
        Assert.Null(info.Category);
    }

#endregion
}