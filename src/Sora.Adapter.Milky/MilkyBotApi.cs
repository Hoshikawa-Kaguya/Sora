using Mapster;
using Sora.Adapter.Milky.Converter;
using Sora.Adapter.Milky.Models;
using Sora.Adapter.Milky.Models.Api;
using Sora.Adapter.Milky.Net;

namespace Sora.Adapter.Milky;

/// <summary>
///     Milky protocol implementation of the common bot API.
/// </summary>
public sealed class MilkyBotApi : IBotApi, IMilkyExtApi
{
    private readonly MilkyHttpApiClient _apiClient;
    private readonly Lazy<ILogger>      _loggerLazy = new(SoraLogger.CreateLogger<MilkyBotApi>);
    private          ILogger            _logger => _loggerLazy.Value;

    /// <summary>Initializes a new instance of the <see cref="MilkyBotApi" /> class.</summary>
    /// <param name="apiClient">The HTTP API client for Milky protocol calls.</param>
    internal MilkyBotApi(MilkyHttpApiClient apiClient)
    {
        _apiClient = apiClient;
    }

#region Milky Api Extension

    /// <inheritdoc />
    public T? GetExtension<T>() where T : class, IAdapterExtension
    {
        if (this is T ext) return ext;
        return null;
    }

#endregion

#region IBotApi — Identity

