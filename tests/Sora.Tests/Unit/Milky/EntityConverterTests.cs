using Mapster;
using Sora.Adapter.Milky.Models;
using Xunit;

namespace Sora.Tests.Unit.Milky;

/// <summary>Tests for Milky entity Mapster mappings.</summary>
[Collection("Milky.Unit")]
[Trait("Category", "Unit")]
public class EntityConverterTests
{
#region FriendInfo Mapping Tests

    /// <summary>Verifies <see cref="MilkyFriendEntity" /> → <see cref="FriendInfo" /> maps all fields.</summary>
    [Fact]
    public void ToFriendInfo_AllFields()
    {
        MilkyFriendEntity entity = new()
            {
                UserId   = 123, Nickname  = "Test", Sex = "female",
                Qid      = "qid1", Remark = "bestie",
                Category = new MilkyFriendCategoryEntity { CategoryId = 2, CategoryName = "Close" }
            };

        FriendInfo result = entity.Adapt<FriendInfo>();
        Assert.Equal(123L, (long)result.UserId);
        Assert.Equal(Sex.Female, result.Sex);
        Assert.Equal("qid1", result.Qid);
        Assert.Equal("bestie", result.Remark);
        Assert.NotNull(result.Category);
        Assert.Equal(2, result.Category!.CategoryId);
        Assert.Equal("Close", result.Category.CategoryName);
    }

    /// <summary>Verifies <see cref="MilkyFriendEntity" /> → <see cref="FriendInfo" /> handles a null category.</summary>
    [Fact]
    public void ToFriendInfo_NullCategory()
    {
        MilkyFriendEntity entity = new()
            {
                UserId = 1, Nickname = "N", Sex = "unknown", Category = null!
            };
        FriendInfo result = entity.Adapt<FriendInfo>();
        Assert.Null(result.Category);
    }

#endregion

#region Group Info Mapping Tests

    /// <summary>Verifies <see cref="MilkyGroupAnnouncementEntity" /> → <see cref="GroupAnnouncementInfo" /> maps all fields.</summary>
    [Fact]
    public void ToGroupAnnouncementInfo_AllFields()
    {
        MilkyGroupAnnouncementEntity entity = new()
            {
                GroupId = 100, AnnouncementId = "a1", UserId       = 200,
                Time    = 1700000000, Content = "Notice", ImageUrl = "http://img.png"
            };

        GroupAnnouncementInfo result = entity.Adapt<GroupAnnouncementInfo>();
        Assert.Equal(100L, (long)result.GroupId);
        Assert.Equal("a1", result.AnnouncementId);
        Assert.Equal("Notice", result.Content);
        Assert.Equal("http://img.png", result.ImageUrl);
    }

    /// <summary>Verifies <see cref="MilkyGroupFileEntity" /> → <see cref="GroupFileInfo" /> maps all fields.</summary>
    [Fact]
    public void ToGroupFileInfo_AllFields()
    {
        MilkyGroupFileEntity entity = new()
            {
                FileId     = "f1", FileName       = "doc.pdf", ParentFolderId = "/",
                FileSize   = 2048, UploadedTime   = 1700000000, ExpireTime    = 1700100000,
                UploaderId = 456, DownloadedTimes = 3
            };

        GroupFileInfo result = entity.Adapt<GroupFileInfo>();
        Assert.Equal("f1", result.FileId);
        Assert.Equal("/", result.ParentFolderId);
        Assert.Equal(2048, result.FileSize);
        Assert.NotNull(result.UploadedTime);
        Assert.NotNull(result.ExpireTime);
        Assert.Equal(456L, (long)result.UploaderId);
        Assert.Equal(3, result.DownloadedTimes);
    }

    /// <summary>Verifies <see cref="MilkyGroupFolderEntity" /> → <see cref="GroupFolderInfo" /> maps all fields.</summary>
    [Fact]
    public void ToGroupFolderInfo_AllFields()
    {
        MilkyGroupFolderEntity entity = new()
            {
                FolderId    = "d1", ParentFolderId         = "/", FolderName = "docs",
                CreatedTime = 1700000000, LastModifiedTime = 1700050000,
                CreatorId   = 789, FileCount               = 5
            };

        GroupFolderInfo result = entity.Adapt<GroupFolderInfo>();
        Assert.Equal("d1", result.FolderId);
        Assert.Equal("/", result.ParentFolderId);
        Assert.NotNull(result.CreatedTime);
        Assert.NotNull(result.LastModifiedTime);
        Assert.Equal(789L, (long)result.CreatorId);
        Assert.Equal(5, result.FileCount);
    }

    /// <summary>Verifies <see cref="MilkyGroupMemberEntity" /> → <see cref="GroupMemberInfo" /> maps all fields.</summary>
    [Fact]
    public void ToGroupMemberInfo_AllFields()
    {
        MilkyGroupMemberEntity entity = new()
            {
                UserId        = 100, GroupId             = 200, Nickname  = "User",
                Card          = "Card", Title            = "Title", Level = 5,
                Role          = "admin", Sex             = "male",
                JoinTime      = 1700000000, LastSentTime = 1700050000,
                ShutUpEndTime = 1700100000
            };

        GroupMemberInfo result = entity.Adapt<GroupMemberInfo>();
        Assert.Equal(MemberRole.Admin, result.Role);
        Assert.Equal(Sex.Male, result.Sex);
        Assert.NotNull(result.JoinTime);
        Assert.NotNull(result.MuteExpireTime);
    }

#endregion
}