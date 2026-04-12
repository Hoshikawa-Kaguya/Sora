using Mapster;
using Newtonsoft.Json.Linq;
using Sora.Adapter.OneBot11.Events;
using Sora.Adapter.OneBot11.Models;

namespace Sora.Adapter.OneBot11.Converter;

/// <summary>
///     Converts OneBot v11 events to Sora BotEvent types.
/// </summary>
internal static class EventConverter
{
    private static readonly Lazy<ILogger> LoggerLazy = new(() => SoraLogger.CreateLogger(typeof(EventConverter).FullName!));
    private static          ILogger       Logger => LoggerLazy.Value;

#region Public API

    /// <summary>Converts an OB11 event model to a Sora BotEvent.</summary>
    public static BotEvent? ToSoraEvent(OneBotEvent oneBotEvent, Guid connectionId, IBotApi api)
    {
        EventBase baseProps = new(
            connectionId,
            oneBotEvent.SelfId,
            DateTimeOffset.FromUnixTimeSeconds(oneBotEvent.Time).LocalDateTime,
            api);

        return oneBotEvent.PostType switch
                   {
                       "message"    => ConvertMessageEvent(oneBotEvent, baseProps),
                       "notice"     => ConvertNoticeEvent(oneBotEvent, baseProps),
                       "request"    => ConvertRequestEvent(oneBotEvent, baseProps),
                       "meta_event" => ConvertMetaEvent(oneBotEvent, baseProps),
                       _            => LogAndReturnNull("Unknown OB11 post type: {PostType}", oneBotEvent.PostType)
                   };
    }

#endregion

#region Message Events

    /// <summary>Converts an OB11 message event to a <see cref="MessageReceivedEvent" />.</summary>
    /// <param name="oneBotEvent">The raw OB11 event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if conversion fails.</returns>
    private static BotEvent? ConvertMessageEvent(OneBotEvent oneBotEvent, EventBase baseProps)
    {
        MessageBody body = MessageConverter.ToMessageBody(oneBotEvent.Message);
        if (body.Count == 0)
        {
            Logger.LogWarning("OB11 message event has empty body, discarding");
            return null;
        }

        MessageSourceType sourceType = oneBotEvent.MessageType switch
                                           {
                                               "group"   => MessageSourceType.Group,
                                               "private" => MessageSourceType.Friend,
                                               _         => (MessageSourceType)(-1)
                                           };
        if (sourceType == (MessageSourceType)(-1))
        {
            Logger.LogWarning("Invalid OB11 message source type: {MessageType}", oneBotEvent.MessageType ?? "(null)");
            return null;
        }

        MessageContext msg = new()
            {
                MessageId  = oneBotEvent.MessageId,
                SourceType = sourceType,
                GroupId    = sourceType == MessageSourceType.Group ? (GroupId)oneBotEvent.GroupId : default,
                SenderId   = oneBotEvent.UserId,
                Time       = baseProps.Time,
                Body       = body
            };

        UserInfo sender = oneBotEvent.Sender is not null
            ? oneBotEvent.Sender.Adapt<UserInfo>()
            : new UserInfo { UserId = oneBotEvent.UserId };

        GroupInfo? group = sourceType == MessageSourceType.Group
            ? new GroupInfo { GroupId = oneBotEvent.GroupId }
            : null;

        GroupMemberInfo? member = sourceType == MessageSourceType.Group && oneBotEvent.Sender is not null
            ? oneBotEvent.Sender.Adapt<GroupMemberInfo>() with { GroupId = oneBotEvent.GroupId }
            : null;

        return new MessageReceivedEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                Message      = msg,
                Sender       = sender,
                Group        = group ?? new GroupInfo(),
                Member       = member ?? new GroupMemberInfo()
            };
    }

#endregion