    /// <inheritdoc />
    public async ValueTask<ApiResult<BotIdentity>> GetSelfInfoAsync(CancellationToken ct = default)
    {
        ApiResult<GetLoginInfoOutput> resp = await CallApiAsync<GetLoginInfoOutput>(
            "get_login_info",
            null,
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<BotIdentity>.Ok(data.Adapt<BotIdentity>())
            : ApiResult<BotIdentity>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<ImplInfo>> GetImplInfoAsync(CancellationToken ct = default)
    {
        ApiResult<GetImplInfoOutput> resp = await CallApiAsync<GetImplInfoOutput>("get_impl_info", new { }, ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<ImplInfo>.Ok(data.Adapt<ImplInfo>())
            : ApiResult<ImplInfo>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> GetCookiesAsync(string domain, CancellationToken ct = default)
    {
        ApiResult<GetCookiesOutput> resp = await CallApiAsync<GetCookiesOutput>(
            "get_cookies",
            new GetCookiesInput { Domain = domain },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.Cookies ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> GetCsrfTokenAsync(CancellationToken ct = default)
    {
        ApiResult<GetCsrfTokenOutput> resp = await CallApiAsync<GetCsrfTokenOutput>(
            "get_csrf_token",
            new { },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.CsrfToken ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> GetResourceTempUrlAsync(
        string            resourceId,
        CancellationToken ct = default)
    {
        ApiResult<GetResourceTempUrlOutput> resp = await CallApiAsync<GetResourceTempUrlOutput>(
            "get_resource_temp_url",
            new GetResourceTempUrlInput
                {
                    ResourceId = resourceId
                },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.Url ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<string>>> GetCustomFaceUrlListAsync(
        CancellationToken ct = default)
    {
        ApiResult<GetCustomFaceUrlListOutput> resp =
            await CallApiAsync<GetCustomFaceUrlListOutput>(
                "get_custom_face_url_list",
                new { },
                ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<IReadOnlyList<string>>.Ok(data.Urls)
            : ApiResult<IReadOnlyList<string>>.Fail(resp.Code, resp.Message);
    }

#endregion

#region IBotApi — Messaging

    /// <inheritdoc />
    public async ValueTask<ApiResult<MessageContext>> GetMessageAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId         messageSeq,
        CancellationToken ct = default)
    {
        ApiResult<GetMessageOutput> resp = await CallApiAsync<GetMessageOutput>(
            "get_message",
            new GetMessageInput
                {
                    MessageScene = scene.Adapt<string>(),
                    PeerId       = peerId,
                    MessageSeq   = messageSeq
                },
            ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<MessageContext>.Fail(resp.Code, resp.Message);

        MilkyMessage msg = data.Message;
        return ApiResult<MessageContext>.Ok(msg.Adapt<MessageContext>());
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<HistoryMessagesResult>> GetHistoryMessagesAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId?        startMessageSeq = null,
        int               limit           = 20,
        CancellationToken ct              = default)
    {
        ApiResult<GetHistoryMessagesOutput> resp = await CallApiAsync<GetHistoryMessagesOutput>(
            "get_history_messages",
            new GetHistoryMessagesInput
                {
                    MessageScene    = scene.Adapt<string>(),
                    PeerId          = peerId,
                    StartMessageSeq = startMessageSeq ?? 0L,
                    Limit           = limit
                },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<HistoryMessagesResult>.Ok(data.Adapt<HistoryMessagesResult>())
            : ApiResult<HistoryMessagesResult>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<MessageContext>>> GetForwardMessagesAsync(
        string            forwardId,
        CancellationToken ct = default)
    {
        ApiResult<GetForwardedMessagesOutput> resp = await CallApiAsync<GetForwardedMessagesOutput>(
            "get_forwarded_messages",
            new GetForwardedMessagesInput { ForwardId = forwardId },
            ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<MessageContext>>.Fail(resp.Code, resp.Message);

        List<MessageContext> contexts = data.Messages.Select(m => m.Adapt<MessageContext>()).ToList();
        return ApiResult<IReadOnlyList<MessageContext>>.Ok(contexts);
    }

    /// <inheritdoc />
    public async ValueTask<SendMessageResult> SendFriendMessageAsync(
        UserId            userId,
        MessageBody       message,
        CancellationToken ct = default)
    {
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(message);
        if (segments.Count == 0)
        {
            _logger.LogWarning(
                "Cannot send Milky private message to user[{UserId}]: no valid segments remain after conversion",
                userId);
            return SendMessageResult.Fail(ApiStatusCode.InvalidMessage, "Empty message");
        }

        IReadOnlyList<string> issues = message.Validate();
        if (issues.Count > 0)
        {
            _logger.LogWarning(
                "Cannot send Milky private message to user[{UserId}]: validation failed with {IssueCount} issue(s): {Issues}",
                userId,
                issues.Count,
                string.Join(" | ", issues));
            return SendMessageResult.Fail(ApiStatusCode.InvalidMessage, string.Join(Environment.NewLine, issues));
        }

        _logger.LogInformation(
            "Sending Milky private message to user[{UserId}] with {SegmentCount} segment(s)",
            userId,
            segments.Count);

        ApiResult<SendMessageOutput> resp = await CallApiAsync<SendMessageOutput>(
            "send_private_message",
            new SendPrivateMessageInput
                {
                    UserId  = userId,
                    Message = segments
                },
            ct);
        if (resp is { IsSuccess: true, Data: { } data })
        {
            _logger.LogDebug("Milky private message sent to user[{UserId}], messageSeq={MessageSeq}", userId, data.MessageSeq);
            return SendMessageResult.Ok(data.MessageSeq);
        }

        _logger.LogWarning(
            "Milky private message send failed for user[{UserId}]: code={Code}, message={Message}",
            userId,
            resp.Code,
            resp.Message);
        return SendMessageResult.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<SendMessageResult> SendGroupMessageAsync(
        GroupId           groupId,
        MessageBody       message,
        CancellationToken ct = default)
    {
        List<MilkySegment> segments = MessageConverter.ToMilkySegments(message);
        if (segments.Count == 0)
        {
            _logger.LogWarning(
                "Cannot send Milky group message to group[{GroupId}]: no valid segments remain after conversion",
                groupId);
            return SendMessageResult.Fail(ApiStatusCode.InvalidMessage, "Empty message");
        }

        IReadOnlyList<string> issues = message.Validate();
        if (issues.Count > 0)
        {
            _logger.LogWarning(
                "Cannot send Milky group message to group[{GroupId}]: validation failed with {IssueCount} issue(s): {Issues}",
                groupId,
                issues.Count,
                string.Join(" | ", issues));
            return SendMessageResult.Fail(ApiStatusCode.InvalidMessage, string.Join(Environment.NewLine, issues));
        }

        _logger.LogInformation(
            "Sending Milky group message to group[{GroupId}] with {SegmentCount} segment(s)",
            groupId,
            segments.Count);

        ApiResult<SendMessageOutput> resp = await CallApiAsync<SendMessageOutput>(
            "send_group_message",
            new SendGroupMessageInput
                {
                    GroupId = groupId,
                    Message = segments
                },
            ct);
        if (resp is { IsSuccess: true, Data: { } data })
        {
            _logger.LogDebug("Milky group message sent to group[{GroupId}], messageSeq={MessageSeq}", groupId, data.MessageSeq);
            return SendMessageResult.Ok(data.MessageSeq);
        }

        _logger.LogWarning(
            "Milky group message send failed for group[{GroupId}]: code={Code}, message={Message}",
            groupId,
            resp.Code,
            resp.Message);
        return SendMessageResult.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult> RecallGroupMessageAsync(
        GroupId           groupId,
        MessageId         messageId,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "recall_group_message",
            new RecallGroupMessageInput { GroupId = groupId, MessageSeq = messageId },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> RecallPrivateMessageAsync(
        UserId            userId,
        MessageId         messageId,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "recall_private_message",
            new RecallPrivateMessageInput { UserId = userId, MessageSeq = messageId },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> MarkMessageAsReadAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId         messageSeq,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "mark_message_as_read",
            new MarkMessageAsReadInput
                {
                    MessageScene = scene.Adapt<string>(),
                    PeerId       = peerId,
                    MessageSeq   = messageSeq
                },
            ct);

#endregion

#region IBotApi — User & Friend

    /// <inheritdoc />
    public async ValueTask<ApiResult<UserInfo>> GetUserInfoAsync(
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetUserProfileOutput> resp = await CallApiAsync<GetUserProfileOutput>(
            "get_user_profile",
            new GetUserProfileInput { UserId = userId },
            ct);

        //UserId not mapped (resp do not have userid)
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<UserInfo>.Ok(data.Adapt<UserInfo>() with { UserId = userId })
            : ApiResult<UserInfo>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<UserProfile>> GetUserProfileAsync(
        UserId            userId,
        CancellationToken ct = default)
    {
        ApiResult<GetUserProfileOutput> resp = await CallApiAsync<GetUserProfileOutput>(
            "get_user_profile",
            new GetUserProfileInput { UserId = userId },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<UserProfile>.Ok(data.Adapt<UserProfile>() with { UserId = userId })
            : ApiResult<UserProfile>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<FriendInfo>> GetFriendInfoAsync(
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetFriendInfoOutput> resp = await CallApiAsync<GetFriendInfoOutput>(
            "get_friend_info",
            new GetFriendInfoInput
                {
                    UserId  = userId,
                    NoCache = noCache
                },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<FriendInfo>.Ok(data.Friend.Adapt<FriendInfo>())
            : ApiResult<FriendInfo>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<FriendInfo>>> GetFriendListAsync(
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetFriendListOutput> resp = await CallApiAsync<GetFriendListOutput>(
            "get_friend_list",
            new NoCacheInput { NoCache = noCache },
            ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<FriendInfo>>.Fail(resp.Code, resp.Message);

        List<FriendInfo> friends = data.Friends.Select(f => f.Adapt<FriendInfo>()).ToList();
        return ApiResult<IReadOnlyList<FriendInfo>>.Ok(friends);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<FriendRequestInfo>>> GetFriendRequestsAsync(
        int               limit      = 20,
        bool              isFiltered = false,
        CancellationToken ct         = default)
    {
        ApiResult<GetFriendRequestsOutput> resp =
            await CallApiAsync<GetFriendRequestsOutput>(
                "get_friend_requests",
                new GetFriendRequestsInput
                    {
                        Limit      = limit,
                        IsFiltered = isFiltered
                    },
                ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<FriendRequestInfo>>.Fail(resp.Code, resp.Message);

        List<FriendRequestInfo> requests = data.Requests.Select(r => r.Adapt<FriendRequestInfo>()).ToList();
        return ApiResult<IReadOnlyList<FriendRequestInfo>>.Ok(requests);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult> HandleFriendRequestAsync(
        UserId            fromUserId,
        bool              isFiltered,
        bool              approve,
        string            remark = "",
        CancellationToken ct     = default)
    {
        _logger.LogDebug(
            "Handling Milky friend request from user[{UserId}] (filtered: {IsFiltered}, approve: {Approve})",
            fromUserId,
            isFiltered,
            approve);

        // Query pending friend requests to resolve InitiatorUid at handle time
        ApiResult<GetFriendRequestsOutput> reqResp = await CallApiAsync<GetFriendRequestsOutput>(
            "get_friend_requests",
            new GetFriendRequestsInput { Limit = 100, IsFiltered = isFiltered },
            ct);
        if (reqResp is not { IsSuccess: true, Data: { } reqData })
            return ApiResult.Fail(reqResp.Code, reqResp.Message);

        MilkyFriendRequest? match = reqData.Requests
                                           .FirstOrDefault(r => r.InitiatorId == (long)fromUserId && r.State == "pending");
        if (match is null)
        {
            _logger.LogWarning("No matching pending Milky friend request was found for user[{UserId}]", fromUserId);
            return ApiResult.Fail(ApiStatusCode.Failed, "No matching pending friend request found");
        }

        string initiatorUid = match.InitiatorUid ?? "";

        return approve
            ? await CallApiAsync(
                "accept_friend_request",
                new AcceptFriendRequestInput
                        { InitiatorUid = initiatorUid, IsFiltered = isFiltered },
                ct)
            : await CallApiAsync(
                "reject_friend_request",
                new RejectFriendRequestInput
                        { InitiatorUid = initiatorUid, IsFiltered = isFiltered, Reason = remark },
                ct);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult> DeleteFriendAsync(UserId userId, CancellationToken ct = default) =>
        await CallApiAsync(
            "delete_friend",
            new DeleteFriendInput { UserId = userId },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SendFriendNudgeAsync(UserId userId, CancellationToken ct = default) =>
        await CallApiAsync(
            "send_friend_nudge",
            new SendFriendNudgeInput { UserId = userId },
            ct);

#endregion

#region IBotApi — Group Info

    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupInfo>> GetGroupInfoAsync(
        GroupId           groupId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetGroupInfoOutput> resp = await CallApiAsync<GetGroupInfoOutput>(
            "get_group_info",
            new GetGroupInfoInput
                {
                    GroupId = groupId,
                    NoCache = noCache
                },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<GroupInfo>.Ok(data.Group.Adapt<GroupInfo>())
            : ApiResult<GroupInfo>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<GroupInfo>>> GetGroupListAsync(
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetGroupListOutput> resp = await CallApiAsync<GetGroupListOutput>(
            "get_group_list",
            new NoCacheInput { NoCache = noCache },
            ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<GroupInfo>>.Fail(resp.Code, resp.Message);

        List<GroupInfo> groups = data.Groups.Select(g => g.Adapt<GroupInfo>()).ToList();
        return ApiResult<IReadOnlyList<GroupInfo>>.Ok(groups);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupMemberInfo>> GetGroupMemberInfoAsync(
        GroupId           groupId,
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetGroupMemberInfoOutput> resp = await CallApiAsync<GetGroupMemberInfoOutput>(
            "get_group_member_info",
            new GetGroupMemberInfoInput
                {
                    GroupId = groupId,
                    UserId  = userId,
                    NoCache = noCache
                },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<GroupMemberInfo>.Ok(data.Member.Adapt<GroupMemberInfo>())
            : ApiResult<GroupMemberInfo>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<GroupMemberInfo>>> GetGroupMemberListAsync(
        GroupId           groupId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetGroupMemberListOutput> resp = await CallApiAsync<GetGroupMemberListOutput>(
            "get_group_member_list",
            new GetGroupMemberListInput { GroupId = groupId, NoCache = noCache },
            ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<GroupMemberInfo>>.Fail(resp.Code, resp.Message);

        List<GroupMemberInfo> members = data.Members.Select(m => m.Adapt<GroupMemberInfo>()).ToList();
        return ApiResult<IReadOnlyList<GroupMemberInfo>>.Ok(members);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<GroupAnnouncementInfo>>> GetGroupAnnouncementsAsync(
        GroupId           groupId,
        CancellationToken ct = default)
    {
        ApiResult<GetGroupAnnouncementsOutput> resp = await CallApiAsync<GetGroupAnnouncementsOutput>(
            "get_group_announcements",
            new GetGroupAnnouncementsInput { GroupId = groupId },
            ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<GroupAnnouncementInfo>>.Fail(resp.Code, resp.Message);

        List<GroupAnnouncementInfo> announcements = data.Announcements
                                                        .Select(a => a.Adapt<GroupAnnouncementInfo>())
                                                        .ToList();
        return ApiResult<IReadOnlyList<GroupAnnouncementInfo>>.Ok(announcements);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupEssenceMessagesPage>> GetGroupEssenceMessagesAsync(
        GroupId           groupId,
        int               pageIndex,
        int               pageSize,
        CancellationToken ct = default)
    {
        ApiResult<GetGroupEssenceMessagesOutput> resp = await CallApiAsync<GetGroupEssenceMessagesOutput>(
            "get_group_essence_messages",
            new GetGroupEssenceMessagesInput { GroupId = groupId, PageIndex = pageIndex, PageSize = pageSize },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<GroupEssenceMessagesPage>.Ok(data.Adapt<GroupEssenceMessagesPage>())
            : ApiResult<GroupEssenceMessagesPage>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupNotificationsResult>> GetGroupNotificationsAsync(
        long?             startNotificationSeq = null,
        bool              isFiltered           = false,
        int               limit                = 20,
        CancellationToken ct                   = default)
    {
        ApiResult<GetGroupNotificationsOutput> resp = await CallApiAsync<GetGroupNotificationsOutput>(
            "get_group_notifications",
            new GetGroupNotificationsInput
                {
                    StartNotificationSeq = startNotificationSeq ?? 0L,
                    IsFiltered           = isFiltered,
                    Limit                = limit
                },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<GroupNotificationsResult>.Ok(data.Adapt<GroupNotificationsResult>())
            : ApiResult<GroupNotificationsResult>.Fail(resp.Code, resp.Message);
    }

#endregion

#region IBotApi — Group Management

    /// <inheritdoc />
    public async ValueTask<ApiResult> HandleGroupRequestAsync(
        GroupId                   groupId,
        long                      notificationSeq,
        GroupJoinNotificationType joinNotificationType,
        bool                      isFiltered,
        bool                      approve,
        string                    reason = "",
        CancellationToken         ct     = default)
    {
        _logger.LogDebug(
            "Handling Milky group request for group[{GroupId}] (notificationSeq: {NotificationSeq}, type: {JoinType}, filtered: {IsFiltered}, approve: {Approve})",
            groupId,
            notificationSeq,
            joinNotificationType,
            isFiltered,
            approve);

        string typeStr = joinNotificationType switch
                             {
                                 GroupJoinNotificationType.JoinRequest => "join_request",
                                 GroupJoinNotificationType.InvitedJoinRequest => "invited_join_request",
                                 _ => throw new ArgumentOutOfRangeException(nameof(joinNotificationType), joinNotificationType, null)
                             };

        return approve
            ? await CallApiAsync(
                "accept_group_request",
                new AcceptGroupRequestInput
                    {
                        GroupId          = groupId,
                        NotificationSeq  = notificationSeq,
                        NotificationType = typeStr,
                        IsFiltered       = isFiltered
                    },
                ct)
            : await CallApiAsync(
                "reject_group_request",
                new RejectGroupRequestInput
                    {
                        GroupId          = groupId,
                        NotificationSeq  = notificationSeq,
                        NotificationType = typeStr,
                        IsFiltered       = isFiltered,
                        Reason           = reason
                    },
                ct);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult> HandleGroupInvitationAsync(
        GroupId           groupId,
        long              invitationSeq,
        bool              approve,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Handling Milky group invitation for group[{GroupId}] (invitationSeq: {InvitationSeq}, approve: {Approve})",
            groupId,
            invitationSeq,
            approve);

        return approve
            ? await CallApiAsync(
                "accept_group_invitation",
                new AcceptGroupInvitationInput { GroupId = groupId, InvitationSeq = invitationSeq },
                ct)
            : await CallApiAsync(
                "reject_group_invitation",
                new RejectGroupInvitationInput { GroupId = groupId, InvitationSeq = invitationSeq },
                ct);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupNameAsync(
        GroupId           groupId,
        string            name,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_group_name",
            new SetGroupNameInput { GroupId = groupId, NewGroupName = name },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupAvatarAsync(
        GroupId           groupId,
        string            imageUri,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_group_avatar",
            new SetGroupAvatarInput { GroupId = groupId, ImageUri = imageUri },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupAdminAsync(
        GroupId           groupId,
        UserId            userId,
        bool              enable,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_group_member_admin",
            new SetGroupMemberAdminInput { GroupId = groupId, UserId = userId, IsSet = enable },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupMemberCardAsync(
        GroupId           groupId,
        UserId            userId,
        string            card,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_group_member_card",
            new SetGroupMemberCardInput { GroupId = groupId, UserId = userId, Card = card },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupMemberSpecialTitleAsync(
        GroupId           groupId,
        UserId            userId,
        string            title,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_group_member_special_title",
            new SetGroupMemberSpecialTitleInput { GroupId = groupId, UserId = userId, SpecialTitle = title },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupEssenceMessageAsync(
        GroupId           groupId,
        MessageId         messageSeq,
        bool              isSet = true,
        CancellationToken ct    = default) =>
        await CallApiAsync(
            "set_group_essence_message",
            new SetGroupEssenceMessageInput { GroupId = groupId, MessageSeq = messageSeq, IsSet = isSet },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SendGroupAnnouncementAsync(
        GroupId           groupId,
        string            content,
        string?           imageUri = null,
        CancellationToken ct       = default) =>
        await CallApiAsync(
            "send_group_announcement",
            new SendGroupAnnouncementInput
                {
                    GroupId  = groupId,
                    Content  = content,
                    ImageUri = imageUri ?? ""
                },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> DeleteGroupAnnouncementAsync(
        GroupId           groupId,
        string            announcementId,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "delete_group_announcement",
            new DeleteGroupAnnouncementInput { GroupId = groupId, AnnouncementId = announcementId },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> KickGroupMemberAsync(
        GroupId           groupId,
        UserId            userId,
        bool              rejectFuture = false,
        CancellationToken ct           = default) =>
        await CallApiAsync(
            "kick_group_member",
            new KickGroupMemberInput { GroupId = groupId, UserId = userId, RejectAddRequest = rejectFuture },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> MuteGroupMemberAsync(
        GroupId           groupId,
        UserId            userId,
        int               durationSeconds,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_group_member_mute",
            new SetGroupMemberMuteInput { GroupId = groupId, UserId = userId, Duration = durationSeconds },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> MuteGroupAllAsync(
        GroupId           groupId,
        bool              enable,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_group_whole_mute",
            new SetGroupWholeMuteInput { GroupId = groupId, IsMute = enable },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> LeaveGroupAsync(
        GroupId           groupId,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "quit_group",
            new QuitGroupInput { GroupId = groupId },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SendGroupNudgeAsync(
        GroupId           groupId,
        UserId            userId,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "send_group_nudge",
            new SendGroupNudgeInput { GroupId = groupId, UserId = userId },
            ct);

#endregion

#region IBotApi — File Operations

    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupFilesResult>> GetGroupFilesAsync(
        GroupId           groupId,
        string            parentFolderId = "/",
        CancellationToken ct             = default)
    {
        ApiResult<GetGroupFilesOutput> resp = await CallApiAsync<GetGroupFilesOutput>(
            "get_group_files",
            new GetGroupFilesInput
                {
                    GroupId        = groupId,
                    ParentFolderId = parentFolderId
                },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<GroupFilesResult>.Ok(data.Adapt<GroupFilesResult>())
            : ApiResult<GroupFilesResult>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> GetGroupFileDownloadUrlAsync(
        GroupId           groupId,
        string            fileId,
        CancellationToken ct = default)
    {
        ApiResult<DownloadUrlOutput> resp = await CallApiAsync<DownloadUrlOutput>(
            "get_group_file_download_url",
            new GetGroupFileDownloadUrlInput
                {
                    GroupId = groupId,
                    FileId  = fileId
                },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.DownloadUrl ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> GetPrivateFileDownloadUrlAsync(
        UserId            userId,
        string            fileId,
        string            fileHash,
        CancellationToken ct = default)
    {
        ApiResult<DownloadUrlOutput> resp = await CallApiAsync<DownloadUrlOutput>(
            "get_private_file_download_url",
            new GetPrivateFileDownloadUrlInput
                {
                    UserId   = userId,
                    FileId   = fileId,
                    FileHash = fileHash
                },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.DownloadUrl ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> CreateGroupFolderAsync(
        GroupId           groupId,
        string            folderName,
        CancellationToken ct = default)
    {
        ApiResult<CreateGroupFolderOutput> resp = await CallApiAsync<CreateGroupFolderOutput>(
            "create_group_folder",
            new CreateGroupFolderInput
                {
                    GroupId    = groupId,
                    FolderName = folderName
                },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.FolderId ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> UploadGroupFileAsync(
        GroupId           groupId,
        string            fileUri,
        string            fileName,
        string            parentFolderId = "/",
        CancellationToken ct             = default)
    {
        ApiResult<UploadFileOutput> resp = await CallApiAsync<UploadFileOutput>(
            "upload_group_file",
            new UploadGroupFileInput
                {
                    GroupId        = groupId,
                    FileUri        = fileUri,
                    FileName       = fileName,
                    ParentFolderId = parentFolderId
                },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.FileId ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> UploadPrivateFileAsync(
        UserId            userId,
        string            fileUri,
        string            fileName,
        CancellationToken ct = default)
    {
        ApiResult<UploadFileOutput> resp = await CallApiAsync<UploadFileOutput>(
            "upload_private_file",
            new UploadPrivateFileInput
                {
                    UserId   = userId,
                    FileUri  = fileUri,
                    FileName = fileName
                },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.FileId ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }

    /// <inheritdoc />
    public async ValueTask<ApiResult> DeleteGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "delete_group_file",
            new DeleteGroupFileInput { GroupId = groupId, FileId = fileId },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> DeleteGroupFolderAsync(
        GroupId           groupId,
        string            folderId,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "delete_group_folder",
            new DeleteGroupFolderInput { GroupId = groupId, FolderId = folderId },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> MoveGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        string            parentFolderId,
        string            targetFolderId,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "move_group_file",
            new MoveGroupFileInput
                {
                    GroupId        = groupId,
                    FileId         = fileId,
                    ParentFolderId = parentFolderId,
                    TargetFolderId = targetFolderId
                },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> RenameGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        string            parentFolderId,
        string            newFileName,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "rename_group_file",
            new RenameGroupFileInput
                {
                    GroupId        = groupId,
                    FileId         = fileId,
                    ParentFolderId = parentFolderId,
                    NewFileName    = newFileName
                },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> RenameGroupFolderAsync(
        GroupId           groupId,
        string            folderId,
        string            newName,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "rename_group_folder",
            new RenameGroupFolderInput { GroupId = groupId, FolderId = folderId, NewFolderName = newName },
            ct);

#endregion

#region IBotApi — Profile

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetAvatarAsync(
        string            uri,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_avatar",
            new SetAvatarInput { Uri = uri },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetBioAsync(
        string            bio,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_bio",
            new SetBioInput { NewBio = bio },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetNicknameAsync(
        string            nickname,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_nickname",
            new SetNicknameInput { NewNickname = nickname },
            ct);

    /// <inheritdoc />
    public async ValueTask<ApiResult> SendProfileLikeAsync(
        UserId            userId,
        int               count = 1,
        CancellationToken ct    = default) =>
        await CallApiAsync(
            "send_profile_like",
            new SendProfileLikeInput { UserId = userId, Count = count },
            ct);

#endregion

#region IBotApi — Reactions

    /// <inheritdoc />
    public async ValueTask<ApiResult> SendGroupMessageReactionAsync(
        GroupId           groupId,
        MessageId         messageSeq,
        string            faceId,
        bool              isAdd        = true,
        string            reactionType = "face",
        CancellationToken ct           = default) =>
        await CallApiAsync(
            "send_group_message_reaction",
            new SendGroupMessageReactionInput
                {
                    GroupId      = groupId,
                    MessageSeq   = messageSeq,
                    Reaction     = faceId,
                    ReactionType = reactionType,
                    IsAdd        = isAdd
                },
            ct);

#endregion

#region IMilkyExtApi — Query

    /// <inheritdoc />
    public async ValueTask<ApiResult<PeerPinsResult>> GetPeerPinsAsync(CancellationToken ct = default)
    {
        ApiResult<GetPeerPinsOutput> resp = await CallApiAsync<GetPeerPinsOutput>(
            "get_peer_pins",
            null,
            ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<PeerPinsResult>.Fail(resp.Code, resp.Message);

        PeerPinsResult result = new()
            {
                Friends = data.Friends.Select(f => f.Adapt<FriendInfo>()).ToList(),
                Groups  = data.Groups.Select(g => g.Adapt<GroupInfo>()).ToList()
            };
        return ApiResult<PeerPinsResult>.Ok(result);
    }

#endregion

#region IMilkyExtApi — Settings

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetPeerPinAsync(
        MessageSourceType messageScene,
        long              peerId,
        bool              isPinned,
        CancellationToken ct = default) =>
        await CallApiAsync(
            "set_peer_pin",
            new SetPeerPinInput
                {
                    MessageScene = messageScene.Adapt<string>(),
                    PeerId       = peerId,
                    IsPinned     = isPinned
                },
            ct);

#endregion

#region Private Helpers

    /// <summary>Calls a Milky API endpoint and deserializes the response.</summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="action">The API action name.</param>
    /// <param name="parameters">The request parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The deserialized API result.</returns>
    private async ValueTask<ApiResult<T>> CallApiAsync<T>(
        string            action,
        object?           parameters,
        CancellationToken ct) where T : class
    {
        MilkyApiResponse resp = await _apiClient.CallApiAsync(
            action,
            parameters,
            ct);
        if (resp.Status != "ok")
        {
            _logger.LogWarning(
                "Milky API action [{Action}] failed: retCode={RetCode}, message={Message}",
                action,
                resp.RetCode,
                resp.Message ?? "");
            return ApiResult<T>.Fail(MapRetCode(resp.RetCode), resp.Message ?? "");
        }

        T? data = resp.Data?.ToObject<T>();
        return data is not null
            ? ApiResult<T>.Ok(data)
            : ReturnEmptyFailure<T>(action);
    }

    /// <summary>Calls a Milky API endpoint without deserializing response data.</summary>
    /// <param name="action">The API action name.</param>
    /// <param name="parameters">The request parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A simple success/failure API result.</returns>
    private async ValueTask<ApiResult> CallApiAsync(
        string            action,
        object?           parameters,
        CancellationToken ct)
    {
        MilkyApiResponse resp = await _apiClient.CallApiAsync(action, parameters, ct);
        if (resp.Status == "ok")
            return ApiResult.Ok();

        _logger.LogWarning(
            "Milky API action [{Action}] failed: retCode={RetCode}, message={Message}",
            action,
            resp.RetCode,
            resp.Message ?? "");
        return ApiResult.Fail(MapRetCode(resp.RetCode), resp.Message ?? "");
    }

    private ApiResult<T> ReturnEmptyFailure<T>(string action) where T : class
    {
        _logger.LogWarning("Milky API action [{Action}] succeeded but returned empty response data", action);
        return ApiResult<T>.Fail(ApiStatusCode.Failed, "Empty response data");
    }

    /// <summary>Maps a Milky return code to an <see cref="ApiStatusCode" />.</summary>
    /// <param name="retCode">The Milky API return code (negative for protocol errors, positive for HTTP/framework).</param>
    /// <returns>The corresponding API status code.</returns>
    private static ApiStatusCode MapRetCode(int retCode) =>
        Enum.IsDefined(typeof(ApiStatusCode), retCode)
            ? (ApiStatusCode)retCode
            : ApiStatusCode.Unknown;

#endregion
}