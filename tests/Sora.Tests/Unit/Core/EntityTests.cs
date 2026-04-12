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

    /// <see cref="FriendInfo" />
    [Fact]
    public void FriendInfo_FullConstruction()
    {
        FriendInfo info = new()
            {
                UserId   = 123L, Nickname   = "Test", Sex = Sex.Male,
                Qid      = "qid123", Remark = "remark",
                Category = new FriendCategoryInfo { CategoryId = 1, CategoryName = "Friends" }
            };
        Assert.Equal(123L, (long)info.UserId);
        Assert.Equal(Sex.Male, info.Sex);
        Assert.Equal("qid123", info.Qid);
        Assert.NotNull(info.Category);
        Assert.Equal(1, info.Category!.CategoryId);
    }

    /// <see cref="FriendRequestInfo" />
    [Fact]
    public void FriendRequestInfo_Construction()
    {
        FriendRequestInfo req = new()
            {
                InitiatorId  = 100L, InitiatorUid = "uid1",
                TargetUserId = 200L, State        = "pending",
                Comment      = "hi", Via          = "search", IsFiltered = false
            };
        Assert.Equal("pending", req.State);
        Assert.Equal("uid1", req.InitiatorUid);
    }

#endregion

#region Group Info Tests

    /// <see cref="GroupAnnouncementInfo" />
    [Fact]
    public void GroupAnnouncementInfo_Construction()
    {
        GroupAnnouncementInfo ann = new()
            {
                GroupId = 100L, AnnouncementId  = "a1", UserId      = 200L,
                Time    = DateTime.Now, Content = "hello", ImageUrl = "http://img.png"
            };
        Assert.Equal("a1", ann.AnnouncementId);
        Assert.Equal("http://img.png", ann.ImageUrl);
    }

    /// <see cref="GroupEssenceMessageInfo" />
    [Fact]
    public void GroupEssenceMessageInfo_Construction()
    {
        GroupEssenceMessageInfo ess = new()
            {
                GroupId    = 100L, MessageId    = 999L, SenderId     = 200L,
                SenderName = "user", OperatorId = 300L, OperatorName = "admin"
            };
        Assert.Equal(999L, (long)ess.MessageId);
        Assert.Equal("admin", ess.OperatorName);
    }

    /// <see cref="GroupEssenceMessagesPage" />
    [Fact]
    public void GroupEssenceMessagesResult_Construction()
    {
        GroupEssenceMessagesPage page = new()
            {
                Messages = [],
                IsEnd    = true
            };
        Assert.Empty(page.Messages);
        Assert.True(page.IsEnd);
    }

    /// <see cref="GroupFileInfo" />
    [Fact]
    public void GroupFileInfo_AllFields()
    {
        GroupFileInfo file = new()
            {
                FileId     = "f1", FileName        = "test.txt", ParentFolderId = "/",
                FileSize   = 1024, UploadedTime    = DateTime.Now,
                UploaderId = 123L, DownloadedTimes = 5
            };
        Assert.Equal("f1", file.FileId);
        Assert.Equal("/", file.ParentFolderId);
        Assert.Equal(5, file.DownloadedTimes);
    }

    /// <see cref="GroupFolderInfo" />
    [Fact]
    public void GroupFolderInfo_AllFields()
    {
        GroupFolderInfo folder = new()
            {
                FolderId    = "d1", ParentFolderId           = "/", FolderName = "docs",
                CreatedTime = DateTime.Now, LastModifiedTime = DateTime.Now,
                CreatorId   = 456L, FileCount                = 10
            };
        Assert.Equal("d1", folder.FolderId);
        Assert.Equal("/", folder.ParentFolderId);
    }

    /// <see cref="GroupNotificationInfo" />
    [Fact]
    public void GroupNotificationInfo_Construction()
    {
        GroupNotificationInfo notif = new()
            {
                Type            = "join_request", GroupId = 100L,
                NotificationSeq = 12345, InitiatorId      = 200L,
                State           = "pending", Comment      = "let me in"
            };
        Assert.Equal("join_request", notif.Type);
    }

    /// <see cref="GroupNotificationsResult" />
    [Fact]
    public void GroupNotificationsResult_Construction()
    {
        GroupNotificationsResult result = new()
            {
                Notifications       = [],
                NextNotificationSeq = 999
            };
        Assert.Empty(result.Notifications);
        Assert.Equal(999, result.NextNotificationSeq);
    }

#endregion

#region Other Info Tests

    /// <see cref="HistoryMessagesResult" />
    [Fact]
    public void HistoryMessagesResult_Construction()
    {
        HistoryMessagesResult result = new()
            {
                Messages =
                        [new MessageContext { MessageId = 1L, Body = new MessageBody("test") }],
                NextMessageSeq = 2L
            };
        Assert.Single(result.Messages);
        Assert.Equal(2L, (long)(result.NextMessageSeq ?? default));
    }

    /// <see cref="ImplInfo" />
    [Fact]
    public void ImplInfo_Construction()
    {
        ImplInfo impl = new()
            {
                ImplName          = "LLBot", ImplVersion  = "1.0",
                QqProtocolVersion = "9.0", QqProtocolType = "windows",
                ProtocolVersion   = "0.5"
            };
        Assert.Equal("LLBot", impl.ImplName);
        Assert.Equal("0.5", impl.ProtocolVersion);
    }

    /// <see cref="UserProfile" />
    [Fact]
    public void UserProfile_Construction()
    {
        UserProfile profile = new()
            {
                UserId = 123L, Nickname = "Test", Qid     = "q1",
                Age    = 25, Sex        = Sex.Female, Bio = "hello",
                Level  = 50, Country    = "CN", City      = "SH"
            };
        Assert.Equal(25, profile.Age);
        Assert.Equal("hello", profile.Bio);
    }

#endregion
}