using Mapster;
using Sora.Adapter.Milky.Models;

namespace Sora.Adapter.Milky.Converter;

/// <summary>
///     Converts Milky events to Sora BotEvent types.
/// </summary>
internal static class EventConverter
{
    private static readonly Lazy<ILogger> LoggerLazy = new(() => SoraLogger.CreateLogger(typeof(EventConverter).FullName!));
    private static          ILogger       Logger => LoggerLazy.Value;

#region Converter Entry

    /// <summary>Converts a MilkyEvent to a Sora BotEvent.</summary>
    public static BotEvent? ToSoraEvent(MilkyEvent milkyEvent, Guid connectionId, IBotApi api)
    {
        EventBase baseProps = new(
            connectionId,
            milkyEvent.SelfId,
            DateTimeOffset.FromUnixTimeSeconds(milkyEvent.Time).LocalDateTime,
            api);

        return milkyEvent.EventType switch
                   {
                       "message_receive" => ConvertMessageReceive(milkyEvent, baseProps),
                       "message_recall" => ConvertMessageRecall(milkyEvent, baseProps),
                       "bot_offline" => ConvertBotOffline(milkyEvent, baseProps),
                       "friend_request" => ConvertFriendRequest(milkyEvent, baseProps),
                       "group_join_request" => ConvertGroupJoinRequest(milkyEvent, baseProps),
                       "group_invited_join_request" => ConvertGroupInvitedJoinRequest(milkyEvent, baseProps),
                       "group_invitation" => ConvertGroupInvitation(milkyEvent, baseProps),
                       "friend_nudge" => ConvertFriendNudge(milkyEvent, baseProps),
                       "group_nudge" => ConvertGroupNudge(milkyEvent, baseProps),
                       "friend_file_upload" => ConvertFriendFileUpload(milkyEvent, baseProps),
                       "group_file_upload" => ConvertGroupFileUpload(milkyEvent, baseProps),
                       "group_admin_change" => ConvertGroupAdminChange(milkyEvent, baseProps),
                       "group_essence_message_change" => ConvertGroupEssenceChange(milkyEvent, baseProps),
                       "group_member_increase" => ConvertMemberIncrease(milkyEvent, baseProps),
                       "group_member_decrease" => ConvertMemberDecrease(milkyEvent, baseProps),
                       "group_name_change" => ConvertGroupNameChange(milkyEvent, baseProps),
                       "group_message_reaction" => ConvertGroupReaction(milkyEvent, baseProps),
                       "group_mute" => ConvertGroupMute(milkyEvent, baseProps),
                       "group_whole_mute" => ConvertGroupWholeMute(milkyEvent, baseProps),
                       "peer_pin_change" => ConvertPeerPinChange(milkyEvent, baseProps),
                       _ => LogAndReturnNull("Unknown Milky event type: {EventType}", milkyEvent.EventType)
                   };
    }

#endregion

#region Message Events

    /// <summary>Converts a Milky message_receive event to a <see cref="MessageReceivedEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertMessageReceive(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyMessage? msg = milkyEvent.Data?.ToObject<MilkyMessage>();
        if (msg is null)
        {
            Logger.LogWarning("Milky message_receive event data could not be deserialized");
            return null;
        }

        MessageSourceType sourceType = (msg.MessageScene ?? "").Adapt<MessageSourceType>();
        if (sourceType == (MessageSourceType)(-1))
        {
            Logger.LogWarning("Invalid Milky message source type: {MessageScene}", msg.MessageScene ?? "(null)");
            return null;
        }

        MessageContext context = new()
            {
                MessageId  = msg.MessageSeq,
                SourceType = sourceType,
                GroupId    = sourceType == MessageSourceType.Group ? (GroupId)msg.PeerId : default,
                SenderId   = msg.SenderId,
                Time       = DateTimeOffset.FromUnixTimeSeconds(msg.Time).LocalDateTime,
                Body       = MessageConverter.ToMessageBody(msg.Segments)
            };

        if (context.Body.Count == 0)
        {
            Logger.LogWarning("Milky message_receive event has empty body, discarding");
            return null;
        }

        UserInfo? sender = sourceType switch
                               {
                                   MessageSourceType.Friend or MessageSourceType.Temp when msg.Friend is not null =>
                                       msg.Friend.Adapt<UserInfo>(),
                                   MessageSourceType.Group when msg.GroupMember is not null =>
                                       msg.GroupMember.Adapt<UserInfo>(),
                                   _ => null
                               };

