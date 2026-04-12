using System.Collections.Concurrent;
using Mapster;
using Newtonsoft.Json.Linq;
using Sora.Adapter.OneBot11.Converter;
using Sora.Adapter.OneBot11.Models;
using Sora.Adapter.OneBot11.Models.Api;
using Sora.Adapter.OneBot11.Net;

namespace Sora.Adapter.OneBot11;

/// <summary>
///     OneBot v11 implementation of the common bot API.
/// </summary>
public sealed class OneBot11BotApi : IBotApi, IOneBot11ExtApi
{
    private readonly ReactiveApiManager _apiManager;
    private readonly Lazy<ILogger>      _loggerLazy = new(SoraLogger.CreateLogger<OneBot11BotApi>);
    private          ILogger            _logger => _loggerLazy.Value;

    /// <summary>Stores normal friend request flags from events, keyed by user ID.</summary>
    private readonly ConcurrentDictionary<long, string> _friendRequestFlags = new();

    /// <summary>Stores group invitation flags from events, keyed by (groupId, invitorId).</summary>
    private readonly ConcurrentDictionary<(long GroupId, long InvitorId), string> _groupInvitationFlags = new();

    /// <summary>Stores group join request flags from events, keyed by (groupId, userId).</summary>
    private readonly ConcurrentDictionary<(long GroupId, long UserId), string> _groupRequestFlags = new();

    private readonly Func<string, ValueTask> _sendFunc;

