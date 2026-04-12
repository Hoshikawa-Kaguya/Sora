using Newtonsoft.Json.Linq;
using Sora.Adapter.Milky.Converter;
using Sora.Adapter.Milky.Models;
using Xunit;

namespace Sora.Tests.Unit.Milky;

/// <summary>Tests for <see cref="EventConverter" /> (Milky).</summary>
[Collection("Milky.Unit")]
[Trait("Category", "Unit")]
public class EventConverterTests
{
    private static readonly Guid TestConnectionId = Guid.NewGuid();

#region Message Event Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a friend message-receive event.</summary>
    [Fact]
    public void ConvertMessageReceive_Friend()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "message_receive",
                Data = JObject.Parse(
                    @"{
                ""message_scene"": ""friend"", ""peer_id"": 222, ""message_seq"": 888,
                ""sender_id"": 222, ""time"": 1700000000,
                ""segments"": [{""type"": ""text"", ""data"": {""text"": ""hi""}}],
                ""friend"": {""user_id"": 222, ""nickname"": ""Friend"", ""sex"": ""female"", ""qid"": """", ""remark"": ""MyFriend""}
            }")
            };

        BotEvent             result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        MessageReceivedEvent msg    = (MessageReceivedEvent)result;
        Assert.Equal(MessageSourceType.Friend, msg.Message.SourceType);
        Assert.Equal("Friend", msg.Sender.Nickname);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group message-receive event.</summary>
    [Fact]
    public void ConvertMessageReceive_Group()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "message_receive",
                Data = JObject.Parse(
                    @"{
                ""message_scene"": ""group"", ""peer_id"": 111, ""message_seq"": 999,
                ""sender_id"": 222, ""time"": 1700000000,
                ""segments"": [{""type"": ""text"", ""data"": {""text"": ""hello""}}],
                ""group"": {""group_id"": 111, ""group_name"": ""TestGroup"", ""member_count"": 10, ""max_member_count"": 200},
                ""group_member"": {""user_id"": 222, ""nickname"": ""User"", ""sex"": ""male"", ""group_id"": 111, ""card"": """", ""title"": """", ""level"": 1, ""role"": ""member"", ""join_time"": 0, ""last_sent_time"": 0}
            }")
            };

        BotEvent             result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        MessageReceivedEvent msg    = (MessageReceivedEvent)result;
        Assert.Equal(MessageSourceType.Group, msg.Message.SourceType);
        Assert.Equal(999L, (long)msg.Message.MessageId);
        Assert.Equal("hello", msg.Message.Body.GetText());
        Assert.Equal("TestGroup", msg.Group.GroupName);
    }

#endregion

#region Group Notice Event Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-admin-change event.</summary>
    [Fact]
    public void ConvertGroupAdminChange()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_admin_change",
                Data = JObject.Parse(@"{""group_id"": 111, ""user_id"": 222, ""operator_id"": 333, ""is_set"": true}")
            };

        BotEvent               result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        GroupAdminChangedEvent admin  = (GroupAdminChangedEvent)result;
        Assert.True(admin.IsSet);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-essence-change event.</summary>
    [Fact]
    public void ConvertGroupEssenceChange()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_essence_message_change",
                Data = JObject.Parse(@"{""group_id"": 111, ""message_seq"": 999, ""operator_id"": 333, ""is_set"": true}")
            };

        BotEvent                 result  = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        GroupEssenceChangedEvent essence = (GroupEssenceChangedEvent)result;
        Assert.True(essence.IsSet);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-file-upload event.</summary>
    [Fact]
    public void ConvertGroupFileUpload()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_file_upload",
                Data = JObject.Parse(
                    @"{""group_id"": 111, ""user_id"": 222, ""file_id"": ""f2"", ""file_name"": ""doc.pdf"", ""file_size"": 4096}")
            };

        BotEvent        result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        FileUploadEvent file   = (FileUploadEvent)result;
        Assert.Equal(MessageSourceType.Group, file.SourceType);
        Assert.Equal(111L, (long)file.GroupId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-member-decrease event.</summary>
    [Fact]
    public void ConvertGroupMemberDecrease()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_member_decrease",
                Data = JObject.Parse(@"{""group_id"": 111, ""user_id"": 222, ""operator_id"": 333}")
            };

        BotEvent        result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        MemberLeftEvent left   = (MemberLeftEvent)result;
        Assert.True(left.IsKicked);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-member-increase event.</summary>
    [Fact]
    public void ConvertGroupMemberIncrease()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_member_increase",
                Data = JObject.Parse(@"{""group_id"": 111, ""user_id"": 222, ""operator_id"": 333, ""invitor_id"": 0}")
            };

        BotEvent          result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        MemberJoinedEvent joined = (MemberJoinedEvent)result;
        Assert.Equal(111L, (long)joined.GroupId);
        Assert.Equal(222L, (long)joined.UserId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-mute event.</summary>
    [Fact]
    public void ConvertGroupMute()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_mute",
                Data = JObject.Parse(@"{""group_id"": 111, ""user_id"": 222, ""operator_id"": 333, ""duration"": 3600}")
            };

        BotEvent       result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        GroupMuteEvent mute   = (GroupMuteEvent)result;
        Assert.Equal(3600, mute.DurationSeconds);
        Assert.False(mute.IsWholeGroup);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-whole-mute event.</summary>
    [Fact]
    public void ConvertGroupWholeMute()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_whole_mute",
                Data = JObject.Parse(@"{""group_id"": 111, ""operator_id"": 333, ""is_mute"": true}")
            };

        BotEvent       result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        GroupMuteEvent mute   = (GroupMuteEvent)result;
        Assert.True(mute.IsWholeGroup);
        Assert.Equal(int.MaxValue, mute.DurationSeconds);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-name-change event.</summary>
    [Fact]
    public void ConvertGroupNameChange()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_name_change",
                Data = JObject.Parse(@"{""group_id"": 111, ""new_group_name"": ""NewName"", ""operator_id"": 333}")
            };

        BotEvent              result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        GroupNameChangedEvent name   = (GroupNameChangedEvent)result;
        Assert.Equal("NewName", name.NewName);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-nudge event.</summary>
    [Fact]
    public void ConvertGroupNudge()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_nudge",
                Data = JObject.Parse(
                    @"{""group_id"": 111, ""sender_id"": 222, ""receiver_id"": 333, ""display_action"": ""poked"", ""display_suffix"": """", ""display_action_img_url"": """"}")
            };

        BotEvent   result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        NudgeEvent nudge  = (NudgeEvent)result;
        Assert.Equal(MessageSourceType.Group, nudge.SourceType);
        Assert.Equal(111L, (long)nudge.GroupId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-reaction event.</summary>
    [Fact]
    public void ConvertGroupReaction()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_message_reaction",
                Data = JObject.Parse(
                    @"{""group_id"": 111, ""user_id"": 222, ""message_seq"": 999, ""face_id"": ""128"", ""reaction_type"": ""emoji"", ""is_add"": true}")
            };

        BotEvent           result   = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        GroupReactionEvent reaction = (GroupReactionEvent)result;
        Assert.Equal("128", reaction.FaceId);
        Assert.True(reaction.IsAdd);
    }

#endregion

#region Friend and Other Notice Event Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a friend-file-upload event.</summary>
    [Fact]
    public void ConvertFriendFileUpload()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "friend_file_upload",
                Data = JObject.Parse(
                    @"{""user_id"": 222, ""file_id"": ""f1"", ""file_name"": ""test.txt"", ""file_size"": 2048, ""file_hash"": ""abc"", ""is_self"": false}")
            };

        BotEvent        result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        FileUploadEvent file   = (FileUploadEvent)result;
        Assert.Equal(MessageSourceType.Friend, file.SourceType);
        Assert.Equal("test.txt", file.FileName);
        Assert.Equal(2048L, file.FileSize);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a friend-nudge event.</summary>
    [Fact]
    public void ConvertFriendNudge()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "friend_nudge",
                Data = JObject.Parse(
                    @"{""user_id"": 222, ""is_self_send"": false, ""is_self_receive"": true, ""display_action"": ""poked"", ""display_suffix"": ""you"", ""display_action_img_url"": """"}")
            };