#region Notice Events

    /// <summary>Converts an OB11 notice event to the appropriate Sora event.</summary>
    /// <param name="oneBotEvent">The raw OB11 event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if the notice type is unsupported.</returns>
    private static BotEvent? ConvertNoticeEvent(OneBotEvent oneBotEvent, EventBase baseProps)
    {
        return oneBotEvent.NoticeType switch
                   {
                       "group_increase" => new MemberJoinedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               GroupId      = oneBotEvent.GroupId,
                               UserId       = oneBotEvent.UserId,
                               SubType      = oneBotEvent.SubType ?? "",
                               OperatorId = oneBotEvent.SubType == "approve"
                                   ? (UserId)oneBotEvent.OperatorId
                                   : default,
                               InvitorId = oneBotEvent.SubType == "invite"
                                   ? (UserId)oneBotEvent.OperatorId
                                   : default
                           },
                       "group_decrease" => new MemberLeftEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               GroupId      = oneBotEvent.GroupId,
                               UserId       = oneBotEvent.UserId,
                               SubType      = oneBotEvent.SubType ?? "",
                               OperatorId   = (UserId)oneBotEvent.OperatorId,
                               IsKicked     = oneBotEvent.SubType is "kick" or "kick_me"
                           },
                       "group_admin" => new GroupAdminChangedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               GroupId      = oneBotEvent.GroupId,
                               UserId       = oneBotEvent.UserId,
                               OperatorId   = default,
                               IsSet        = oneBotEvent.SubType == "set"
                           },
                       "group_ban" => new GroupMuteEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               GroupId      = oneBotEvent.GroupId,
                               UserId       = oneBotEvent.UserId,
                               OperatorId   = oneBotEvent.OperatorId,
                               IsWholeGroup = oneBotEvent.UserId == 0,
                               // 全群禁言没有时间长度，DurationSeconds 仅对个人禁言有意义
                               DurationSeconds = oneBotEvent.UserId == 0
                                   ? 0
                                   : oneBotEvent.SubType == "lift_ban"
                                       ? 0
                                       : (int)oneBotEvent.Duration
                           },
                       "group_upload" => new FileUploadEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               SourceType   = MessageSourceType.Group,
                               GroupId      = oneBotEvent.GroupId,
                               UserId       = oneBotEvent.UserId,
                               FileId       = oneBotEvent.File?.Id ?? "",
                               FileName     = oneBotEvent.File?.Name ?? "",
                               FileSize     = oneBotEvent.File?.Size ?? 0
                           },
                       "group_recall" => new MessageDeletedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               MessageId    = oneBotEvent.MessageId,
                               SenderId     = oneBotEvent.UserId,
                               OperatorId   = oneBotEvent.OperatorId,
                               SourceType   = MessageSourceType.Group,
                               GroupId      = oneBotEvent.GroupId
                           },
                       "friend_recall" => new MessageDeletedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               MessageId    = oneBotEvent.MessageId,
                               SenderId     = oneBotEvent.UserId,
                               OperatorId   = oneBotEvent.UserId,
                               SourceType   = MessageSourceType.Friend,
                               GroupId      = default
                           },
                       "friend_add" => new FriendAddedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               UserId       = oneBotEvent.UserId
                           },
                       "notify" when oneBotEvent.SubType == "poke" => new NudgeEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               SourceType = oneBotEvent.GroupId != 0
                                   ? MessageSourceType.Group
                                   : MessageSourceType.Friend,
                               GroupId = oneBotEvent.GroupId != 0
                                   ? (GroupId)oneBotEvent.GroupId
                                   : default,
                               SenderId       = oneBotEvent.UserId,
                               ReceiverId     = oneBotEvent.TargetId,
                               ActionText     = TryGetRawInfoField(oneBotEvent.RawInfo, "action_text"),
                               ActionImageUrl = TryGetRawInfoField(oneBotEvent.RawInfo, "action_img_url"),
                               SuffixText     = TryGetRawInfoField(oneBotEvent.RawInfo, "suffix_text")
                           },
                       "essence" => new GroupEssenceChangedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               GroupId      = oneBotEvent.GroupId,
                               MessageId    = oneBotEvent.MessageId,
                               OperatorId   = oneBotEvent.OperatorId,
                               SenderId     = oneBotEvent.SenderId,
                               IsSet        = oneBotEvent.SubType == "add"
                           },
                       "group_msg_emoji_like" => ConvertGroupReactionEvent(oneBotEvent, baseProps),
                       "group_card" => new GroupCardChangedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               GroupId      = oneBotEvent.GroupId,
                               UserId       = oneBotEvent.UserId,
                               CardNew      = oneBotEvent.CardNew ?? "",
                               CardOld      = oneBotEvent.CardOld ?? ""
                           },
                       "group_dismiss" => new GroupDismissedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               GroupId      = oneBotEvent.GroupId,
                               OperatorId   = oneBotEvent.UserId
                           },
                       "notify" when oneBotEvent.SubType == "title" => new GroupTitleChangedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               GroupId      = oneBotEvent.GroupId,
                               UserId       = oneBotEvent.UserId,
                               Title        = oneBotEvent.Title ?? ""
                           },
                       "notify" when oneBotEvent.SubType == "profile_like" => new ProfileLikedEvent
                           {
                               ConnectionId     = baseProps.ConnectionId,
                               SelfId           = baseProps.SelfId,
                               Time             = baseProps.Time,
                               Api              = baseProps.Api,
                               SenderId         = oneBotEvent.OperatorId,
                               OperatorNickname = oneBotEvent.OperatorNick ?? "",
                               Times            = oneBotEvent.Times
                           },
                       "notify" when oneBotEvent.SubType == "poke_recall" => oneBotEvent.GroupId != 0
                           ? new GroupPokeRecallEvent
                               {
                                   ConnectionId = baseProps.ConnectionId,
                                   SelfId       = baseProps.SelfId,
                                   Time         = baseProps.Time,
                                   Api          = baseProps.Api,
                                   GroupId      = oneBotEvent.GroupId,
                                   UserId       = oneBotEvent.UserId
                               }
                           : new FriendPokeRecallEvent
                               {
                                   ConnectionId = baseProps.ConnectionId,
                                   SelfId       = baseProps.SelfId,
                                   Time         = baseProps.Time,
                                   Api          = baseProps.Api,
                                   UserId       = oneBotEvent.UserId
                               },
                       "flash_file_downloading" => new FlashFileDownloadingEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               Title        = oneBotEvent.Title ?? "",
                               FileSetId    = oneBotEvent.FileSetId ?? "",
                               SceneType    = oneBotEvent.SceneType
                           },
                       "flash_file_downloaded" => new FlashFileDownloadedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               Title        = oneBotEvent.Title ?? "",
                               FileSetId    = oneBotEvent.FileSetId ?? "",
                               SceneType    = oneBotEvent.SceneType,
                               FileUrl      = oneBotEvent.FileUrl ?? ""
                           },
                       "flash_file_uploading" => new FlashFileUploadingEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               Title        = oneBotEvent.Title ?? "",
                               FileSetId    = oneBotEvent.FileSetId ?? "",
                               SceneType    = oneBotEvent.SceneType
                           },
                       "flash_file_uploaded" => new FlashFileUploadedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               Title        = oneBotEvent.Title ?? "",
                               FileSetId    = oneBotEvent.FileSetId ?? "",
                               SceneType    = oneBotEvent.SceneType
                           },
                       _ => LogAndReturnNull("Unknown OB11 notice type: {NoticeType}", oneBotEvent.NoticeType)
                   };
    }

    /// <summary>Converts an OB11 group_msg_emoji_like notice to a <see cref="GroupReactionEvent" />.</summary>
    /// <param name="oneBotEvent">The raw OB11 event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if required fields are missing.</returns>
    private static BotEvent ConvertGroupReactionEvent(OneBotEvent oneBotEvent, EventBase baseProps)
    {
        // Extract first emoji_id from likes array if available
        string faceId = "";
        if (oneBotEvent.Likes is JArray { Count: > 0 } likesArr)
        {
            JToken first = likesArr[0];
            faceId = first["emoji_id"]?.ToString() ?? "";
        }

        return new GroupReactionEvent
            {
                ConnectionId = baseProps.ConnectionId,
                SelfId       = baseProps.SelfId,
                Time         = baseProps.Time,
                Api          = baseProps.Api,
                GroupId      = oneBotEvent.GroupId,
                UserId       = oneBotEvent.UserId,
                MessageId    = oneBotEvent.MessageId,
                FaceId       = faceId,
                IsAdd        = oneBotEvent.IsAdd
            };
    }