    /// <summary>Initializes a new instance of the <see cref="OneBot11BotApi" /> class.</summary>
    /// <param name="apiManager">The reactive API manager for request/response matching.</param>
    /// <param name="sendFunc">The function to send raw JSON through the WebSocket.</param>
    internal OneBot11BotApi(ReactiveApiManager apiManager, Func<string, ValueTask> sendFunc)
    {
        _apiManager = apiManager;
        _sendFunc   = sendFunc;
    }


#region Onebot11 Extension Api

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
        ApiResult<GetLoginInfoResponse> resp = await CallActionAsync<GetLoginInfoResponse>("get_login_info", null, ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<BotIdentity>.Ok(data.Adapt<BotIdentity>())
            : ApiResult<BotIdentity>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<ImplInfo>> GetImplInfoAsync(CancellationToken ct = default)
    {
        ApiResult<GetVersionInfoResponse> resp = await CallActionAsync<GetVersionInfoResponse>("get_version_info", null, ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<ImplInfo>.Ok(data.Adapt<ImplInfo>())
            : ApiResult<ImplInfo>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> GetCookiesAsync(string domain, CancellationToken ct = default)
    {
        ApiResult<GetCookiesResponse> resp = await CallActionAsync<GetCookiesResponse>(
            "get_cookies",
            new GetCookiesParams { Domain = domain },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.Cookies ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> GetCsrfTokenAsync(CancellationToken ct = default)
    {
        ApiResult<GetCsrfTokenResponse> resp = await CallActionAsync<GetCsrfTokenResponse>("get_csrf_token", null, ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.Token.ToString())
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> GetResourceTempUrlAsync(string resourceId, CancellationToken ct = default)
    {
        ApiResult<GetFileResponse> resp = await CallActionAsync<GetFileResponse>(
            "get_file",
            new GetFileParams { File = resourceId },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.Url ?? data.File ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<string>>> GetCustomFaceUrlListAsync(CancellationToken ct = default)
    {
        ApiResult<List<string>> resp = await CallActionAsync<List<string>>(
            "fetch_custom_face",
            new FetchCustomFaceParams { Count = 48 },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<IReadOnlyList<string>>.Ok(data)
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
        // OB11 ignores scene and peerId — uses message_id directly
        ApiResult<GetMsgResponse> resp = await CallActionAsync<GetMsgResponse>(
            "get_msg",
            new GetMsgParams { MessageId = (int)messageSeq },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<MessageContext>.Ok(data.Adapt<MessageContext>())
            : ApiResult<MessageContext>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<HistoryMessagesResult>> GetHistoryMessagesAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId?        startMessageSeq = null,
        int               limit           = 20,
        CancellationToken ct              = default)
    {
        switch (scene)
        {
            case MessageSourceType.Group:
            {
                ApiResult<GetMsgHistoryResponse> resp = await CallActionAsync<GetMsgHistoryResponse>(
                    "get_group_msg_history",
                    new GetGroupMsgHistoryParams
                        {
                            GroupId    = peerId,
                            MessageSeq = startMessageSeq.HasValue ? (int)startMessageSeq.Value : null,
                            Count      = limit
                        },
                    ct);
                return resp is { IsSuccess: true, Data: { } data }
                    ? ApiResult<HistoryMessagesResult>.Ok(data.Adapt<HistoryMessagesResult>())
                    : ApiResult<HistoryMessagesResult>.Fail(resp.Code, resp.Message);
            }
            case MessageSourceType.Friend:
            {
                ApiResult<GetMsgHistoryResponse> resp = await CallActionAsync<GetMsgHistoryResponse>(
                    "get_friend_msg_history",
                    new GetFriendMsgHistoryParams
                        {
                            UserId     = peerId,
                            MessageSeq = startMessageSeq.HasValue ? (int)startMessageSeq.Value : null,
                            Count      = limit
                        },
                    ct);
                return resp is { IsSuccess: true, Data: { } data }
                    ? ApiResult<HistoryMessagesResult>.Ok(data.Adapt<HistoryMessagesResult>())
                    : ApiResult<HistoryMessagesResult>.Fail(resp.Code, resp.Message);
            }
            case MessageSourceType.Temp:
            default:
                return ApiResult<HistoryMessagesResult>.Fail(ApiStatusCode.Unknown, $"Unknown MessageSourceType: {scene}");
        }
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<MessageContext>>> GetForwardMessagesAsync(
        string            forwardId,
        CancellationToken ct = default)
    {
        ApiResult<GetForwardMsgResponse> resp =
            await CallActionAsync<GetForwardMsgResponse>("get_forward_msg", new GetForwardMsgParams { Id = forwardId }, ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<MessageContext>>.Fail(resp.Code, resp.Message);

        List<MessageContext> contexts = [];
        if (data.Messages is { Count: > 0 })
            foreach (ForwardMsgNode node in data.Messages)
            {
                MessageBody body = MessageConverter.ToMessageBody(node.Content);
                contexts.Add(
                    new MessageContext
                        {
                            SenderId   = node.Sender?.UserId ?? 0,
                            SenderName = node.Sender?.Nickname ?? "",
                            Time       = DateTimeOffset.FromUnixTimeSeconds(node.Time).LocalDateTime,
                            Body       = body
                        });
            }

        return ApiResult<IReadOnlyList<MessageContext>>.Ok(contexts);
    }


    /// <inheritdoc />
    public async ValueTask<SendMessageResult> SendFriendMessageAsync(
        UserId            userId,
        MessageBody       message,
        CancellationToken ct = default)
    {
        // Forward messages use a separate API
        ForwardSegment? forward = message.GetFirst<ForwardSegment>();
        if (forward is not null && forward.Messages.Count > 0)
        {
            List<JObject> nodes = MessageConverter.ConvertForwardNodes(forward);
            _logger.LogInformation(
                "Sending OB11 private forward message to user[{UserId}] with {NodeCount} node(s)",
                userId,
                nodes.Count);
            ApiResult<SendMsgResponse> fwResp = await CallActionAsync<SendMsgResponse>(
                "send_private_forward_msg",
                new SendPrivateForwardMsgParams
                        { UserId = userId, Messages = nodes },
                ct);
            if (fwResp is { IsSuccess: true, Data: { } fwData })
            {
                _logger.LogDebug(
                    "OB11 private forward message sent to user[{UserId}], messageId={MessageId}",
                    userId,
                    fwData.MessageId);
                return SendMessageResult.Ok(fwData.MessageId);
            }

            _logger.LogWarning(
                "OB11 private forward message send failed for user[{UserId}]: code={Code}, message={Message}",
                userId,
                fwResp.Code,
                fwResp.Message);
            return SendMessageResult.Fail(fwResp.Code, fwResp.Message);
        }

        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(message);
        if (segments.Count == 0)
        {
            _logger.LogWarning(
                "Cannot send OB11 private message to user[{UserId}]: no valid segments remain after conversion",
                userId);
            return SendMessageResult.Fail(ApiStatusCode.InvalidMessage, "Empty message");
        }

        IReadOnlyList<string> issues = message.Validate();
        if (issues.Count > 0)
        {
            _logger.LogWarning(
                "Cannot send OB11 private message to user[{UserId}]: validation failed with {IssueCount} issue(s): {Issues}",
                userId,
                issues.Count,
                string.Join(" | ", issues));
            return SendMessageResult.Fail(ApiStatusCode.InvalidMessage, string.Join(Environment.NewLine, issues));
        }

        _logger.LogInformation(
            "Sending OB11 private message to user[{UserId}] with {SegmentCount} segment(s)",
            userId,
            segments.Count);
        ApiResult<SendMsgResponse> resp = await CallActionAsync<SendMsgResponse>(
            "send_private_msg",
            new SendPrivateMsgParams
                {
                    UserId  = userId,
                    Message = segments
                },
            ct);
        if (resp is { IsSuccess: true, Data: { } data })
        {
            _logger.LogDebug("OB11 private message sent to user[{UserId}], messageId={MessageId}", userId, data.MessageId);
            return SendMessageResult.Ok(data.MessageId);
        }

        _logger.LogWarning(
            "OB11 private message send failed for user[{UserId}]: code={Code}, message={Message}",
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
        // Forward messages use a separate API
        ForwardSegment? forward = message.GetFirst<ForwardSegment>();
        if (forward is not null && forward.Messages.Count > 0)
        {
            List<JObject> nodes = MessageConverter.ConvertForwardNodes(forward);
            _logger.LogDebug("Sending OB11 group forward message to {GroupId} with {NodeCount} node(s)", groupId, nodes.Count);
            ApiResult<SendMsgResponse> fwResp = await CallActionAsync<SendMsgResponse>(
                "send_group_forward_msg",
                new SendGroupForwardMsgParams
                        { GroupId = groupId, Messages = nodes },
                ct);
            if (fwResp is { IsSuccess: true, Data: { } fwData })
            {
                _logger.LogDebug(
                    "OB11 group forward message sent to group[{GroupId}], messageId={MessageId}",
                    groupId,
                    fwData.MessageId);
                return SendMessageResult.Ok(fwData.MessageId);
            }

            _logger.LogWarning(
                "OB11 group forward message send failed for group[{GroupId}]: code={Code}, message={Message}",
                groupId,
                fwResp.Code,
                fwResp.Message);
            return SendMessageResult.Fail(fwResp.Code, fwResp.Message);
        }

        List<OneBotSegment> segments = MessageConverter.ToOneBotSegments(message);
        if (segments.Count == 0)
        {
            _logger.LogWarning(
                "Cannot send OB11 group message to group[{GroupId}]: no valid segments remain after conversion",
                groupId);
            return SendMessageResult.Fail(ApiStatusCode.InvalidMessage, "Empty message");
        }

        IReadOnlyList<string> issues = message.Validate();
        if (issues.Count > 0)
        {
            _logger.LogWarning(
                "Cannot send OB11 group message to group[{GroupId}]: validation failed with {IssueCount} issue(s): {Issues}",
                groupId,
                issues.Count,
                string.Join(" | ", issues));
            return SendMessageResult.Fail(ApiStatusCode.InvalidMessage, string.Join(Environment.NewLine, issues));
        }

        _logger.LogDebug("Sending OB11 group message to group[{GroupId}] with {SegmentCount} segment(s)", groupId, segments.Count);
        ApiResult<SendMsgResponse> resp = await CallActionAsync<SendMsgResponse>(
            "send_group_msg",
            new SendGroupMsgParams
                {
                    GroupId = groupId,
                    Message = segments
                },
            ct);
        if (resp is { IsSuccess: true, Data: { } data })
        {
            _logger.LogDebug("OB11 group message sent to group[{GroupId}], messageId={MessageId}", groupId, data.MessageId);
            return SendMessageResult.Ok(data.MessageId);
        }

        _logger.LogWarning(
            "OB11 group message send failed for group[{GroupId}]: code={Code}, message={Message}",
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
        await CallActionAsync("delete_msg", new DeleteMsgParams { MessageId = (int)messageId }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> RecallPrivateMessageAsync(
        UserId            userId,
        MessageId         messageId,
        CancellationToken ct = default) =>
        await CallActionAsync("delete_msg", new DeleteMsgParams { MessageId = (int)messageId }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> MarkMessageAsReadAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId         messageSeq,
        CancellationToken ct = default) =>
        await CallActionAsync("mark_msg_as_read", new MarkMsgAsReadParams { MessageId = (int)messageSeq }, ct);

#endregion

#region IBotApi — User & Friend

    /// <inheritdoc />
    public async ValueTask<ApiResult<UserInfo>> GetUserInfoAsync(
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetStrangerInfoResponse> resp = await CallActionAsync<GetStrangerInfoResponse>(
            "get_stranger_info",
            new
                GetStrangerInfoParams
                    {
                        UserId  = userId,
                        NoCache = noCache
                    },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<UserInfo>.Ok(data.Adapt<UserInfo>())
            : ApiResult<UserInfo>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<UserProfile>> GetUserProfileAsync(UserId userId, CancellationToken ct = default)
    {
        ApiResult<GetStrangerInfoResponse> resp = await CallActionAsync<GetStrangerInfoResponse>(
            "get_stranger_info",
            new GetStrangerInfoParams { UserId = userId, NoCache = true },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<UserProfile>.Ok(
                new UserProfile
                    {
                        UserId   = data.UserId,
                        Nickname = data.Nickname ?? "",
                        Age      = data.Age,
                        Sex      = (data.Sex ?? "").Adapt<Sex>()
                    })
            : ApiResult<UserProfile>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<FriendInfo>> GetFriendInfoAsync(
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        // OB11 doesn't have get_friend_info; use get_stranger_info as fallback
        ApiResult<UserInfo> userResult = await GetUserInfoAsync(userId, noCache, ct);
        return userResult is { IsSuccess: true, Data: { } userData }
            ? ApiResult<FriendInfo>.Ok(userData.Adapt<FriendInfo>())
            : ApiResult<FriendInfo>.Fail(userResult.Code, userResult.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<FriendInfo>>> GetFriendListAsync(
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<List<GetFriendListItem>> resp =
            await CallActionAsync<List<GetFriendListItem>>("get_friend_list", null, ct);
        if (resp is not { IsSuccess: true, Data: { } data }) return ApiResult<IReadOnlyList<FriendInfo>>.Fail(resp.Code, resp.Message);
        List<FriendInfo> friends = data.Select(f => f.Adapt<FriendInfo>()).ToList();
        return ApiResult<IReadOnlyList<FriendInfo>>.Ok(friends);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<FriendRequestInfo>>> GetFriendRequestsAsync(
        int               limit      = 20,
        bool              isFiltered = false,
        CancellationToken ct         = default)
    {
        ApiResult<List<DoubtFriendRequestItem>> resp = await CallActionAsync<List<DoubtFriendRequestItem>>(
            "get_doubt_friends_add_request",
            new GetDoubtFriendsAddRequestParams { Count = limit },
            ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<FriendRequestInfo>>.Fail(resp.Code, resp.Message);
        List<FriendRequestInfo> requests = data.Select(r => r.Adapt<FriendRequestInfo>()).ToList();
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
            "Handling OB11 friend request from user[{UserId}] (filtered: {IsFiltered}, approve: {Approve})",
            fromUserId,
            isFiltered,
            approve);

        if (isFiltered)
        {
            // Doubt/filtered requests: query via get_doubt_friends_add_request and use set_doubt_friends_add_request
            ApiResult<List<DoubtFriendRequestItem>> doubtResp =
                await CallActionAsync<List<DoubtFriendRequestItem>>(
                    "get_doubt_friends_add_request",
                    new GetDoubtFriendsAddRequestParams { Count = 50 },
                    ct);
            if (doubtResp is not { IsSuccess: true, Data: { } doubtData })
                return ApiResult.Fail(doubtResp.Code, doubtResp.Message);

            DoubtFriendRequestItem? match = doubtData
                .FirstOrDefault(r => r.Uin == ((long)fromUserId).ToString());
            if (match is null)
            {
                _logger.LogWarning("No matching OB11 filtered friend request was found for user[{UserId}]", fromUserId);
                return ApiResult.Fail(ApiStatusCode.Failed, "No matching doubt friend request found");
            }

            return await CallActionAsync(
                "set_doubt_friends_add_request",
                new SetFriendAddRequestParams { Flag = match.Flag ?? "", Approve = approve, Remark = remark },
                ct);
        }

        // Normal requests: look up stored flag from event
        if (!_friendRequestFlags.TryRemove(fromUserId, out string? flag))
        {
            _logger.LogWarning("No pending OB11 friend request flag was found for user[{UserId}]", fromUserId);
            return ApiResult.Fail(ApiStatusCode.Failed, "No pending friend request flag found for this user");
        }

        return await CallActionAsync(
            "set_friend_add_request",
            new SetFriendAddRequestParams { Flag = flag, Approve = approve, Remark = remark },
            ct);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult> DeleteFriendAsync(UserId userId, CancellationToken ct = default) =>
        await CallActionAsync("delete_friend", new DeleteFriendParams { UserId = userId }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SendFriendNudgeAsync(UserId userId, CancellationToken ct = default) =>
        await CallActionAsync("friend_poke", new FriendPokeParams { UserId = userId }, ct);

#endregion

#region IBotApi — Group Info

    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupInfo>> GetGroupInfoAsync(
        GroupId           groupId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetGroupInfoResponse> resp = await CallActionAsync<GetGroupInfoResponse>(
            "get_group_info",
            new GetGroupInfoParams
                {
                    GroupId = groupId,
                    NoCache = noCache
                },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<GroupInfo>.Ok(data.Adapt<GroupInfo>())
            : ApiResult<GroupInfo>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<GroupInfo>>> GetGroupListAsync(
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<List<GetGroupInfoResponse>> resp =
            await CallActionAsync<List<GetGroupInfoResponse>>(
                "get_group_list",
                new GetGroupListParams { NoCache = noCache },
                ct);
        if (resp is not { IsSuccess: true, Data: { } data }) return ApiResult<IReadOnlyList<GroupInfo>>.Fail(resp.Code, resp.Message);
        List<GroupInfo> groups = data.Select(g => g.Adapt<GroupInfo>()).ToList();
        return ApiResult<IReadOnlyList<GroupInfo>>.Ok(groups);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupMemberInfo>> GetGroupMemberInfoAsync(
        GroupId           groupId,
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<GetGroupMemberInfoResponse> resp = await CallActionAsync<GetGroupMemberInfoResponse>(
            "get_group_member_info",
            new GetGroupMemberInfoParams
                {
                    GroupId = groupId,
                    UserId  = userId,
                    NoCache = noCache
                },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<GroupMemberInfo>.Ok(data.Adapt<GroupMemberInfo>())
            : ApiResult<GroupMemberInfo>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<GroupMemberInfo>>> GetGroupMemberListAsync(
        GroupId           groupId,
        bool              noCache = false,
        CancellationToken ct      = default)
    {
        ApiResult<List<GetGroupMemberInfoResponse>> resp = await CallActionAsync<List<GetGroupMemberInfoResponse>>(
            "get_group_member_list",
            new GetGroupMemberListParams { GroupId = groupId, NoCache = noCache },
            ct);

        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<GroupMemberInfo>>.Fail(resp.Code, resp.Message);
        List<GroupMemberInfo> members = data.Select(m => m.Adapt<GroupMemberInfo>()).ToList();
        return ApiResult<IReadOnlyList<GroupMemberInfo>>.Ok(members);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<GroupAnnouncementInfo>>> GetGroupAnnouncementsAsync(
        GroupId           groupId,
        CancellationToken ct = default)
    {
        ApiResult<List<GroupNoticeItem>> resp = await CallActionAsync<List<GroupNoticeItem>>(
            "_get_group_notice",
            new GetGroupNoticeParams { GroupId = groupId },
            ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<GroupAnnouncementInfo>>.Fail(resp.Code, resp.Message);
        List<GroupAnnouncementInfo> announcements = data.Select(a => a.Adapt<GroupAnnouncementInfo>()).ToList();
        return ApiResult<IReadOnlyList<GroupAnnouncementInfo>>.Ok(announcements);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupEssenceMessagesPage>> GetGroupEssenceMessagesAsync(
        GroupId           groupId,
        int               pageIndex,
        int               pageSize,
        CancellationToken ct = default)
    {
        ApiResult<List<EssenceMsgItem>> resp = await CallActionAsync<List<EssenceMsgItem>>(
            "get_essence_msg_list",
            new GetEssenceMsgListParams { GroupId = groupId },
            ct);

        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<GroupEssenceMessagesPage>.Ok(
                new GroupEssenceMessagesPage
                    {
                        // OB11 returns all essence messages at once; apply client-side pagination
                        Messages = data.Select(m => m.Adapt<GroupEssenceMessageInfo>())
                                       .Skip(pageIndex * pageSize)
                                       .Take(pageSize)
                                       .ToList(),
                        IsEnd = (pageIndex + 1) * pageSize >= data.Count
                    })
            : ApiResult<GroupEssenceMessagesPage>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupNotificationsResult>> GetGroupNotificationsAsync(
        long?             startNotificationSeq = null,
        bool              isFiltered           = false,
        int               limit                = 20,
        CancellationToken ct                   = default)
    {
        ApiResult<GetGroupSystemMsgResponse> resp = await CallActionAsync<GetGroupSystemMsgResponse>("get_group_system_msg", null, ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<GroupNotificationsResult>.Fail(resp.Code, resp.Message);

        List<GroupNotificationInfo> notifications = [];
        notifications.AddRange((data.JoinRequests ?? []).Select(r => r.Adapt<GroupNotificationInfo>()));
        notifications.AddRange((data.InvitedRequests ?? []).Select(r => r.Adapt<GroupNotificationInfo>()));

        return ApiResult<GroupNotificationsResult>.Ok(new GroupNotificationsResult { Notifications = notifications });
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
            "Handling OB11 group request for group[{GroupId}] (notificationSeq: {NotificationSeq}, type: {JoinType}, filtered: {IsFiltered}, approve: {Approve})",
            groupId,
            notificationSeq,
            joinNotificationType,
            isFiltered,
            approve);

        if (!_groupRequestFlags.TryRemove((groupId, notificationSeq), out string? flag))
        {
            _logger.LogWarning(
                "No pending OB11 group request flag was found for group group[{GroupId}] and notificationSeq {NotificationSeq}",
                groupId,
                notificationSeq);
            return ApiResult.Fail(ApiStatusCode.Failed, "No pending group request flag found");
        }

        return await CallActionAsync(
            "set_group_add_request",
            new SetGroupAddRequestParams
                {
                    Flag    = flag,
                    Approve = approve,
                    Reason  = reason
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
            "Handling OB11 group invitation for group[{GroupId}] (invitationSeq: {InvitationSeq}, approve: {Approve})",
            groupId,
            invitationSeq,
            approve);

        if (!_groupInvitationFlags.TryRemove((groupId, invitationSeq), out string? flag))
        {
            _logger.LogWarning(
                "No pending OB11 group invitation flag was found for group group[{GroupId}] and invitationSeq {InvitationSeq}",
                groupId,
                invitationSeq);
            return ApiResult.Fail(ApiStatusCode.Failed, "No pending group invitation flag found");
        }

        return await CallActionAsync(
            "set_group_add_request",
            new SetGroupAddRequestParams
                {
                    Flag    = flag,
                    Approve = approve
                },
            ct);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupNameAsync(GroupId groupId, string name, CancellationToken ct = default) =>
        await CallActionAsync(
            "set_group_name",
            new SetGroupNameParams
                {
                    GroupId   = groupId,
                    GroupName = name
                },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupAvatarAsync(GroupId groupId, string imageUri, CancellationToken ct = default) =>
        await CallActionAsync("set_group_portrait", new SetGroupPortraitParams { GroupId = groupId, File = imageUri }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupAdminAsync(
        GroupId           groupId,
        UserId            userId,
        bool              enable,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "set_group_admin",
            new SetGroupAdminParams
                {
                    GroupId = groupId,
                    UserId  = userId,
                    Enable  = enable
                },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupMemberCardAsync(
        GroupId           groupId,
        UserId            userId,
        string            card,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "set_group_card",
            new SetGroupCardParams
                {
                    GroupId = groupId,
                    UserId  = userId,
                    Card    = card
                },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupMemberSpecialTitleAsync(
        GroupId           groupId,
        UserId            userId,
        string            title,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "set_group_special_title",
            new SetGroupSpecialTitleParams
                    { GroupId = groupId, UserId = userId, SpecialTitle = title },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupEssenceMessageAsync(
        GroupId           groupId,
        MessageId         messageSeq,
        bool              isSet = true,
        CancellationToken ct    = default) =>
        await CallActionAsync(
            isSet ? "set_essence_msg" : "delete_essence_msg",
            new SetEssenceMsgParams { MessageId = (int)messageSeq },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SendGroupAnnouncementAsync(
        GroupId           groupId,
        string            content,
        string?           imageUri = null,
        CancellationToken ct       = default) =>
        await CallActionAsync(
            "_send_group_notice",
            new SendGroupNoticeParams { GroupId = groupId, Content = content, Image = imageUri },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> DeleteGroupAnnouncementAsync(
        GroupId           groupId,
        string            announcementId,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "_delete_group_notice",
            new DeleteGroupNoticeParams { GroupId = groupId, NoticeId = announcementId },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> KickGroupMemberAsync(
        GroupId           groupId,
        UserId            userId,
        bool              rejectFuture = false,
        CancellationToken ct           = default) =>
        await CallActionAsync(
            "set_group_kick",
            new SetGroupKickParams
                {
                    GroupId          = groupId,
                    UserId           = userId,
                    RejectAddRequest = rejectFuture
                },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> MuteGroupMemberAsync(
        GroupId           groupId,
        UserId            userId,
        int               durationSeconds,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "set_group_ban",
            new SetGroupBanParams
                {
                    GroupId  = groupId,
                    UserId   = userId,
                    Duration = durationSeconds
                },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> MuteGroupAllAsync(GroupId groupId, bool enable, CancellationToken ct = default) =>
        await CallActionAsync(
            "set_group_whole_ban",
            new SetGroupWholeBanParams
                {
                    GroupId = groupId,
                    Enable  = enable
                },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> LeaveGroupAsync(GroupId groupId, CancellationToken ct = default) =>
        await CallActionAsync("set_group_leave", new SetGroupLeaveParams { GroupId = groupId }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SendGroupNudgeAsync(GroupId groupId, UserId userId, CancellationToken ct = default) =>
        await CallActionAsync("group_poke", new GroupPokeParams { GroupId = groupId, UserId = userId }, ct);

#endregion

#region IBotApi — File Operations

    /// <inheritdoc />
    public async ValueTask<ApiResult<GroupFilesResult>> GetGroupFilesAsync(
        GroupId           groupId,
        string            parentFolderId = "/",
        CancellationToken ct             = default)
    {
        bool isRoot = string.IsNullOrEmpty(parentFolderId) || parentFolderId == "/";
        ApiResult<GetGroupFilesResponse> resp = isRoot
            ? await CallActionAsync<GetGroupFilesResponse>(
                "get_group_root_files",
                new GetGroupRootFilesParams { GroupId = groupId },
                ct)
            : await CallActionAsync<GetGroupFilesResponse>(
                "get_group_files_by_folder",
                new GetGroupFilesByFolderParams
                        { GroupId = groupId, FolderId = parentFolderId },
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
        ApiResult<GetGroupFileUrlResponse> resp = await CallActionAsync<GetGroupFileUrlResponse>(
            "get_group_file_url",
            new GetGroupFileUrlParams { GroupId = groupId, FileId = fileId },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.Url ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> GetPrivateFileDownloadUrlAsync(
        UserId            userId,
        string            fileId,
        string            fileHash,
        CancellationToken ct = default)
    {
        ApiResult<GetPrivateFileUrlResponse> resp = await CallActionAsync<GetPrivateFileUrlResponse>(
            "get_private_file_url",
            new GetPrivateFileUrlParams { FileId = fileId },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.Url ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> CreateGroupFolderAsync(
        GroupId           groupId,
        string            folderName,
        CancellationToken ct = default)
    {
        ApiResult<CreateGroupFileFolderResponse> resp = await CallActionAsync<CreateGroupFileFolderResponse>(
            "create_group_file_folder",
            new CreateGroupFileFolderParams { GroupId = groupId, Name = folderName },
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
        ApiResult result = await CallActionAsync(
            "upload_group_file",
            new UploadGroupFileParams
                {
                    GroupId = groupId,
                    File    = fileUri,
                    Name    = fileName,
                    Folder  = parentFolderId == "/" ? "" : parentFolderId
                },
            ct);
        return result.IsSuccess
            ? ApiResult<string>.Ok("")
            : ApiResult<string>.Fail(result.Code, result.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> UploadPrivateFileAsync(
        UserId            userId,
        string            fileUri,
        string            fileName,
        CancellationToken ct = default)
    {
        ApiResult<UploadPrivateFileResponse> resp = await CallActionAsync<UploadPrivateFileResponse>(
            "upload_private_file",
            new UploadPrivateFileParams { UserId = userId, File = fileUri, Name = fileName },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.FileId ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult> DeleteGroupFileAsync(GroupId groupId, string fileId, CancellationToken ct = default) =>
        await CallActionAsync("delete_group_file", new DeleteGroupFileParams { GroupId = groupId, FileId = fileId }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult>
        DeleteGroupFolderAsync(GroupId groupId, string folderId, CancellationToken ct = default) =>
        await CallActionAsync("delete_group_folder", new DeleteGroupFolderParams { GroupId = groupId, FolderId = folderId }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> MoveGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        string            parentFolderId,
        string            targetFolderId,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "move_group_file",
            new MoveGroupFileParams
                {
                    GroupId         = groupId,
                    FileId          = fileId,
                    ParentDirectory = parentFolderId,
                    TargetDirectory = targetFolderId
                },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> RenameGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        string            parentFolderId,
        string            newFileName,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "rename_group_file",
            new RenameGroupFileParams
                {
                    GroupId                = groupId,
                    FileId                 = fileId,
                    CurrentParentDirectory = parentFolderId,
                    NewName                = newFileName
                },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> RenameGroupFolderAsync(
        GroupId           groupId,
        string            folderId,
        string            newName,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "rename_group_file_folder",
            new RenameGroupFileFolderParams { GroupId = groupId, FolderId = folderId, NewFolderName = newName },
            ct);

#endregion

#region IBotApi — Profile

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetAvatarAsync(string uri, CancellationToken ct = default) =>
        await CallActionAsync("set_qq_avatar", new SetQQAvatarParams { File = uri }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetBioAsync(string bio, CancellationToken ct = default) =>
        await CallActionAsync("set_qq_profile", new SetQQProfileParams { PersonalNote = bio }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetNicknameAsync(string nickname, CancellationToken ct = default) =>
        await CallActionAsync("set_qq_profile", new SetQQProfileParams { Nickname = nickname }, ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SendProfileLikeAsync(UserId userId, int count = 1, CancellationToken ct = default) =>
        await CallActionAsync("send_like", new SendLikeParams { UserId = userId, Times = count }, ct);

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
        await CallActionAsync(
            isAdd ? "set_msg_emoji_like" : "unset_msg_emoji_like",
            new SetMsgEmojiLikeParams { MessageId = (int)messageSeq, EmojiId = faceId, Set = isAdd },
            ct);

#endregion

#region IOneBot11ExtApi — Query

    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<string>>> FetchCustomFaceAsync(
        int               count = 48,
        CancellationToken ct    = default)
    {
        ApiResult<List<string>> resp = await CallActionAsync<List<string>>(
            "fetch_custom_face",
            new FetchCustomFaceParams { Count = count },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<IReadOnlyList<string>>.Ok(data)
            : ApiResult<IReadOnlyList<string>>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<FriendCategory>>> GetFriendsWithCategoryAsync(
        CancellationToken ct = default)
    {
        ApiResult<List<GetFriendsWithCategoryItem>> resp =
            await CallActionAsync<List<GetFriendsWithCategoryItem>>("get_friends_with_category", null, ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<FriendCategory>>.Fail(resp.Code, resp.Message);
        List<FriendCategory> categories = data.Select(c => new FriendCategory
                                                  {
                                                      CategoryId   = c.CategoryId,
                                                      CategoryName = c.CategoryName ?? "",
                                                      FriendCount  = c.CategoryMbCount,
                                                      OnlineCount  = c.OnlineCount,
                                                      SortId       = c.CategorySortId,
                                                      Friends = c.BuddyList?.Select(f => f.Adapt<FriendInfo>()).ToList()
                                                                ?? (IReadOnlyList<FriendInfo>)[]
                                                  })
                                              .ToList();
        return ApiResult<IReadOnlyList<FriendCategory>>.Ok(categories);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<IReadOnlyList<GroupMemberInfo>>> GetGroupShutListAsync(
        GroupId           groupId,
        CancellationToken ct = default)
    {
        ApiResult<List<GetGroupMemberInfoResponse>> resp =
            await CallActionAsync<List<GetGroupMemberInfoResponse>>(
                "get_group_shut_list",
                new GetGroupShutListParams { GroupId = groupId },
                ct);
        if (resp is not { IsSuccess: true, Data: { } data })
            return ApiResult<IReadOnlyList<GroupMemberInfo>>.Fail(resp.Code, resp.Message);
        List<GroupMemberInfo> members = data.Select(m => m.Adapt<GroupMemberInfo>()).ToList();
        return ApiResult<IReadOnlyList<GroupMemberInfo>>.Ok(members);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<OcrResult>> OcrImageAsync(
        string            imageUri,
        CancellationToken ct = default)
    {
        ApiResult<OcrImageResponse> resp = await CallActionAsync<OcrImageResponse>(
            "ocr_image",
            new OcrImageParams { Image = imageUri },
            ct);
        if (resp is not { IsSuccess: true, Data: { } data }) return ApiResult<OcrResult>.Fail(resp.Code, resp.Message);
        OcrResult result = new()
            {
                Language = data.Language ?? "",
                Texts = data.Texts?.Select(t => new OcrTextDetection
                                {
                                    Text       = t.Text ?? "",
                                    Confidence = t.Confidence
                                })
                            .ToList()
                        ?? []
            };
        return ApiResult<OcrResult>.Ok(result);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<string>> VoiceMsgToTextAsync(
        MessageId         messageId,
        CancellationToken ct = default)
    {
        ApiResult<VoiceMsgToTextResponse> resp = await CallActionAsync<VoiceMsgToTextResponse>(
            "voice_msg_to_text",
            new VoiceMsgToTextParams { MessageId = messageId },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<string>.Ok(data.Text ?? "")
            : ApiResult<string>.Fail(resp.Code, resp.Message);
    }

#endregion

#region IOneBot11ExtApi — Message Forwarding

    /// <inheritdoc />
    public async ValueTask<ApiResult<MessageId>> ForwardFriendSingleMsgAsync(
        UserId            userId,
        MessageId         messageId,
        CancellationToken ct = default)
    {
        ApiResult<ForwardSingleMsgResponse> resp = await CallActionAsync<ForwardSingleMsgResponse>(
            "forward_friend_single_msg",
            new ForwardSingleMsgParams { UserId = userId, MessageId = messageId },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<MessageId>.Ok(data.MessageId)
            : ApiResult<MessageId>.Fail(resp.Code, resp.Message);
    }


    /// <inheritdoc />
    public async ValueTask<ApiResult<MessageId>> ForwardGroupSingleMsgAsync(
        GroupId           groupId,
        MessageId         messageId,
        CancellationToken ct = default)
    {
        ApiResult<ForwardSingleMsgResponse> resp = await CallActionAsync<ForwardSingleMsgResponse>(
            "forward_group_single_msg",
            new ForwardSingleMsgParams { GroupId = groupId, MessageId = messageId },
            ct);
        return resp is { IsSuccess: true, Data: { } data }
            ? ApiResult<MessageId>.Ok(data.MessageId)
            : ApiResult<MessageId>.Fail(resp.Code, resp.Message);
    }

#endregion

#region IOneBot11ExtApi — Friend & Group Settings

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetFriendCategoryAsync(
        UserId            userId,
        int               categoryId,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "set_friend_category",
            new SetFriendCategoryParams { UserId = userId, CategoryId = categoryId },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetFriendRemarkAsync(
        UserId            userId,
        string            remark,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "set_friend_remark",
            new SetFriendRemarkParams { UserId = userId, Remark = remark },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SetGroupRemarkAsync(
        GroupId           groupId,
        string            remark,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "set_group_remark",
            new SetGroupRemarkParams { GroupId = groupId, Remark = remark },
            ct);

#endregion

#region IOneBot11ExtApi — Status & Actions

    /// <inheritdoc />
    public async ValueTask<ApiResult> SetOnlineStatusAsync(
        int               status,
        int               extStatus,
        int               batteryStatus,
        CancellationToken ct = default) =>
        await CallActionAsync(
            "set_online_status",
            new SetOnlineStatusParams { Status = status, ExtStatus = extStatus, BatteryStatus = batteryStatus },
            ct);


    /// <inheritdoc />
    public async ValueTask<ApiResult> SendFriendNudgeAsync(
        UserId            userId,
        UserId?           targetId = null,
        CancellationToken ct       = default) =>
        await CallActionAsync(
            "friend_poke",
            new FriendPokeParams
                {
                    UserId   = userId,
                    TargetId = targetId.HasValue ? (long)targetId.Value : null
                },
            ct);

#endregion

#region Flag Storage

    /// <summary>Stores a normal friend request flag for later retrieval.</summary>
    /// <param name="userId">The user ID from the friend request event.</param>
    /// <param name="flag">The protocol-specific flag string.</param>
    internal void StoreFriendRequestFlag(long userId, string flag)
    {
        _friendRequestFlags[userId] = flag;
        _logger.LogDebug("Stored OB11 friend request flag for user[{UserId}]", userId);
    }


    /// <summary>Stores a group invitation flag for later retrieval.</summary>
    /// <param name="groupId">The group ID from the invitation event.</param>
    /// <param name="invitorId">The invitor's user ID.</param>
    /// <param name="flag">The protocol-specific flag string.</param>
    internal void StoreGroupInvitationFlag(long groupId, long invitorId, string flag)
    {
        _groupInvitationFlags[(groupId, invitorId)] = flag;
        _logger.LogDebug("Stored OB11 group invitation flag for group[{GroupId}], invitor {InvitorId}", groupId, invitorId);
    }


    /// <summary>Stores a group join request flag for later retrieval.</summary>
    /// <param name="groupId">The group ID from the request event.</param>
    /// <param name="userId">The requesting user ID.</param>
    /// <param name="flag">The protocol-specific flag string.</param>
    internal void StoreGroupRequestFlag(long groupId, long userId, string flag)
    {
        _groupRequestFlags[(groupId, userId)] = flag;
        _logger.LogDebug("Stored OB11 group request flag for group[{GroupId}], user[{UserId}]", groupId, userId);
    }

#endregion

#region Private Helpers

    /// <summary>Sends an OB11 API action and deserializes the response.</summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="action">The API action name.</param>
    /// <param name="parameters">The request parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The deserialized API result.</returns>
    private async ValueTask<ApiResult<T>> CallActionAsync<T>(string action, object? parameters, CancellationToken ct)
        where T : class
    {
        try
        {
            OneBotApiResponse response = await _apiManager.SendRequestAsync(action, parameters, _sendFunc, ct);
            if (response.RetCode != 0)
            {
                _logger.LogWarning(
                    "OB11 API action [{Action}] failed: retCode={RetCode}, status={Status}",
                    action,
                    response.RetCode,
                    response.Status ?? "");
                return ApiResult<T>.Fail(MapRetCode(response.RetCode), response.Status ?? "");
            }

            T? data = response.Data?.ToObject<T>();
            return data is not null
                ? ApiResult<T>.Ok(data)
                : LogEmptyDataFailure<T>(action);
        }
        catch (TimeoutException)
        {
            return ApiResult<T>.Fail(ApiStatusCode.Timeout, "API call timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OB11 API action [{Action}] threw an exception", action);
            return ApiResult<T>.Fail(ApiStatusCode.InternalError, ex.Message);
        }
    }


    /// <summary>Sends an OB11 API action without deserializing response data.</summary>
    /// <param name="action">The API action name.</param>
    /// <param name="parameters">The request parameters.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A simple success/failure API result.</returns>
    private async ValueTask<ApiResult> CallActionAsync(string action, object? parameters, CancellationToken ct)
    {
        try
        {
            OneBotApiResponse response = await _apiManager.SendRequestAsync(action, parameters, _sendFunc, ct);
            if (response.RetCode == 0)
                return ApiResult.Ok();

            _logger.LogWarning(
                "OB11 API action [{Action}] failed: retCode={RetCode}, status={Status}",
                action,
                response.RetCode,
                response.Status ?? "");
            return ApiResult.Fail(MapRetCode(response.RetCode), response.Status ?? "");
        }
        catch (TimeoutException)
        {
            return ApiResult.Fail(ApiStatusCode.Timeout, "API call timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OB11 API action [{Action}] threw an exception", action);
            return ApiResult.Fail(ApiStatusCode.InternalError, ex.Message);
        }
    }

    private ApiResult<T> LogEmptyDataFailure<T>(string action) where T : class
    {
        _logger.LogWarning("OB11 API action [{Action}] succeeded but returned empty response data", action);
        return ApiResult<T>.Fail(ApiStatusCode.Failed, "Empty response data");
    }


    /// <summary>Maps an OB11 return code to an <see cref="ApiStatusCode" />.</summary>
    /// <param name="retCode">The OB11 API return code.</param>
    /// <returns>The corresponding API status code.</returns>
    private static ApiStatusCode MapRetCode(int retCode) =>
        retCode switch
            {
                0            => ApiStatusCode.Ok,
                1            => ApiStatusCode.Failed,
                1400         => ApiStatusCode.Failed,
                1401 or 1403 => ApiStatusCode.Forbidden,
                1404         => ApiStatusCode.NotFound,
                _            => ApiStatusCode.Unknown
            };

#endregion
}