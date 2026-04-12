using Xunit;

namespace Sora.Tests.Unit.Core;

/// <summary>Tests for <see cref="UserId" />, <see cref="GroupId" />, <see cref="MessageId" />, and result types.</summary>
[Collection("Core.Unit")]
[Trait("Category", "Unit")]
public class IdTypeTests
{
#region UserId Tests

    /// <see cref="UserId" />
    [Fact]
    public void UserId_ImplicitConversionFromLong()
    {
        UserId id = 123456789L;
        Assert.Equal(123456789L, id.Value);
        Assert.Equal(123456789L, (long)id);
    }

    /// <see cref="UserId" />
    [Fact]
    public void UserId_Equality()
    {
        UserId a = 100L;
        UserId b = 100L;
        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    /// <see cref="UserId" />
    [Fact]
    public void UserId_Inequality()
    {
        UserId a = 100L;
        UserId b = 200L;
        Assert.NotEqual(a, b);
    }

    /// <see cref="UserId" />
    [Fact]
    public void UserId_DefaultValue()
    {
        UserId id = default;
        Assert.Equal(0L, id.Value);
    }

    /// <see cref="UserId" />
    [Fact]
    public void UserId_LargeValue()
    {
        UserId id = 9999999999L;
        Assert.Equal(9999999999L, id.Value);
        Assert.Equal("9999999999", id.ToString());
    }

    /// <see cref="UserId" />
    [Fact]
    public void UserId_NegativeValue()
    {
        UserId id = -1L;
        Assert.Equal(-1L, id.Value);
    }

    /// <see cref="UserId.ToString" />
    [Fact]
    public void UserId_ToString()
    {
        UserId id = 12345L;
        Assert.Equal("12345", id.ToString());
    }

    /// <see cref="UserId" />
    [Fact]
    public void UserId_AsDictionaryKey()
    {
        Dictionary<UserId, string> map = new() { [100L] = "a", [200L] = "b" };
        map[100L] = "c";
        Assert.Equal(2, map.Count);
        Assert.Equal("c", map[100L]);
    }

    /// <see cref="UserId.Equals(object)" />
    [Fact]
    public void UserId_TypeSafety_NotEqualToGroupId()
    {
        // UserId and GroupId are different record struct types.
        // record struct Equals checks the concrete type first, so cross-type
        // equality is always false even when the wrapped value is the same.
        UserId  userId  = 100L;
        GroupId groupId = 100L;
        // ReSharper disable once SuspiciousTypeConversion.Global
        Assert.False(userId.Equals(groupId));
    }

#endregion

#region GroupId Tests

    /// <see cref="GroupId" />
    [Fact]
    public void GroupId_ImplicitConversionFromLong()
    {
        GroupId id = 987654321L;
        Assert.Equal(987654321L, id.Value);
    }

    /// <see cref="GroupId.CompareTo" />
    [Fact]
    public void GroupId_CompareTo()
    {
        GroupId a = 100L;
        GroupId b = 200L;
        Assert.True(a.CompareTo(b) < 0);
        Assert.True(b.CompareTo(a) > 0);
    }

    /// <see cref="GroupId.GetHashCode" />
    [Fact]
    public void GroupId_HashCode_EqualForSameValue()
    {
        GroupId a = 100L;
        GroupId b = 100L;
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    /// <see cref="GroupId" />
    [Fact]
    public void GroupId_InHashSet_Deduplicates()
    {
        // ReSharper disable once DuplicateKeyCollectionInitialization
        HashSet<GroupId> set = [1L, 2L, 1L];
        Assert.Equal(2, set.Count);
    }

#endregion

#region MessageId Tests

    /// <see cref="MessageId" />
    [Fact]
    public void MessageId_ImplicitConversionFromInt()
    {
        MessageId id = 42;
        Assert.Equal(42L, id.Value);
    }

    /// <see cref="MessageId" />
    [Fact]
    public void MessageId_ImplicitConversionFromLong()
    {
        MessageId id = 999999999L;
        Assert.Equal(999999999L, (long)id);
    }

    /// <see cref="MessageId" />
    [Fact]
    public void MessageId_FromInt_PreservesValue()
    {
        MessageId id = 42;
        Assert.Equal(42L, id.Value);
        Assert.Equal(42L, (long)id);
    }

    /// <see cref="MessageId" />
    [Fact]
    public void MessageId_MaxLongValue()
    {
        MessageId id = long.MaxValue;
        Assert.Equal(long.MaxValue, (long)id);
    }

#endregion

#region ApiResult Tests

    /// <see cref="ApiResult.Ok(string)" />
    [Fact]
    public void ApiResult_Ok_Factory()
    {
        ApiResult result = ApiResult.Ok();
        Assert.True(result.IsSuccess);
        Assert.Equal(ApiStatusCode.Ok, result.Code);
        Assert.Equal("ok", result.Message);
    }

    /// <see cref="ApiResult.Ok(string)" />
    [Fact]
    public void ApiResult_Success()
    {
        ApiResult result = ApiResult.Ok("success");
        Assert.True(result.IsSuccess);
        Assert.Equal(ApiStatusCode.Ok, result.Code);
    }

    /// <see cref="ApiResult.Fail" />
    [Fact]
    public void ApiResult_Failure()
    {
        ApiResult result = ApiResult.Fail(ApiStatusCode.Timeout, "timed out");
        Assert.False(result.IsSuccess);
        Assert.Equal(ApiStatusCode.Timeout, result.Code);
    }

    /// <see cref="ApiResult{T}.Ok" />
    [Fact]
    public void ApiResultT_Success()
    {
        ApiResult<string> result = ApiResult<string>.Ok("data");
        Assert.True(result.IsSuccess);
        Assert.Equal("data", result.Data);
    }

    /// <see cref="ApiResult{T}.Fail" />
    [Fact]
    public void ApiResultT_Fail_HasDefaultData()
    {
        ApiResult<string> result = ApiResult<string>.Fail(ApiStatusCode.NotFound, "not found");
        Assert.False(result.IsSuccess);
        Assert.Null(result.Data);
    }

#endregion

#region SendMessageResult Tests

    /// <see cref="SendMessageResult.Ok" />
    [Fact]
    public void SendMessageResult_Success()
    {
        MessageId         msgId  = 42;
        SendMessageResult result = SendMessageResult.Ok(msgId);
        Assert.True(result.IsSuccess);
        Assert.Equal(42L, (long)result.MessageId);
    }

    /// <see cref="SendMessageResult.Fail" />
    [Fact]
    public void SendMessageResult_Fail_HasDefaultMessageId()
    {
        SendMessageResult result = SendMessageResult.Fail(ApiStatusCode.Timeout, "err");
        Assert.False(result.IsSuccess);
        Assert.Equal(0L, (long)result.MessageId);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }

#endregion
}