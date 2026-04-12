using Newtonsoft.Json.Linq;
using Sora.Adapter.OneBot11.Converter;
using Sora.Adapter.OneBot11.Events;
using Sora.Adapter.OneBot11.Models;
using Xunit;

namespace Sora.Tests.Unit.OneBot11;

/// <summary>Tests for <see cref="EventConverter" /> (OneBot11).</summary>
[Collection("OneBot11.Unit")]
[Trait("Category", "Unit")]
public class EventConverterTests
{
    private static readonly Guid TestConnectionId = Guid.NewGuid();

#region Message Event Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a private message.</summary>
    [Fact]
    public void ConvertMessageEvent_Private()
    {
        OneBotEvent obEvent = new()
            {
                Time        = 1700000000, SelfId = 12345, PostType = "message",
                MessageType = "private", SubType = "friend",
                MessageId   = 100, UserId        = 67890,
                Message     = JArray.Parse(@"[{""type"":""text"",""data"":{""text"":""hello""}}]"),
                Sender      = new OneBotSender { UserId = 67890, Nickname = "TestUser", Sex = "male" }
            };

        BotEvent result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        Assert.IsType<MessageReceivedEvent>(result);
        MessageReceivedEvent msg = (MessageReceivedEvent)result;
        Assert.Equal(MessageSourceType.Friend, msg.Message.SourceType);
        Assert.Equal(100L, (long)msg.Message.MessageId);
        Assert.Equal(67890L, (long)msg.Message.SenderId);
        Assert.Equal("hello", msg.Message.Body.GetText());
        Assert.Equal("TestUser", msg.Sender.Nickname);
        Assert.Equal(Sex.Male, msg.Sender.Sex);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group message.</summary>
    [Fact]
    public void ConvertMessageEvent_Group()
    {
        OneBotEvent obEvent = new()
            {
                Time        = 1700000000, SelfId = 12345, PostType = "message",
                MessageType = "group", SubType   = "normal",
                MessageId   = 200, UserId        = 67890, GroupId = 111222,
                Message     = JArray.Parse(@"[{""type"":""text"",""data"":{""text"":""group msg""}}]"),
                Sender = new OneBotSender
                        { UserId = 67890, Nickname = "Member", Role = "admin", Card = "CardName" }
            };

        BotEvent             result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        MessageReceivedEvent msg    = (MessageReceivedEvent)result;
        Assert.Equal(MessageSourceType.Group, msg.Message.SourceType);
        Assert.Equal(111222L, (long)msg.Message.GroupId);
        Assert.NotNull(msg.Group);
        Assert.NotNull(msg.Member);
        Assert.Equal(MemberRole.Admin, msg.Member.Role);
        Assert.Equal("CardName", msg.Member.Card);
    }

#endregion

#region Notice Event Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a friend-add notice.</summary>
    [Fact]
    public void ConvertNotice_FriendAdd()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "friend_add",
                UserId     = 222
            };

        BotEvent         result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        FriendAddedEvent evt    = (FriendAddedEvent)result;
        Assert.Equal(222L, (long)evt.UserId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a friend-recall notice.</summary>
    [Fact]
    public void ConvertNotice_FriendRecall()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "friend_recall",
                UserId     = 222, MessageId = 888
            };