        GroupInfo? group = msg.Group?.Adapt<GroupInfo>();

        GroupMemberInfo? member = msg.GroupMember?.Adapt<GroupMemberInfo>();

        return new MessageReceivedEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                Message      = context,
                Sender       = sender ?? new UserInfo(),
                Group        = group ?? new GroupInfo(),
                Member       = member ?? new GroupMemberInfo()
            };
    }

    /// <summary>Converts a Milky message_recall event to a <see cref="MessageDeletedEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertMessageRecall(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyMessageRecallData? d = Deserialize<MilkyMessageRecallData>(milkyEvent);
        if (d is null) return null;
        MessageSourceType sourceType = (d.MessageScene ?? "").Adapt<MessageSourceType>();
        if (sourceType == (MessageSourceType)(-1))
        {
            Logger.LogWarning("Invalid Milky message_recall source type: {MessageScene}", d.MessageScene ?? "(null)");
            return null;
        }

        return new MessageDeletedEvent
            {
                ConnectionId  = baseProps.ConnectionId,
                SelfId        = baseProps.SelfId,
                Time          = baseProps.Time,
                Api           = baseProps.Api,
                MessageId     = d.MessageSeq,
                SenderId      = d.SenderId,
                OperatorId    = d.OperatorId,
                SourceType    = sourceType,
                GroupId       = sourceType == MessageSourceType.Group ? (GroupId)d.PeerId : default,
                DisplaySuffix = d.DisplaySuffix ?? ""
            };
    }

#endregion

#region Request Events

    /// <summary>Converts a Milky friend_request event to a <see cref="FriendRequestEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertFriendRequest(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyFriendRequestData? d = Deserialize<MilkyFriendRequestData>(milkyEvent);
        if (d is null) return null;
        return new FriendRequestEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                FromUserId   = d.InitiatorId,
                Comment      = d.Comment ?? "",
                Via          = d.Via ?? ""
            };
    }

    /// <summary>Converts a Milky group_join_request event to a <see cref="GroupJoinRequestEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupJoinRequest(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupJoinRequestData? d = Deserialize<MilkyGroupJoinRequestData>(milkyEvent);
        if (d is null) return null;
        return new GroupJoinRequestEvent
            {
                ConnectionId         = baseProps.ConnectionId,
                SelfId               = baseProps.SelfId,
                Time                 = baseProps.Time,
                Api                  = baseProps.Api,
                GroupId              = d.GroupId,
                FromUserId           = d.InitiatorId,
                NotificationSeq      = d.NotificationSeq,
                JoinNotificationType = GroupJoinNotificationType.JoinRequest,
                IsFiltered           = d.IsFiltered,
                Comment              = d.Comment ?? ""
            };
    }

    /// <summary>Converts a Milky group_invited_join_request event to a <see cref="GroupJoinRequestEvent" /> (invited).</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupInvitedJoinRequest(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupInvitedJoinRequestData? d = Deserialize<MilkyGroupInvitedJoinRequestData>(milkyEvent);
        if (d is null) return null;
        return new GroupJoinRequestEvent
            {
                ConnectionId         = baseProps.ConnectionId,
                SelfId               = baseProps.SelfId,
                Time                 = baseProps.Time,
                Api                  = baseProps.Api,
                GroupId              = d.GroupId,
                FromUserId           = d.TargetUserId,
                NotificationSeq      = d.NotificationSeq,
                JoinNotificationType = GroupJoinNotificationType.InvitedJoinRequest,
                Comment              = ""
            };
    }

    /// <summary>Converts a Milky group_invitation event to a <see cref="GroupInvitationEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupInvitation(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupInvitationData? d = Deserialize<MilkyGroupInvitationData>(milkyEvent);
        if (d is null) return null;
        return new GroupInvitationEvent
            {
                ConnectionId  = baseProps.ConnectionId,
                SelfId        = baseProps.SelfId,
                Time          = baseProps.Time,
                Api           = baseProps.Api,
                GroupId       = d.GroupId,
                InvitorId     = d.InitiatorId,
                SourceGroupId = d.SourceGroupId,
                InvitationSeq = d.InvitationSeq
            };
    }

#endregion