#endregion

#region Request Events

    /// <summary>Converts an OB11 request event to the appropriate Sora event.</summary>
    /// <param name="oneBotEvent">The raw OB11 event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if the request type is unsupported.</returns>
    private static BotEvent? ConvertRequestEvent(OneBotEvent oneBotEvent, EventBase baseProps)
    {
        return oneBotEvent.RequestType switch
                   {
                       "friend" => new FriendRequestEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api,
                               FromUserId   = oneBotEvent.UserId,
                               Comment      = oneBotEvent.Comment ?? "",
                               Via          = oneBotEvent.Via ?? ""
                           },
                       "group" when oneBotEvent.SubType == "add" => new GroupJoinRequestEvent
                           {
                               ConnectionId    = baseProps.ConnectionId,
                               SelfId          = baseProps.SelfId,
                               Time            = baseProps.Time,
                               Api             = baseProps.Api,
                               GroupId         = oneBotEvent.GroupId,
                               FromUserId      = oneBotEvent.UserId,
                               NotificationSeq = oneBotEvent.UserId,
                               InvitorId       = oneBotEvent.InvitorId != 0 ? (UserId)oneBotEvent.InvitorId : default,
                               JoinNotificationType =
                                   GroupJoinNotificationType.JoinRequest,
                               Comment = oneBotEvent.Comment ?? ""
                           },
                       "group" when oneBotEvent.SubType == "invite" => new GroupInvitationEvent
                           {
                               ConnectionId  = baseProps.ConnectionId,
                               SelfId        = baseProps.SelfId,
                               Time          = baseProps.Time,
                               Api           = baseProps.Api,
                               GroupId       = oneBotEvent.GroupId,
                               InvitorId     = oneBotEvent.UserId,
                               InvitationSeq = oneBotEvent.UserId,
                               SourceGroupId = oneBotEvent.SourceGroupId != 0 ? (GroupId)oneBotEvent.SourceGroupId : default
                           },
                       _ => LogAndReturnNull("Unknown OB11 request type: {RequestType}", oneBotEvent.RequestType)
                   };
    }