        BotEvent            result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        MessageDeletedEvent evt    = (MessageDeletedEvent)result;
        Assert.Equal(MessageSourceType.Friend, evt.SourceType);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-admin notice.</summary>
    [Fact]
    public void ConvertNotice_GroupAdmin()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId     = 12345, PostType = "notice",
                NoticeType = "group_admin", SubType = "set",
                GroupId    = 111, UserId            = 222
            };

        BotEvent               result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupAdminChangedEvent evt    = (GroupAdminChangedEvent)result;
        Assert.True(evt.IsSet);
        Assert.Equal(222L, (long)evt.UserId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-ban notice.</summary>
    [Fact]
    public void ConvertNotice_GroupBan()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId   = 12345, PostType = "notice",
                NoticeType = "group_ban", SubType = "ban",
                GroupId    = 111, UserId          = 222, OperatorId = 333, Duration = 3600
            };

        BotEvent       result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupMuteEvent evt    = (GroupMuteEvent)result;
        Assert.Equal(3600, evt.DurationSeconds);
        Assert.False(evt.IsWholeGroup);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-decrease kick notice.</summary>
    [Fact]
    public void ConvertNotice_GroupDecrease_Kick()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId        = 12345, PostType = "notice",
                NoticeType = "group_decrease", SubType = "kick",
                GroupId    = 111, UserId               = 222, OperatorId = 333
            };

        BotEvent        result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        MemberLeftEvent evt    = (MemberLeftEvent)result;
        Assert.True(evt.IsKicked);
        Assert.Equal(333L, (long)(evt.OperatorId ?? default));
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-decrease leave notice.</summary>
    [Fact]
    public void ConvertNotice_GroupDecrease_Leave()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId        = 12345, PostType = "notice",
                NoticeType = "group_decrease", SubType = "leave",
                GroupId    = 111, UserId               = 222
            };

        BotEvent        result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        MemberLeftEvent evt    = (MemberLeftEvent)result;
        Assert.False(evt.IsKicked);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-increase notice.</summary>
    [Fact]
    public void ConvertNotice_GroupIncrease()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId        = 12345, PostType = "notice",
                NoticeType = "group_increase", SubType = "approve",
                GroupId    = 111, UserId               = 222, OperatorId = 333
            };

        BotEvent result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        Assert.IsType<MemberJoinedEvent>(result);
        MemberJoinedEvent evt = (MemberJoinedEvent)result;
        Assert.Equal(111L, (long)evt.GroupId);
        Assert.Equal(222L, (long)evt.UserId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-recall notice.</summary>
    [Fact]
    public void ConvertNotice_GroupRecall()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "group_recall",
                GroupId    = 111, UserId = 222, OperatorId = 333, MessageId = 999
            };

        BotEvent            result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        MessageDeletedEvent evt    = (MessageDeletedEvent)result;
        Assert.Equal(MessageSourceType.Group, evt.SourceType);
        Assert.Equal(999L, (long)evt.MessageId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-upload notice.</summary>
    [Fact]
    public void ConvertNotice_GroupUpload()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "group_upload",
                GroupId    = 111, UserId = 222,
                File       = new OneBotFile { Id = "file1", Name = "test.txt", Size = 1024 }
            };

        BotEvent        result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        FileUploadEvent evt    = (FileUploadEvent)result;
        Assert.Equal("file1", evt.FileId);
        Assert.Equal("test.txt", evt.FileName);
        Assert.Equal(1024L, evt.FileSize);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a poke notice.</summary>
    [Fact]
    public void ConvertNotice_Poke()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "notify", SubType  = "poke",
                GroupId    = 111, UserId        = 222, TargetId = 333
            };

        BotEvent   result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        NudgeEvent evt    = (NudgeEvent)result;
        Assert.Equal(222L, (long)evt.SenderId);
        Assert.Equal(333L, (long)evt.ReceiverId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts an essence notice.</summary>
    [Fact]
    public void ConvertNotice_Essence()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "essence", SubType = "add",
                GroupId    = 111, MessageId     = 999, OperatorId = 333, SenderId = 444
            };

        BotEvent                 result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupEssenceChangedEvent evt    = (GroupEssenceChangedEvent)result;
        Assert.Equal(111L, (long)evt.GroupId);
        Assert.Equal(999L, (long)evt.MessageId);
        Assert.Equal(333L, (long)evt.OperatorId);
        Assert.Equal(444L, (long)evt.SenderId);
        Assert.True(evt.IsSet);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group_msg_emoji_like notice.</summary>
    [Fact]
    public void ConvertNotice_GroupMsgEmojiLike()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "group_msg_emoji_like",
                GroupId    = 111, UserId = 222, MessageId = 888,
                IsAdd      = true,
                Likes      = JArray.Parse(@"[{""emoji_id"":""128077""}]")
            };

        BotEvent           result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupReactionEvent evt    = (GroupReactionEvent)result;
        Assert.Equal(111L, (long)evt.GroupId);
        Assert.Equal(222L, (long)evt.UserId);
        Assert.Equal(888L, (long)evt.MessageId);
        Assert.Equal("128077", evt.FaceId);
        Assert.True(evt.IsAdd);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group_card notice.</summary>
    [Fact]
    public void ConvertNotice_GroupCard()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "group_card",
                GroupId    = 111, UserId        = 222,
                CardOld    = "OldCard", CardNew = "NewCard"
            };

        BotEvent              result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupCardChangedEvent evt    = (GroupCardChangedEvent)result;
        Assert.Equal(111L, (long)evt.GroupId);
        Assert.Equal(222L, (long)evt.UserId);
        Assert.Equal("OldCard", evt.CardOld);
        Assert.Equal("NewCard", evt.CardNew);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group_dismiss notice.</summary>
    [Fact]
    public void ConvertNotice_GroupDismiss()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "group_dismiss",
                GroupId    = 111, UserId = 222
            };

        BotEvent            result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupDismissedEvent evt    = (GroupDismissedEvent)result;
        Assert.Equal(111L, (long)evt.GroupId);
        Assert.Equal(222L, (long)evt.OperatorId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a title notify notice.</summary>
    [Fact]
    public void ConvertNotice_Title()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "notify", SubType  = "title",
                GroupId    = 111, UserId        = 222, Title = "Champion"
            };

        BotEvent               result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupTitleChangedEvent evt    = (GroupTitleChangedEvent)result;
        Assert.Equal(111L, (long)evt.GroupId);
        Assert.Equal(222L, (long)evt.UserId);
        Assert.Equal("Champion", evt.Title);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a profile_like notify notice.</summary>
    [Fact]
    public void ConvertNotice_ProfileLike()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "notify", SubType  = "profile_like",
                OperatorId = 222, OperatorNick  = "LikerNick", Times = 5
            };

        BotEvent          result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        ProfileLikedEvent evt    = (ProfileLikedEvent)result;
        Assert.Equal(222L, (long)evt.SenderId);
        Assert.Equal("LikerNick", evt.OperatorNickname);
        Assert.Equal(5, evt.Times);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group poke_recall notice.</summary>
    [Fact]
    public void ConvertNotice_PokeRecall_Group()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "notify", SubType  = "poke_recall",
                GroupId    = 111, UserId        = 222
            };

        BotEvent             result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupPokeRecallEvent evt    = (GroupPokeRecallEvent)result;
        Assert.Equal(111L, (long)evt.GroupId);
        Assert.Equal(222L, (long)evt.UserId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a friend poke_recall notice.</summary>
    [Fact]
    public void ConvertNotice_PokeRecall_Friend()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "notify", SubType  = "poke_recall",
                UserId     = 222
            };

        BotEvent              result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        FriendPokeRecallEvent evt    = (FriendPokeRecallEvent)result;
        Assert.Equal(222L, (long)evt.UserId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a flash_file_downloading notice.</summary>
    [Fact]
    public void ConvertNotice_FlashFileDownloading()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "flash_file_downloading",
                Title      = "photo.jpg", FileSetId = "fset1", SceneType = 2
            };

        BotEvent                  result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        FlashFileDownloadingEvent evt    = (FlashFileDownloadingEvent)result;
        Assert.Equal("photo.jpg", evt.Title);
        Assert.Equal("fset1", evt.FileSetId);
        Assert.Equal(2, evt.SceneType);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a flash_file_downloaded notice.</summary>
    [Fact]
    public void ConvertNotice_FlashFileDownloaded()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "flash_file_downloaded",
                Title      = "photo.jpg", FileSetId = "fset1", SceneType = 2,
                FileUrl    = "http://example.com/photo.jpg"
            };

        BotEvent                 result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        FlashFileDownloadedEvent evt    = (FlashFileDownloadedEvent)result;
        Assert.Equal("photo.jpg", evt.Title);
        Assert.Equal("fset1", evt.FileSetId);
        Assert.Equal(2, evt.SceneType);
        Assert.Equal("http://example.com/photo.jpg", evt.FileUrl);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a flash_file_uploading notice.</summary>
    [Fact]
    public void ConvertNotice_FlashFileUploading()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "flash_file_uploading",
                Title      = "doc.pdf", FileSetId = "fset2", SceneType = 3
            };

        BotEvent                result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        FlashFileUploadingEvent evt    = (FlashFileUploadingEvent)result;
        Assert.Equal("doc.pdf", evt.Title);
        Assert.Equal("fset2", evt.FileSetId);
        Assert.Equal(3, evt.SceneType);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a flash_file_uploaded notice.</summary>
    [Fact]
    public void ConvertNotice_FlashFileUploaded()
    {
        OneBotEvent obEvent = new()
            {
                Time       = 1700000000, SelfId = 12345, PostType = "notice",
                NoticeType = "flash_file_uploaded",
                Title      = "doc.pdf", FileSetId = "fset2", SceneType = 3
            };

        BotEvent               result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        FlashFileUploadedEvent evt    = (FlashFileUploadedEvent)result;
        Assert.Equal("doc.pdf", evt.Title);
        Assert.Equal("fset2", evt.FileSetId);
        Assert.Equal(3, evt.SceneType);
    }