#region Friend Notice Events

    /// <summary>Converts a Milky friend_nudge event to a <see cref="NudgeEvent" /> (friend).</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertFriendNudge(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyFriendNudgeData? d = Deserialize<MilkyFriendNudgeData>(milkyEvent);
        if (d is null) return null;
        return new NudgeEvent
            {
                ConnectionId   = baseProps.ConnectionId,
                SelfId         = baseProps.SelfId,
                Time           = baseProps.Time,
                Api            = baseProps.Api,
                SourceType     = MessageSourceType.Friend,
                SenderId       = d.IsSelfSend ? baseProps.SelfId : d.UserId,
                ReceiverId     = d.IsSelfReceive ? baseProps.SelfId : d.UserId,
                ActionText     = d.DisplayAction ?? "",
                SuffixText     = d.DisplaySuffix ?? "",
                ActionImageUrl = d.DisplayActionImgUrl ?? ""
            };
    }

    /// <summary>Converts a Milky friend_file_upload event to a <see cref="FileUploadEvent" /> (friend).</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertFriendFileUpload(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyFriendFileUploadData? d = Deserialize<MilkyFriendFileUploadData>(milkyEvent);
        if (d is null) return null;
        return new FileUploadEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                SourceType   = MessageSourceType.Friend,
                UserId       = d.UserId,
                FileId       = d.FileId ?? "",
                FileName     = d.FileName ?? "",
                FileSize     = d.FileSize,
                FileHash     = d.FileHash ?? "",
                IsSelfSent   = d.IsSelf
            };
    }

#endregion

#region Group Notice Events

    /// <summary>Converts a Milky group_admin_change event to a <see cref="GroupAdminChangedEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupAdminChange(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupAdminChangeData? d = Deserialize<MilkyGroupAdminChangeData>(milkyEvent);
        if (d is null) return null;
        return new GroupAdminChangedEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                GroupId      = d.GroupId,
                UserId       = d.UserId,
                OperatorId   = d.OperatorId,
                IsSet        = d.IsSet
            };
    }

    /// <summary>Converts a Milky group_essence_message_change event to a <see cref="GroupEssenceChangedEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupEssenceChange(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupEssenceMessageChangeData? d = Deserialize<MilkyGroupEssenceMessageChangeData>(milkyEvent);
        if (d is null) return null;
        return new GroupEssenceChangedEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                GroupId      = d.GroupId,
                MessageId    = d.MessageSeq,
                OperatorId   = d.OperatorId,
                IsSet        = d.IsSet
            };
    }

    /// <summary>Converts a Milky group_file_upload event to a <see cref="FileUploadEvent" /> (group).</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupFileUpload(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupFileUploadData? d = Deserialize<MilkyGroupFileUploadData>(milkyEvent);
        if (d is null) return null;
        return new FileUploadEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                SourceType   = MessageSourceType.Group,
                GroupId      = d.GroupId,
                UserId       = d.UserId,
                FileId       = d.FileId ?? "",
                FileName     = d.FileName ?? "",
                FileSize     = d.FileSize
            };
    }

    /// <summary>Converts a Milky group_mute event to a <see cref="GroupMuteEvent" /> (individual).</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupMute(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupMuteData? d = Deserialize<MilkyGroupMuteData>(milkyEvent);
        if (d is null) return null;
        return new GroupMuteEvent
            {
                ConnectionId    = baseProps.ConnectionId,
                SelfId          = baseProps.SelfId,
                Time            = baseProps.Time,
                Api             = baseProps.Api,
                GroupId         = d.GroupId,
                UserId          = d.UserId,
                OperatorId      = d.OperatorId,
                DurationSeconds = d.Duration,
                IsWholeGroup    = false
            };
    }

    /// <summary>Converts a Milky group_whole_mute event to a <see cref="GroupMuteEvent" /> (whole group).</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupWholeMute(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupWholeMuteData? d = Deserialize<MilkyGroupWholeMuteData>(milkyEvent);
        if (d is null) return null;
        return new GroupMuteEvent
            {
                ConnectionId    = baseProps.ConnectionId,
                SelfId          = baseProps.SelfId,
                Time            = baseProps.Time,
                Api             = baseProps.Api,
                GroupId         = d.GroupId,
                OperatorId      = d.OperatorId,
                DurationSeconds = d.IsMute ? int.MaxValue : 0,
                IsWholeGroup    = true
            };
    }

    /// <summary>Converts a Milky group_name_change event to a <see cref="GroupNameChangedEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupNameChange(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupNameChangeData? d = Deserialize<MilkyGroupNameChangeData>(milkyEvent);
        if (d is null) return null;
        return new GroupNameChangedEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                GroupId      = d.GroupId,
                NewName      = d.NewGroupName ?? "",
                OperatorId   = d.OperatorId
            };
    }

    /// <summary>Converts a Milky group_nudge event to a <see cref="NudgeEvent" /> (group).</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupNudge(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupNudgeData? d = Deserialize<MilkyGroupNudgeData>(milkyEvent);
        if (d is null) return null;
        return new NudgeEvent
            {
                ConnectionId   = baseProps.ConnectionId,
                SelfId         = baseProps.SelfId,
                Time           = baseProps.Time,
                Api            = baseProps.Api,
                SourceType     = MessageSourceType.Group,
                GroupId        = d.GroupId,
                SenderId       = d.SenderId,
                ReceiverId     = d.ReceiverId,
                ActionText     = d.DisplayAction ?? "",
                SuffixText     = d.DisplaySuffix ?? "",
                ActionImageUrl = d.DisplayActionImgUrl ?? ""
            };
    }

    /// <summary>Converts a Milky group_message_reaction event to a <see cref="GroupReactionEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertGroupReaction(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupMessageReactionData? d = Deserialize<MilkyGroupMessageReactionData>(milkyEvent);
        if (d is null || string.IsNullOrEmpty(d.ReactionType)) return null;
        return new GroupReactionEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                GroupId      = d.GroupId,
                UserId       = d.UserId,
                MessageId    = d.MessageSeq,
                FaceId       = d.FaceId ?? "",
                ReactionType = d.ReactionType,
                IsAdd        = d.IsAdd
            };
    }

    /// <summary>Converts a Milky group_member_increase event to a <see cref="MemberJoinedEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertMemberIncrease(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupMemberIncreaseData? d = Deserialize<MilkyGroupMemberIncreaseData>(milkyEvent);
        if (d is null) return null;
        return new MemberJoinedEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                GroupId      = d.GroupId,
                UserId       = d.UserId,
                OperatorId   = d.OperatorId is > 0 ? (UserId)d.OperatorId.Value : null,
                InvitorId    = d.InvitorId is > 0 ? (UserId)d.InvitorId.Value : null
            };
    }

    /// <summary>Converts a Milky group_member_decrease event to a <see cref="MemberLeftEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertMemberDecrease(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyGroupMemberDecreaseData? d = Deserialize<MilkyGroupMemberDecreaseData>(milkyEvent);
        if (d is null) return null;
        return new MemberLeftEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                GroupId      = d.GroupId,
                UserId       = d.UserId,
                OperatorId   = d.OperatorId is > 0 ? (UserId)d.OperatorId.Value : null,
                IsKicked     = d.OperatorId is > 0
            };
    }

    /// <summary>Converts a Milky peer_pin_change event to a <see cref="PeerPinChangedEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertPeerPinChange(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyPeerPinChangeData? d = Deserialize<MilkyPeerPinChangeData>(milkyEvent);
        if (d is null) return null;
        return new PeerPinChangedEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                MessageScene = d.MessageScene ?? "",
                PeerId       = d.PeerId,
                IsPinned     = d.IsPinned
            };
    }