        BotEvent   result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        NudgeEvent nudge  = (NudgeEvent)result;
        Assert.Equal(MessageSourceType.Friend, nudge.SourceType);
        Assert.Equal(222L, (long)nudge.SenderId);
        Assert.Equal(12345L, (long)nudge.ReceiverId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a message-recall event.</summary>
    [Fact]
    public void ConvertMessageRecall()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "message_recall",
                Data = JObject.Parse(
                    @"{""message_scene"": ""group"", ""peer_id"": 111, ""message_seq"": 999, ""sender_id"": 222, ""operator_id"": 333, ""display_suffix"": """"}")
            };

        BotEvent            result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        MessageDeletedEvent del    = (MessageDeletedEvent)result;
        Assert.Equal(MessageSourceType.Group, del.SourceType);
        Assert.Equal(999L, (long)del.MessageId);
        Assert.Equal(333L, (long)del.OperatorId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a peer-pin-change event.</summary>
    [Fact]
    public void ConvertPeerPinChange()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "peer_pin_change",
                Data = JObject.Parse(@"{""peer_id"": 111, ""message_scene"": ""group"", ""is_pinned"": true}")
            };

        BotEvent            result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        PeerPinChangedEvent pin    = (PeerPinChangedEvent)result;
        Assert.Equal(111L, pin.PeerId);
        Assert.Equal("group", pin.MessageScene);
        Assert.True(pin.IsPinned);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a bot-offline event.</summary>
    [Fact]
    public void ConvertBotOffline()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "bot_offline",
                Data = JObject.Parse(@"{""reason"": ""kicked""}")
            };

        BotEvent          result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        DisconnectedEvent disc   = (DisconnectedEvent)result;
        Assert.Equal("kicked", disc.Reason);
    }

#endregion

#region Request Event Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a friend-request event.</summary>
    [Fact]
    public void ConvertFriendRequest()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "friend_request",
                Data = JObject.Parse(
                    @"{""initiator_id"": 222, ""initiator_uid"": ""uid123"", ""comment"": ""Hello"", ""via"": ""search""}")
            };

        BotEvent           result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        FriendRequestEvent req    = (FriendRequestEvent)result;
        Assert.Equal(222L, (long)req.FromUserId);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-join-request event.</summary>
    [Fact]
    public void ConvertGroupJoinRequest()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_join_request",
                Data = JObject.Parse(
                    @"{""notification_seq"": 501, ""group_id"": 111, ""initiator_id"": 222, ""comment"": ""Please add me"", ""is_filtered"": true}")
            };

        BotEvent              result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        GroupJoinRequestEvent req    = (GroupJoinRequestEvent)result;
        Assert.Equal(111L, (long)req.GroupId);
        Assert.Equal(222L, (long)req.FromUserId);
        Assert.Equal(501L, req.NotificationSeq);
        Assert.Equal(GroupJoinNotificationType.JoinRequest, req.JoinNotificationType);
        Assert.True(req.IsFiltered);
        Assert.Equal("Please add me", req.Comment);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-invited-join-request event.</summary>
    [Fact]
    public void ConvertGroupInvitedJoinRequest()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_invited_join_request",
                Data = JObject.Parse(@"{""notification_seq"": 502, ""group_id"": 111, ""initiator_id"": 333, ""target_user_id"": 444}")
            };

        BotEvent              result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        GroupJoinRequestEvent req    = (GroupJoinRequestEvent)result;
        Assert.Equal(111L, (long)req.GroupId);
        Assert.Equal(444L, (long)req.FromUserId);
        Assert.Equal(502L, req.NotificationSeq);
        Assert.Equal(GroupJoinNotificationType.InvitedJoinRequest, req.JoinNotificationType);
        Assert.Equal("", req.Comment);
    }

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> converts a group-invitation event.</summary>
    [Fact]
    public void ConvertGroupInvitation()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "group_invitation",
                Data = JObject.Parse(@"{""invitation_seq"": 601, ""group_id"": 111, ""source_group_id"": 999, ""initiator_id"": 333}")
            };

        BotEvent             result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!)!;
        GroupInvitationEvent inv    = (GroupInvitationEvent)result;
        Assert.Equal(111L, (long)inv.GroupId);
        Assert.Equal(333L, (long)inv.InvitorId);
        Assert.Equal(999L, (long)inv.SourceGroupId);
        Assert.Equal(601L, inv.InvitationSeq);
    }

#endregion

#region Edge Case Tests

    /// <summary>Verifies <see cref="EventConverter.ToSoraEvent" /> returns null for unknown event types.</summary>
    [Fact]
    public void ConvertUnknownEventType_ReturnsNull()
    {
        MilkyEvent evt = new()
            {
                Time = 1700000000, SelfId = 12345, EventType = "some_future_event",
                Data = JObject.Parse(@"{}")
            };

        BotEvent? result = EventConverter.ToSoraEvent(evt, TestConnectionId, null!);
        Assert.Null(result);
    }

#endregion
}