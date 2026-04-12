using Mapster;
using Sora.Adapter.OneBot11.Models;
using Sora.Adapter.OneBot11.Models.Api;

namespace Sora.Adapter.OneBot11.Converter;

/// <summary>
///     Mapster mapping configuration for the OneBot v11 adapter.
/// </summary>
internal static class OneBot11MapsterConfig
{
    private static readonly Lazy<ILogger> LoggerLazy = new(() => SoraLogger.CreateLogger(typeof(OneBot11MapsterConfig).FullName!));
    private static          ILogger       Logger => LoggerLazy.Value;

    private static bool _configured;

    /// <summary>Registers all OB11 adapter Mapster mappings. Safe to call multiple times.</summary>
    internal static void Configure()
    {
        if (_configured) return;
        _configured = true;

        ConfigureGlobalConverters();
        ConfigureEntityMappings();
    }

    private static void ConfigureEntityMappings()
    {
#region User & Friend Mappings

        TypeAdapterConfig<GetStrangerInfoResponse, UserInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "");
        // UserId, Sex: auto-mapped via global converters

        TypeAdapterConfig<OneBotSender, UserInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "");
        // UserId, Sex: auto-mapped via global converters

        TypeAdapterConfig<OneBotSender, GroupMemberInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "")
            .Map(d => d.Card, s => s.Card ?? "")
            .Map(d => d.Title, s => s.Title ?? "")
            .Map(d => d.Level, s => (s.Level ?? "0").ToIntOrDefault())
            .Ignore(d => d.GroupId);
        // UserId, Role, Sex: auto-mapped via global converters
        // GroupId: comes from event, not sender — set via `with` at call site

        TypeAdapterConfig<GetFriendListItem, FriendInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "")
            .Map(d => d.Remark, s => s.Remark ?? "");
        // UserId: auto-mapped

        TypeAdapterConfig<DoubtFriendRequestItem, FriendRequestInfo>
            .NewConfig()
            .Map(d => d.InitiatorId, s => (UserId)(s.Uin ?? "0").ToLongOrDefault())
            .Map(d => d.Comment, s => s.Msg ?? "")
            .Map(d => d.Via, s => s.Source ?? "")
            .IgnoreNonMapped(true);

#endregion

#region Group Mappings

        TypeAdapterConfig<GetGroupInfoResponse, GroupInfo>
            .NewConfig()
            .Map(d => d.GroupName, s => s.GroupName ?? "")
            .Map(d => d.Remark, s => s.RemarkName ?? "")
            .Map(d => d.Description, s => s.GroupMemo ?? "")
            .Map(d => d.CreatedTime, s => s.GroupCreateTime);
        // GroupId, MemberCount, MaxMemberCount, OwnerId: auto-mapped

        TypeAdapterConfig<GetGroupMemberInfoResponse, GroupMemberInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "")
            .Map(d => d.Card, s => s.Card ?? "")
            .Map(d => d.Title, s => s.Title ?? "")
            .Map(d => d.Level, s => (s.Level ?? "0").ToIntOrDefault())
            .Map(
                d => d.JoinTime,
                s => s.JoinTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.JoinTime).LocalDateTime
                    : default(DateTime?))
            .Map(
                d => d.LastSentTime,
                s => s.LastSentTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.LastSentTime).LocalDateTime
                    : default(DateTime?))
            .Map(
                d => d.MuteExpireTime,
                s => s.ShutUpTimestamp > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.ShutUpTimestamp).LocalDateTime
                    : default(DateTime?));
        // UserId, GroupId, Role, Sex: auto-mapped via global converters

        TypeAdapterConfig<GroupNoticeItem, GroupAnnouncementInfo>
            .NewConfig()
            .Map(d => d.AnnouncementId, s => s.NoticeId ?? "")
            .Map(d => d.UserId, s => (UserId)s.SenderId)
            .Map(d => d.Content, s => s.Message != null ? s.Message.Text ?? "" : "")
            .Map(
                d => d.ImageUrl,
                s => s.Message != null && s.Message.Images != null && s.Message.Images.Count > 0
                    ? s.Message.Images[0].Id ?? ""
                    : "")
            .Map(
                d => d.Time,
                s => s.PublishTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.PublishTime).LocalDateTime
                    : default);

        TypeAdapterConfig<EssenceMsgItem, GroupEssenceMessageInfo>
            .NewConfig()
            .ConstructUsing(() => new GroupEssenceMessageInfo())
            .Map(d => d.SenderName, s => s.SenderNick ?? "")
            .Map(d => d.OperatorName, s => s.OperatorNick ?? "")
            .Map(
                d => d.MessageTime,
                s => s.SenderTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.SenderTime).LocalDateTime
                    : default)
            .Map(
                d => d.OperationTime,
                s => s.OperatorTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.OperatorTime).LocalDateTime
                    : default);
        // MessageId, SenderId, OperatorId: auto-mapped via global converters

        TypeAdapterConfig<GroupSystemMsgJoinRequest, GroupNotificationInfo>
            .NewConfig()
            .Map(d => d.Type, _ => "join_request")
            .Map(d => d.NotificationSeq, s => s.RequestId)
            .Map(d => d.InitiatorId, s => (UserId)s.RequesterUin)
            .Map(d => d.Comment, s => s.Message)
            .Map(d => d.State, s => s.Checked ? "accepted" : "pending");
        // GroupId: auto-mapped

        TypeAdapterConfig<GroupSystemMsgInvitedRequest, GroupNotificationInfo>
            .NewConfig()
            .Map(d => d.Type, _ => "invited_join_request")
            .Map(d => d.NotificationSeq, s => s.RequestId)
            .Map(d => d.InitiatorId, s => (UserId)s.InvitorUin)
            .Map(d => d.State, s => s.Checked ? "accepted" : "pending");
        // GroupId: auto-mapped

