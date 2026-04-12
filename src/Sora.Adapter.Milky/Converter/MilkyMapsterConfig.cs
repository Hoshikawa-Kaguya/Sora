using Mapster;
using Sora.Adapter.Milky.Models;
using Sora.Adapter.Milky.Models.Api;

namespace Sora.Adapter.Milky.Converter;

/// <summary>
///     Mapster mapping configuration for the Milky adapter.
/// </summary>
internal static class MilkyMapsterConfig
{
    private static readonly Lazy<ILogger> LoggerLazy = new(() => SoraLogger.CreateLogger(typeof(MilkyMapsterConfig).FullName!));
    private static          ILogger       Logger => LoggerLazy.Value;

    private static bool _configured;

    /// <summary>Registers all Milky adapter Mapster mappings. Safe to call multiple times.</summary>
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

        TypeAdapterConfig<MilkyFriendCategoryEntity, FriendCategoryInfo>
            .NewConfig()
            .Map(d => d.CategoryName, s => s.CategoryName ?? "");

        TypeAdapterConfig<MilkyFriendEntity, FriendInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "")
            .Map(d => d.Qid, s => s.Qid ?? "")
            .Map(d => d.Remark, s => s.Remark ?? "");
        // UserId: long → UserId via global converter
        // Sex: string? → Sex via global converter
        // Category: MilkyFriendCategoryEntity? → FriendCategoryInfo? via sub-mapping

        TypeAdapterConfig<MilkyFriendEntity, UserInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "");
        // UserId, Sex: auto-mapped via global converters

        TypeAdapterConfig<GetUserProfileOutput, UserInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "")
            .Ignore(d => d.UserId);
        // Sex: string? → Sex via global converter
        // UserId: not present in source, set externally via record `with` expression

        TypeAdapterConfig<GetUserProfileOutput, UserProfile>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "")
            .Map(d => d.Qid, s => s.Qid ?? "")
            .Map(d => d.Remark, s => s.Remark ?? "")
            .Map(d => d.Bio, s => s.Bio ?? "")
            .Map(d => d.Country, s => s.Country ?? "")
            .Map(d => d.City, s => s.City ?? "")
            .Map(d => d.School, s => s.School ?? "")
            .Ignore(d => d.UserId);
        // Age, Level: auto-mapped
        // Sex: string? → Sex via global converter
        // UserId: not present in source, set externally via record `with` expression

        TypeAdapterConfig<MilkyFriendRequest, FriendRequestInfo>
            .NewConfig()
            .Map(d => d.Time, s => DateTimeOffset.FromUnixTimeSeconds(s.Time).LocalDateTime)
            .Map(d => d.InitiatorUid, s => s.InitiatorUid ?? "")
            .Map(d => d.TargetUserUid, s => s.TargetUserUid ?? "")
            .Map(d => d.State, s => s.State ?? "")
            .Map(d => d.Comment, s => s.Comment ?? "")
            .Map(d => d.Via, s => s.Via ?? "");
        // InitiatorId, TargetUserId, IsFiltered: auto-mapped

#endregion