#endregion

#region Meta Events

    /// <summary>Converts a Milky bot_offline event to a <see cref="DisconnectedEvent" />.</summary>
    /// <param name="milkyEvent">The raw Milky event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event.</returns>
    private static BotEvent ConvertBotOffline(MilkyEvent milkyEvent, EventBase baseProps)
    {
        MilkyBotOfflineData? d = Deserialize<MilkyBotOfflineData>(milkyEvent);
        return new DisconnectedEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                Reason       = d?.Reason ?? "Bot offline"
            };
    }

#endregion

#region Helpers

    /// <summary>Deserializes the event data to the specified type.</summary>
    /// <typeparam name="T">The target type to deserialize to.</typeparam>
    /// <param name="milkyEvent">The Milky event containing the data.</param>
    /// <returns>The deserialized data, or null if deserialization fails.</returns>
    private static T? Deserialize<T>(MilkyEvent milkyEvent) where T : class
    {
        T? result = milkyEvent.Data?.ToObject<T>();
        if (result is null)
            Logger.LogWarning(
                "Milky {EventType} event data could not be deserialized to {TargetType}",
                milkyEvent.EventType,
                typeof(T).Name);
        return result;
    }

    /// <summary>Logs a warning for unknown event types and returns null for the switch expression.</summary>
    private static BotEvent? LogAndReturnNull(string message, string? eventType)
    {
        Logger.LogWarning(message, eventType);
        return null;
    }

    /// <summary>Common base properties shared by all converted events.</summary>
    private readonly record struct EventBase(Guid ConnectionId, UserId SelfId, DateTime Time, IBotApi Api);

#endregion
}