#endregion

#region File Mappings

        TypeAdapterConfig<OB11GroupFile, GroupFileInfo>
            .NewConfig()
            .Map(d => d.FileId, s => s.FileId ?? "")
            .Map(d => d.FileName, s => s.FileName ?? "")
            .Map(
                d => d.UploadedTime,
                s => s.UploadTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.UploadTime).LocalDateTime
                    : default(DateTime?))
            .Map(d => d.UploaderId, s => (UserId)s.Uploader)
            .Map(d => d.DownloadedTimes, s => s.DownloadTimes);
        // GroupId, FileSize: auto-mapped

        TypeAdapterConfig<OB11GroupFileFolder, GroupFolderInfo>
            .NewConfig()
            .Map(d => d.FolderId, s => s.FolderId ?? "")
            .Map(d => d.FolderName, s => s.FolderName ?? "")
            .Map(
                d => d.CreatedTime,
                s => s.CreateTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.CreateTime).LocalDateTime
                    : default(DateTime?))
            .Map(d => d.CreatorId, s => (UserId)s.Creator)
            .Map(d => d.FileCount, s => s.TotalFileCount);
        // GroupId: auto-mapped

        TypeAdapterConfig<GetGroupFilesResponse, GroupFilesResult>
            .NewConfig()
            .Map(d => d.Files, s => s.Files ?? new List<OB11GroupFile>())
            .Map(d => d.Folders, s => s.Folders ?? new List<OB11GroupFileFolder>());
        // Files: auto-mapped via OB11GroupFileItem → GroupFileInfo
        // Folders: auto-mapped via OB11GroupFileFolder → GroupFolderInfo

#endregion

#region Message Mappings

        // MessageBody contains abstract Segment records — prevent Mapster from deep-cloning
        TypeAdapterConfig<MessageBody, MessageBody>.NewConfig().MapWith(s => s);

        TypeAdapterConfig<GetMsgResponse, MessageContext>
            .NewConfig()
            .ConstructUsing(() => new MessageContext())
            .Map(d => d.SourceType, s => ParseMessageType(s.MessageType ?? ""))
            .Map(d => d.SenderId, s => s.Sender != null ? (UserId)s.Sender.UserId : default)
            .Map(d => d.Time, s => DateTimeOffset.FromUnixTimeSeconds(s.Time).LocalDateTime)
            .Map(d => d.Body, s => MessageConverter.ToMessageBody(s.Message));
        // MessageId: auto-mapped (int → MessageId via global converter)

        TypeAdapterConfig<GetMsgHistoryResponse, HistoryMessagesResult>
            .NewConfig()
            .Map(d => d.Messages, s => s.Messages ?? new List<GetMsgResponse>());
        // Messages: auto-mapped via GetMsgResponse → MessageContext

#endregion

#region System Mappings

        TypeAdapterConfig<GetLoginInfoResponse, BotIdentity>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "");
        // UserId: auto-mapped

        TypeAdapterConfig<GetVersionInfoResponse, ImplInfo>
            .NewConfig()
            .Map(d => d.ImplName, s => s.AppName ?? "")
            .Map(d => d.ImplVersion, s => s.AppVersion ?? "")
            .Map(d => d.ProtocolVersion, s => s.ProtocolVersion ?? "");

#endregion
    }

    private static void ConfigureGlobalConverters()
    {
        TypeAdapterConfig config = TypeAdapterConfig.GlobalSettings;

        // Enum converters (idempotent — same logic as Milky, safe to re-register)
        config.ForType<string, Sex>().MapWith(src => ParseSex(src));
        config.ForType<string, MemberRole>().MapWith(src => ParseMemberRole(src));

        // ID type converters
        config.ForType<long, UserId>().MapWith(src => src);
        config.ForType<long, GroupId>().MapWith(src => src);
        config.ForType<long, MessageId>().MapWith(src => src);
        config.ForType<int, MessageId>().MapWith(src => src);
    }

    private static MemberRole ParseMemberRole(string src) =>
        src switch
            {
                "owner"  => MemberRole.Owner,
                "admin"  => MemberRole.Admin,
                "member" => MemberRole.Member,
                _        => LogEnumFallback(src, MemberRole.Unknown)
            };

    private static MessageSourceType ParseMessageType(string src) =>
        src switch
            {
                "group"   => MessageSourceType.Group,
                "private" => MessageSourceType.Friend,
                _         => LogEnumFallback(src, (MessageSourceType)(-1))
            };

    private static Sex ParseSex(string src) =>
        src switch
            {
                "male"   => Sex.Male,
                "female" => Sex.Female,
                _        => LogEnumFallback(src, Sex.Unknown)
            };

    private static T LogEnumFallback<T>(string value, T fallback) where T : struct, Enum
    {
        if (!string.IsNullOrEmpty(value))
            Logger.LogWarning("Unknown {EnumType} value: '{Value}', using fallback {Fallback}", typeof(T).Name, value, fallback);
        return fallback;
    }
}