#region Group Mappings

        TypeAdapterConfig<MilkyGroupEntity, GroupInfo>
            .NewConfig()
            .Map(d => d.GroupName, s => s.GroupName ?? "")
            .Map(d => d.Remark, s => s.Remark ?? "")
            .Map(d => d.Description, s => s.Description ?? "")
            .Map(d => d.Question, s => s.Question ?? "")
            .Map(d => d.Announcement, s => s.Announcement ?? "")
            .Ignore(d => d.OwnerId);
        // GroupId, MemberCount, MaxMemberCount, CreatedTime: auto-mapped
        // OwnerId: not available in Milky group entity — set externally if needed

        TypeAdapterConfig<MilkyGroupMemberEntity, GroupMemberInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "")
            .Map(d => d.Card, s => s.Card ?? "")
            .Map(d => d.Title, s => s.Title ?? "")
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
                s => s.ShutUpEndTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.ShutUpEndTime).LocalDateTime
                    : default(DateTime?))
            .Ignore(d => d.Age);
        // UserId, GroupId, Role, Sex, Level: auto-mapped via global converters
        // Age: not available in Milky group member entity

        TypeAdapterConfig<MilkyGroupMemberEntity, UserInfo>
            .NewConfig()
            .Map(d => d.Nickname, s => s.Nickname ?? "");
        // UserId, Sex: auto-mapped via global converters

        TypeAdapterConfig<MilkyGroupAnnouncementEntity, GroupAnnouncementInfo>
            .NewConfig()
            .Map(d => d.AnnouncementId, s => s.AnnouncementId ?? "")
            .Map(d => d.Time, s => DateTimeOffset.FromUnixTimeSeconds(s.Time).LocalDateTime)
            .Map(d => d.Content, s => s.Content ?? "");
        // GroupId, UserId, ImageUrl: auto-mapped

        TypeAdapterConfig<MilkyGroupEssenceMessage, GroupEssenceMessageInfo>
            .NewConfig()
            .ConstructUsing(() => new GroupEssenceMessageInfo())
            .Map(d => d.MessageId, s => (MessageId)s.MessageSeq)
            .Map(d => d.SenderName, s => s.SenderName ?? "")
            .Map(d => d.OperatorName, s => s.OperatorName ?? "")
            .Map(d => d.MessageTime, s => DateTimeOffset.FromUnixTimeSeconds(s.MessageTime).LocalDateTime)
            .Map(d => d.OperationTime, s => DateTimeOffset.FromUnixTimeSeconds(s.OperationTime).LocalDateTime)
            .Map(d => d.Body, s => MessageConverter.ToMessageBody(s.Segments));
        // GroupId, SenderId, OperatorId: auto-mapped

        TypeAdapterConfig<MilkyGroupNotification, GroupNotificationInfo>
            .NewConfig()
            .Map(d => d.Type, s => s.Type ?? "")
            .Map(d => d.OperatorId, s => s.OperatorId > 0 ? (UserId?)s.OperatorId : null)
            .Map(d => d.InitiatorId, s => s.InitiatorId.HasValue ? (UserId)s.InitiatorId.Value : default)
            .Map(d => d.TargetUserId, s => s.TargetUserId.HasValue ? (UserId)s.TargetUserId.Value : default);
        // GroupId, NotificationSeq, IsFiltered, IsSet: auto-mapped
        // State, Comment: auto-mapped (both string?)

        TypeAdapterConfig<GetGroupEssenceMessagesOutput, GroupEssenceMessagesPage>.NewConfig();
        // Messages: auto-mapped via MilkyGroupEssenceMessage → GroupEssenceMessageInfo
        // IsEnd: auto-mapped

        TypeAdapterConfig<GetGroupNotificationsOutput, GroupNotificationsResult>
            .NewConfig()
            .Map(
                d => d.NextNotificationSeq,
                s => s.NextNotificationSeq > 0
                    ? (long?)s.NextNotificationSeq
                    : null);
        // Notifications: auto-mapped via MilkyGroupNotification → GroupNotificationInfo

#endregion

#region File Mappings

        TypeAdapterConfig<MilkyGroupFileEntity, GroupFileInfo>
            .NewConfig()
            .Map(d => d.FileId, s => s.FileId ?? "")
            .Map(d => d.FileName, s => s.FileName ?? "")
            .Map(d => d.ParentFolderId, s => s.ParentFolderId ?? "")
            .Map(
                d => d.UploadedTime,
                s => s.UploadedTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.UploadedTime).LocalDateTime
                    : default(DateTime?))
            .Map(
                d => d.ExpireTime,
                s => s.ExpireTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.ExpireTime).LocalDateTime
                    : default(DateTime?));
        // GroupId, FileSize, UploaderId, DownloadedTimes: auto-mapped

        TypeAdapterConfig<MilkyGroupFolderEntity, GroupFolderInfo>
            .NewConfig()
            .Map(d => d.FolderId, s => s.FolderId ?? "")
            .Map(d => d.ParentFolderId, s => s.ParentFolderId ?? "")
            .Map(d => d.FolderName, s => s.FolderName ?? "")
            .Map(
                d => d.CreatedTime,
                s => s.CreatedTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.CreatedTime).LocalDateTime
                    : default(DateTime?))
            .Map(
                d => d.LastModifiedTime,
                s => s.LastModifiedTime > 0
                    ? DateTimeOffset.FromUnixTimeSeconds(s.LastModifiedTime).LocalDateTime
                    : default(DateTime?));
        // GroupId, CreatorId, FileCount: auto-mapped

        TypeAdapterConfig<GetGroupFilesOutput, GroupFilesResult>.NewConfig();
        // Files: auto-mapped via MilkyGroupFileEntity → GroupFileInfo
        // Folders: auto-mapped via MilkyGroupFolderEntity → GroupFolderInfo

#endregion