#endregion

#region Request Event Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a friend request.</summary>
    [Fact]
    public void ConvertRequest_Friend()
    {
        OneBotEvent obEvent = new()
            {
                Time        = 1700000000, SelfId = 12345, PostType = "request",
                RequestType = "friend",
                UserId      = 222, Comment = "Hi!", Flag = "flag123"
            };

        BotEvent           result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        FriendRequestEvent evt    = (FriendRequestEvent)result;
        Assert.Equal(222L, (long)evt.FromUserId);
        Assert.Equal("Hi!", evt.Comment);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-add request.</summary>
    [Fact]
    public void ConvertRequest_GroupAdd()
    {
        OneBotEvent obEvent = new()
            {
                Time        = 1700000000,
                SelfId      = 12345,
                PostType    = "request",
                RequestType = "group",
                SubType     = "add",
                GroupId     = 111,
                UserId      = 222,
                Comment     = "Let me in",
                Flag        = "groupflag"
            };

        BotEvent              result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupJoinRequestEvent evt    = (GroupJoinRequestEvent)result;
        Assert.Equal(111L, (long)evt.GroupId);
        Assert.Equal(GroupJoinNotificationType.JoinRequest, evt.JoinNotificationType);
        Assert.Equal(222L, evt.NotificationSeq);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-invite request.</summary>
    [Fact]
    public void ConvertRequest_GroupInvite()
    {
        OneBotEvent obEvent = new()
            {
                Time        = 1700000000, SelfId = 12345, PostType = "request",
                RequestType = "group", SubType   = "invite",
                GroupId     = 111, UserId        = 222, Flag = "inviteflag"
            };

        BotEvent             result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!)!;
        GroupInvitationEvent evt    = (GroupInvitationEvent)result;
        Assert.Equal(222L, (long)evt.InvitorId);
    }

#endregion

#region Meta Event Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a lifecycle-connect meta event.</summary>
    [Fact]
    public void ConvertMeta_Lifecycle_Connect()
    {
        OneBotEvent obEvent = new()
            {
                Time          = 1700000000, SelfId   = 12345, PostType = "meta_event",
                MetaEventType = "lifecycle", SubType = "connect"
            };

        BotEvent? result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!);
        Assert.IsType<ConnectedEvent>(result);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> returns null for heartbeat meta events.</summary>
    [Fact]
    public void ConvertMeta_Heartbeat_ReturnsNull()
    {
        OneBotEvent obEvent = new()
            {
                Time          = 1700000000, SelfId = 12345, PostType = "meta_event",
                MetaEventType = "heartbeat"
            };

        BotEvent? result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!);
        Assert.Null(result);
    }

#endregion

#region Edge Case Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> returns null for unknown post types.</summary>
    [Fact]
    public void ConvertUnknownPostType_ReturnsNull()
    {
        OneBotEvent obEvent = new()
            {
                Time = 1700000000, SelfId = 12345, PostType = "unknown_type"
            };

        BotEvent? result = EventConverter.ToSoraEvent(obEvent, TestConnectionId, null!);
        Assert.Null(result);
    }

#endregion
}