#endregion

#region Meta Events

    /// <summary>Converts an OB11 meta event to the appropriate Sora event.</summary>
    /// <param name="oneBotEvent">The raw OB11 event.</param>
    /// <param name="baseProps">The common event base properties.</param>
    /// <returns>The converted event, or null if the meta event type is unsupported.</returns>
    private static BotEvent? ConvertMetaEvent(OneBotEvent oneBotEvent, EventBase baseProps)
    {
        return oneBotEvent.MetaEventType switch
                   {
                       "lifecycle" when oneBotEvent.SubType == "connect" => new ConnectedEvent
                           {
                               ConnectionId = baseProps.ConnectionId,
                               SelfId       = baseProps.SelfId,
                               Time         = baseProps.Time,
                               Api          = baseProps.Api
                           },
                       _ => null // heartbeat etc. handled internally
                   };
    }

#endregion

#region Helpers

    /// <summary>Tries to extract a string field from the raw_info token.</summary>
    /// <param name="rawInfo">The raw_info JToken.</param>
    /// <param name="field">The field name to extract.</param>
    /// <returns>The field value, or empty string if not found.</returns>
    private static string TryGetRawInfoField(JToken? rawInfo, string field)
    {
        switch (rawInfo)
        {
            case JArray arr:
                foreach (JToken item in arr)
                {
                    string? val = item.Value<string>(field);
                    if (!string.IsNullOrEmpty(val)) return val;
                }

                break;
            case JObject obj:
                return obj.Value<string>(field) ?? "";
        }

        return "";
    }

    /// <summary>Common base properties shared by all converted events.</summary>
    private readonly record struct EventBase(Guid ConnectionId, UserId SelfId, DateTime Time, IBotApi Api);

    /// <summary>Logs a warning for unknown event types and returns null for the switch expression.</summary>
    private static BotEvent? LogAndReturnNull(string message, string? eventType)
    {
        Logger.LogWarning(message, eventType ?? "(null)");
        return null;
    }

#endregion
}