#region Message Mappings

        // MessageBody contains abstract Segment records — prevent Mapster from deep-cloning
        TypeAdapterConfig<MessageBody, MessageBody>.NewConfig().MapWith(s => s);

        TypeAdapterConfig<MilkyForwardedMessage, MessageContext>
            .NewConfig()
            .ConstructUsing(() => new MessageContext())
            .Map(d => d.MessageId, s => (MessageId)s.MessageSeq)
            .Map(d => d.SenderName, s => s.SenderName ?? "")
            .Map(d => d.AvatarUrl, s => s.AvatarUrl ?? "")
            .Map(d => d.Time, s => DateTimeOffset.FromUnixTimeSeconds(s.Time).LocalDateTime)
            .Map(d => d.Body, s => MessageConverter.ToMessageBody(s.Segments));

        TypeAdapterConfig<MilkyMessage, MessageContext>
            .NewConfig()
            .ConstructUsing(() => new MessageContext())
            .Map(d => d.MessageId, s => (MessageId)s.MessageSeq)
            .Map(d => d.SourceType, s => (s.MessageScene ?? "").Adapt<MessageSourceType>())
            .Map(d => d.GroupId, s => s.MessageScene == "group" ? (GroupId)s.PeerId : default)
            .Map(d => d.Time, s => DateTimeOffset.FromUnixTimeSeconds(s.Time).LocalDateTime)
            .Map(d => d.Body, s => MessageConverter.ToMessageBody(s.Segments));
        // SenderId: auto-mapped via global long → UserId converter

        TypeAdapterConfig<GetHistoryMessagesOutput, HistoryMessagesResult>
            .NewConfig()
            .Map(d => d.NextMessageSeq, s => s.NextMessageSeq > 0 ? (MessageId?)s.NextMessageSeq : null);
        // Messages: auto-mapped via MilkyMessage → MessageContext

#endregion

#region System Mappings

        TypeAdapterConfig<GetImplInfoOutput, ImplInfo>
            .NewConfig()
            .Map(d => d.ImplName, s => s.ImplName ?? "")
            .Map(d => d.ImplVersion, s => s.ImplVersion ?? "")
            .Map(d => d.QqProtocolVersion, s => s.QqProtocolVersion ?? "")
            .Map(d => d.QqProtocolType, s => s.QqProtocolType ?? "")
            .Map(d => d.ProtocolVersion, s => s.MilkyVersion ?? "");

        TypeAdapterConfig<GetLoginInfoOutput, BotIdentity>
            .NewConfig()
            .Map(d => d.UserId, s => (UserId)s.Uin)
            .Map(d => d.Nickname, s => s.Nickname ?? "");

#endregion
    }

    private static void ConfigureGlobalConverters()
    {
        TypeAdapterConfig config = TypeAdapterConfig.GlobalSettings;

        // Enum converters
        config.ForType<string, Sex>().MapWith(src => ParseSex(src));
        config.ForType<string, MemberRole>().MapWith(src => ParseMemberRole(src));
        config.ForType<string, ImageSubType>().MapWith(src => ParseImageSubType(src));
        config.ForType<ImageSubType, string>().MapWith(src => FormatImageSubType(src));

        // MessageSourceType ↔ string converters
        config.ForType<string, MessageSourceType>().MapWith(src => ParseMessageSourceType(src));
        config.ForType<MessageSourceType, string>().MapWith(src => FormatMessageSourceType(src));

        // ID type converters
        config.ForType<long, UserId>().MapWith(src => src);
        config.ForType<long, GroupId>().MapWith(src => src);
        config.ForType<long, MessageId>().MapWith(src => src);
        config.ForType<int, MessageId>().MapWith(src => src);
    }

    private static string FormatImageSubType(ImageSubType src) =>
        src switch
            {
                ImageSubType.Normal  => "normal",
                ImageSubType.Sticker => "sticker",
                _                    => "normal" // fall back
            };

    private static string FormatMessageSourceType(MessageSourceType src) =>
        src switch
            {
                MessageSourceType.Friend => "friend",
                MessageSourceType.Group  => "group",
                MessageSourceType.Temp   => "temp",
                _                        => "temp" // fall back
            };

    private static ImageSubType ParseImageSubType(string src) =>
        src switch
            {
                "normal"  => ImageSubType.Normal,
                "sticker" => ImageSubType.Sticker,
                _         => LogEnumFallback(src, (ImageSubType)(-1))
            };

    private static MemberRole ParseMemberRole(string src) =>
        src switch
            {
                "owner"  => MemberRole.Owner,
                "admin"  => MemberRole.Admin,
                "member" => MemberRole.Member,
                _        => LogEnumFallback(src, MemberRole.Unknown)
            };

    private static MessageSourceType ParseMessageSourceType(string src) =>
        src switch
            {
                "friend" => MessageSourceType.Friend,
                "group"  => MessageSourceType.Group,
                "temp"   => MessageSourceType.Temp,
                _        => LogEnumFallback(src, (MessageSourceType)(-1))
            };

    private static Sex ParseSex(string src) =>
        src switch
            {
                "male"    => Sex.Male,
                "female"  => Sex.Female,
                "unknown" => Sex.Unknown,
                _         => LogEnumFallback(src, Sex.Unknown)
            };

    private static T LogEnumFallback<T>(string value, T fallback) where T : struct, Enum
    {
        if (!string.IsNullOrEmpty(value))
            Logger.LogWarning("Unknown {EnumType} value: '{Value}', using fallback {Fallback}", typeof(T).Name, value, fallback);
        return fallback;
    }
}