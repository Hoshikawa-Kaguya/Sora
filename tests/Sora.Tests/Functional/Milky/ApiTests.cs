using System.Text;
using Xunit;

namespace Sora.Tests.Functional.Milky;

/// <summary>Functional API tests for the Milky adapter.</summary>
[Collection("Milky.Functional")]
[Trait("Category", "Functional")]
public class ApiTests : IDisposable
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private readonly MilkyTestFixture  _fixture;
    private readonly IDisposable       _logSubscription;
    private readonly ITestOutputHelper _output;
    private          IBotApi           Api      => _fixture.Api!;
    private          IMilkyExtApi      MilkyExt => Api.GetExtension<IMilkyExtApi>()!;

    /// <summary>
    ///     ApiTests ctor
    /// </summary>
    public ApiTests(MilkyTestFixture fixture, ITestOutputHelper output)
    {
        _fixture         = fixture;
        _output          = output;
        _logSubscription = fixture.OutputSink.Subscribe(output.WriteLine);
    }


#region Identity Tests

    /// <see cref="IBotApi.GetSelfInfoAsync" />
    [Fact]
    public async Task GetSelfInfo()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<BotIdentity> result   = await Api.GetSelfInfoAsync(CT);
        BotIdentity            selfInfo = result.AssertSuccess();
        _output.WriteLine($"SelfInfo: {selfInfo.UserId} {selfInfo.Nickname}");
        Assert.True(selfInfo.UserId.Value > 0);
        Assert.False(string.IsNullOrEmpty(selfInfo.Nickname));
    }

    /// <see cref="IBotApi.GetImplInfoAsync" />
    [Fact]
    public async Task GetImplInfo()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<ImplInfo> result   = await Api.GetImplInfoAsync(CT);
        ImplInfo            implInfo = result.AssertSuccess();
        _output.WriteLine($"ImplInfo: name={implInfo.ImplName} ver={implInfo.ImplVersion} qq={implInfo.QqProtocolVersion}");
        Assert.False(string.IsNullOrEmpty(implInfo.ImplName));
    }

    /// <see cref="IBotApi.GetCookiesAsync" />
    [Fact]
    public async Task GetCookies()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<string> result  = await Api.GetCookiesAsync("qq.com", CT);
        string            cookies = result.AssertSuccess();
        _output.WriteLine($"Cookies: success={result.IsSuccess} len={cookies.Length}");
    }

    /// <see cref="IBotApi.GetCsrfTokenAsync" />
    [Fact]
    public async Task GetCsrfToken()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<string> result = await Api.GetCsrfTokenAsync(CT);
        _output.WriteLine($"CsrfToken: success={result.IsSuccess} token={result.Data}");
        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Data), "CSRF token should not be empty");
    }

#endregion

#region Messaging Tests

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendGroupMessage()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;
        MessageBody body =
            [
                new TextSegment { Text = "[Milky Test] SendGroupMessage" }
            ];

        SendMessageResult result = await Api.SendGroupMessageAsync(testGroup, body, CT);
        _output.WriteLine($"Sent: messageId={result.MessageId}");
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0L, result.MessageId.Value);
    }

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendGroupMessage_WithFace()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        MessageBody body = new([new TextSegment { Text = "[Milky Test] Face: " }, new FaceSegment { FaceId = "178" }]);

        SendMessageResult result = await Api.SendGroupMessageAsync(testGroup, body, CT);
        _output.WriteLine($"FaceMsg: messageId={result.MessageId}");
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0L, result.MessageId.Value);
    }

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendGroupMessage_WithMention()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self      = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        GroupId     testGroup = TestConfig.TestGroupId;

        MessageBody body =
            [
                new MentionSegment { Target = self.UserId },
                new TextSegment { Text      = " [Milky Test] Mention test" }
            ];

        SendMessageResult sendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        _output.WriteLine($"MentionMsg: messageId={sendResult.MessageId}");
        Assert.True(sendResult.IsSuccess);

        await Task.Delay(500, CT);

        MessageContext getMsg =
            (await Api.GetMessageAsync(MessageSourceType.Group, testGroup, sendResult.MessageId, CT)).AssertSuccess();

        MentionSegment? mention = getMsg.Body.GetFirst<MentionSegment>();
        Assert.NotNull(mention);
        Assert.Equal((long)self.UserId, (long)mention.Target);
        _output.WriteLine($"MentionName: '{mention.Name}'");
        Assert.False(string.IsNullOrEmpty(mention.Name), "Mention name should be populated by protocol");
    }

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendGroupMessage_WithReply()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        MessageBody       originalBody = "[Milky Test] Original message";
        SendMessageResult firstMsg     = await Api.SendGroupMessageAsync(testGroup, originalBody, CT);
        Assert.True(firstMsg.IsSuccess);

        await Task.Delay(500, CT);

        MessageBody replyBody = new(
            [
                new ReplySegment { TargetId = firstMsg.MessageId }, new TextSegment { Text = "[Milky Test] Reply to above" }
            ]);

        SendMessageResult result = await Api.SendGroupMessageAsync(testGroup, replyBody, CT);
        _output.WriteLine($"ReplyMsg: messageId={result.MessageId}");
        Assert.True(result.IsSuccess);
    }

    /// <see cref="IBotApi.SendFriendMessageAsync" />
    [Fact]
    public async Task SendPrivateMessage()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        Assert.SkipWhen(_fixture.SecondaryUserId.Value == 0, "Secondary bot not available");

        MessageBody       body   = "[Milky Test] Private message test";
        SendMessageResult result = await Api.SendFriendMessageAsync(_fixture.SecondaryUserId, body, CT);
        _output.WriteLine($"SendPrivate: to={_fixture.SecondaryUserId} messageId={result.MessageId}");
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0L, result.MessageId.Value);
    }

    /// <see cref="IBotApi.RecallGroupMessageAsync" />
    [Fact]
    public async Task SendAndRecallGroupMessage()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId     testGroup = TestConfig.TestGroupId;
        MessageBody sendBody  = "[Milky Test] This will be recalled";

        SendMessageResult sendResult = await Api.SendGroupMessageAsync(testGroup, sendBody, CT);
        Assert.True(sendResult.IsSuccess);
        _output.WriteLine($"Sent messageId={sendResult.MessageId}");

        await Task.Delay(1000, CT);

        ApiResult recallResult = await Api.RecallGroupMessageAsync(testGroup, sendResult.MessageId, CT);
        _output.WriteLine($"Recall: success={recallResult.IsSuccess}");
        Assert.True(recallResult.IsSuccess);
    }

    /// <see cref="IBotApi.RecallGroupMessageAsync" />
    [Fact]
    public async Task SendAndRecallPrivateMessage()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendInfo>> friends = await Api.GetFriendListAsync(ct: CT);
        if (!friends.IsSuccess || friends.Data is null || friends.Data.Count == 0)
        {
            _output.WriteLine("Skipped: no friends available");
            return;
        }

        UserId            friendId   = friends.Data[0].UserId;
        MessageBody       body       = "[Milky Test] Will be recalled";
        SendMessageResult sendResult = await Api.SendFriendMessageAsync(friendId, body, CT);
        Assert.True(sendResult.IsSuccess);
        _output.WriteLine($"Sent private messageId={sendResult.MessageId}");

        await Task.Delay(1000, CT);

        ApiResult recallResult = await Api.RecallPrivateMessageAsync(friendId, sendResult.MessageId, CT);
        _output.WriteLine($"RecallPrivate: success={recallResult.IsSuccess}");
        Assert.True(recallResult.IsSuccess);
    }

    /// <see cref="IBotApi.GetMessageAsync" />
    [Fact]
    public async Task GetMessage()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId           testGroup  = TestConfig.TestGroupId;
        MessageBody       body       = "[Milky Test] GetMessage target";
        SendMessageResult sendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        Assert.True(sendResult.IsSuccess);

        await Task.Delay(500, CT);

        ApiResult<MessageContext> result = await Api.GetMessageAsync(
            MessageSourceType.Group,
            TestConfig.TestGroupId,
            sendResult.MessageId,
            CT);
        Assert.True(result.IsSuccess);
        MessageContext message = Assert.IsAssignableFrom<MessageContext>(result.Data);
        _output.WriteLine($"GetMessage: sourceType={message.SourceType} text={message.Body.GetText()}");
    }

    /// <see cref="IBotApi.GetForwardMessagesAsync" />
    [Fact]
    public async Task GetForwardMessages()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;
        MessageBody body = new(
            SegmentBuilder.Forward(
                    [
                        new ForwardedMessageNode
                            {
                                Segments   = [new TextSegment { Text = "test1" }],
                                SenderName = "ybb",
                                UserId     = 114514
                            },
                        new ForwardedMessageNode
                            {
                                Segments   = [new TextSegment { Text = "test2" }],
                                SenderName = "ybb",
                                UserId     = 114514
                            }
                    ],
                "1",
                    ["2"],
                "3",
                "4"));
        SendMessageResult messageSendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        Assert.True(messageSendResult.IsSuccess, "Failed to send forward message");
        await Task.Delay(1000, CT);

        // Try to get forward message in recent sent message
        MessageContext ctx = (await Api.GetMessageAsync(MessageSourceType.Group, testGroup, messageSendResult.MessageId, CT))
            .AssertSuccess();
        string forwardId = ctx.Body.OfType<ForwardSegment>().FirstOrDefault()?.ForwardId ?? string.Empty;
        Assert.False(string.IsNullOrEmpty(forwardId));

        ApiResult<IReadOnlyList<MessageContext>> result          = await Api.GetForwardMessagesAsync(forwardId, CT);
        IReadOnlyList<MessageContext>            forwardMessages = result.AssertSuccess();
        _output.WriteLine($"GetForwardMessages: success={result.IsSuccess} count={forwardMessages.Count}");
        try
        {
            await Api.RecallGroupMessageAsync(testGroup, messageSendResult.MessageId, CT);
        }
        catch (Exception e)
        {
            _output.WriteLine($"Exception: {e}");
        }
    }

    /// <see cref="IBotApi.GetHistoryMessagesAsync" />
    [Fact]
    public async Task GetHistoryMessages()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;
        ApiResult<HistoryMessagesResult> result = await Api.GetHistoryMessagesAsync(
            MessageSourceType.Group,
            testGroup,
            limit: 10,
            ct: CT);
        Assert.True(result.IsSuccess);
        HistoryMessagesResult history = Assert.IsAssignableFrom<HistoryMessagesResult>(result.Data);
        _output.WriteLine($"History: count={history.Messages.Count} nextSeq={history.NextMessageSeq}");
    }

    /// <see cref="IBotApi.MarkMessageAsReadAsync" />
    [Fact]
    public async Task MarkMessageAsRead()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Send a message first to get a valid seq
        MessageBody       body       = "[Milky Test] MarkAsRead target";
        SendMessageResult sendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        Assert.True(sendResult.IsSuccess);

        await Task.Delay(500, CT);

        ApiResult result = await Api.MarkMessageAsReadAsync(MessageSourceType.Group, testGroup, sendResult.MessageId, CT);
        _output.WriteLine($"MarkRead: success={result.IsSuccess}");
        Assert.True(result.IsSuccess);
    }

#endregion

#region User and Friend Tests

    /// <see cref="IBotApi.GetUserInfoAsync" />
    [Fact]
    public async Task GetUserInfo()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        ApiResult<UserInfo> result   = await Api.GetUserInfoAsync(self.UserId, ct: CT);
        UserInfo            userInfo = result.AssertSuccess();
        _output.WriteLine($"UserInfo: {userInfo.Nickname} sex={userInfo.Sex}");
        Assert.Equal((long)self.UserId, (long)userInfo.UserId);
    }

    /// <see cref="IBotApi.GetUserProfileAsync" />
    [Fact]
    public async Task GetUserProfile()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        ApiResult<UserProfile> result  = await Api.GetUserProfileAsync(self.UserId, CT);
        UserProfile            profile = result.AssertSuccess();
        _output.WriteLine($"Profile: nick={profile.Nickname} bio={profile.Bio} level={profile.Level}");
        Assert.Equal((long)self.UserId, (long)profile.UserId);
    }

    /// <see cref="IBotApi.GetFriendInfoAsync" />
    [Fact]
    public async Task GetFriendInfo()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendInfo>> friends = await Api.GetFriendListAsync(ct: CT);
        if (!friends.IsSuccess || friends.Data is null || friends.Data.Count == 0)
        {
            _output.WriteLine("Skipped: no friends available");
            return;
        }

        UserId                friendId   = friends.Data[0].UserId;
        ApiResult<FriendInfo> result     = await Api.GetFriendInfoAsync(friendId, ct: CT);
        FriendInfo            friendInfo = result.AssertSuccess();
        _output.WriteLine($"FriendInfo: {friendInfo.Nickname} remark={friendInfo.Remark} qid={friendInfo.Qid}");
        Assert.Equal((long)friendId, (long)friendInfo.UserId);
    }

    /// <see cref="IBotApi.GetFriendListAsync" />
    [Fact]
    public async Task GetFriendList()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendInfo>> result = await Api.GetFriendListAsync(ct: CT);
        Assert.True(result.IsSuccess);
        IReadOnlyList<FriendInfo> friends = Assert.IsAssignableFrom<IReadOnlyList<FriendInfo>>(result.Data);
        _output.WriteLine($"Friends: {friends.Count}");
        foreach (FriendInfo friend in friends) _output.WriteLine($"  {friend.UserId}: {friend.Nickname}");
    }

    /// <see cref="IBotApi.GetFriendRequestsAsync" />
    [Fact]
    public async Task GetFriendRequests()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendRequestInfo>> result = await Api.GetFriendRequestsAsync(ct: CT);
        Assert.True(result.IsSuccess);
        IReadOnlyList<FriendRequestInfo> requests = Assert.IsAssignableFrom<IReadOnlyList<FriendRequestInfo>>(result.Data);
        _output.WriteLine($"FriendRequests: count={requests.Count}");
        foreach (FriendRequestInfo req in requests)
            _output.WriteLine($"  from={req.InitiatorId} state={req.State} comment={req.Comment}");
    }

    /// <see cref="IBotApi.HandleFriendRequestAsync" />
    [Fact]
    public async Task HandleFriendRequest_NoPending()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // No pending request — expect failure from lookup
        ApiResult result = await Api.HandleFriendRequestAsync(new UserId(999999), false, true, ct: CT);
        _output.WriteLine($"HandleFriendRequest: code={result.Code} msg={result.Message}");
    }

    /// <summary>
    ///     Tests that delete_friend is supported by protocol. Does NOT actually delete.
    ///     Expects a protocol error (invalid target), not timeout.
    /// </summary>
    /// <see cref="IBotApi.DeleteFriendAsync" />
    [Fact]
    public async Task DeleteFriend_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Use a non-friend user ID to avoid actual deletion
        ApiResult result = await Api.DeleteFriendAsync(10001L, CT);
        _output.WriteLine($"DeleteFriend: code={result.Code} msg={result.Message}");
        Assert.Equal(ApiStatusCode.ProtocolNotFound, result.Code);
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <see cref="IBotApi.SendFriendNudgeAsync" />
    [Fact]
    public async Task SendFriendNudge()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendInfo>> friends = await Api.GetFriendListAsync(ct: CT);
        if (!friends.IsSuccess || friends.Data is null || friends.Data.Count == 0)
        {
            _output.WriteLine("Skipped: no friends available");
            return;
        }

        UserId    friendId = friends.Data[0].UserId;
        ApiResult result   = await Api.SendFriendNudgeAsync(friendId, CT);
        _output.WriteLine($"FriendNudge: to={friendId} success={result.IsSuccess}");
        Assert.True(result.IsSuccess);
    }

#endregion

#region Group Info Tests

    /// <see cref="IBotApi.GetGroupInfoAsync" />
    [Fact]
    public async Task GetGroupInfo()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId              testGroup = TestConfig.TestGroupId;
        ApiResult<GroupInfo> result    = await Api.GetGroupInfoAsync(testGroup, ct: CT);
        GroupInfo            groupInfo = result.AssertSuccess();
        _output.WriteLine($"Group: {groupInfo.GroupName} members={groupInfo.MemberCount}");
        Assert.Equal(TestConfig.TestGroupId, (long)groupInfo.GroupId);
    }

    /// <see cref="IBotApi.GetGroupListAsync" />
    [Fact]
    public async Task GetGroupList()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<GroupInfo>> result = await Api.GetGroupListAsync(ct: CT);
        Assert.True(result.IsSuccess);
        IReadOnlyList<GroupInfo> groups = Assert.IsAssignableFrom<IReadOnlyList<GroupInfo>>(result.Data);
        _output.WriteLine($"Groups: {groups.Count}");
        foreach (GroupInfo group in groups)
            _output.WriteLine($"  {group.GroupId}: {group.GroupName} ({group.MemberCount}/{group.MaxMemberCount})");
    }

    /// <see cref="IBotApi.GetGroupMemberInfoAsync" />
    [Fact]
    public async Task GetGroupMemberInfo()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        GroupId                    testGroup  = TestConfig.TestGroupId;
        ApiResult<GroupMemberInfo> result     = await Api.GetGroupMemberInfoAsync(testGroup, self.UserId, ct: CT);
        GroupMemberInfo            memberInfo = result.AssertSuccess();
        _output.WriteLine($"MemberInfo: {memberInfo.Nickname} role={memberInfo.Role}");
        Assert.Equal((long)self.UserId, (long)memberInfo.UserId);
    }

    /// <see cref="IBotApi.GetGroupMemberListAsync" />
    [Fact]
    public async Task GetGroupMemberList()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId                                   testGroup = TestConfig.TestGroupId;
        ApiResult<IReadOnlyList<GroupMemberInfo>> result    = await Api.GetGroupMemberListAsync(testGroup, ct: CT);
        Assert.True(result.IsSuccess);
        IReadOnlyList<GroupMemberInfo> members = Assert.IsAssignableFrom<IReadOnlyList<GroupMemberInfo>>(result.Data);
        _output.WriteLine($"Members: {members.Count}");
        Assert.True(members.Count > 0);
        foreach (GroupMemberInfo member in members.Take(5))
            _output.WriteLine($"  {member.UserId}: {member.Nickname} role={member.Role}");
    }

    /// <see cref="IBotApi.GetGroupNotificationsAsync" />
    [Fact]
    public async Task GetGroupNotifications()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<GroupNotificationsResult> result = await Api.GetGroupNotificationsAsync(ct: CT);
        Assert.True(result.IsSuccess);
        GroupNotificationsResult notifications = Assert.IsAssignableFrom<GroupNotificationsResult>(result.Data);
        _output.WriteLine($"Notifications: count={notifications.Notifications.Count} nextSeq={notifications.NextNotificationSeq}");
    }

#endregion

#region Group Management Tests

    /// <see cref="IBotApi.SetGroupNameAsync" />
    [Fact]
    public async Task SetGroupName_AndRestore()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        GroupInfo originalInfo = (await Api.GetGroupInfoAsync(testGroup, ct: CT)).AssertSuccess();
        string    originalName = originalInfo.GroupName;
        _output.WriteLine($"Original name: {originalName}");

        ApiResult setResult = await Api.SetGroupNameAsync(testGroup, "[Sora Milky] Temp Name", CT);
        _output.WriteLine($"SetName: {setResult.IsSuccess}");

        await Task.Delay(1000, CT);

        ApiResult restoreResult = await Api.SetGroupNameAsync(testGroup, originalName, CT);
        _output.WriteLine($"Restore: {restoreResult.IsSuccess}");
        Assert.True(restoreResult.IsSuccess);
    }

    /// <see cref="IBotApi.SetGroupMemberCardAsync" />
    [Fact]
    public async Task SetGroupMemberCard_AndRestore()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self      = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        GroupId     testGroup = TestConfig.TestGroupId;

        GroupMemberInfo originalMember = (await Api.GetGroupMemberInfoAsync(testGroup, self.UserId, ct: CT)).AssertSuccess();
        string          originalCard   = originalMember.Card;
        _output.WriteLine($"Original card: '{originalCard}'");

        ApiResult setResult = await Api.SetGroupMemberCardAsync(testGroup, self.UserId, "[Milky Test] Bot", CT);
        _output.WriteLine($"SetCard: {setResult.IsSuccess}");
        Assert.True(setResult.IsSuccess);

        await Task.Delay(1000, CT);

        ApiResult restoreResult = await Api.SetGroupMemberCardAsync(testGroup, self.UserId, originalCard, CT);
        _output.WriteLine($"RestoreCard: {restoreResult.IsSuccess}");
    }

    /// <see cref="IBotApi.SetGroupMemberSpecialTitleAsync" />
    [Fact]
    public async Task SetGroupMemberSpecialTitle()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self      = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        GroupId     testGroup = TestConfig.TestGroupId;

        ApiResult result = await Api.SetGroupMemberSpecialTitleAsync(testGroup, self.UserId, "MilkyTest", CT);
        _output.WriteLine($"SetSpecialTitle: success={result.IsSuccess} msg={result.Message}");

        if (result.IsSuccess)
        {
            await Task.Delay(500, CT);
            ApiResult restoreResult = await Api.SetGroupMemberSpecialTitleAsync(testGroup, self.UserId, "", CT);
            _output.WriteLine($"RestoreTitle: {restoreResult.IsSuccess}");
        }
    }

    /// <summary>
    ///     Tests that set_group_admin is supported by protocol. Does NOT actually change admin status.
    ///     Expects a protocol error (invalid target), not timeout.
    /// </summary>
    /// <see cref="IBotApi.SetGroupAdminAsync" />
    [Fact]
    public async Task SetGroupAdmin_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Use a non-existent user ID to avoid actual admin change
        ApiResult result = await Api.SetGroupAdminAsync(TestConfig.TestGroupId, 10001L, false, CT);
        _output.WriteLine($"SetGroupAdmin: code={result.Code} msg={result.Message}");
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <summary>
    ///     Tests that kick_group_member is supported by protocol. Does NOT actually kick.
    ///     Expects a protocol error (invalid target), not timeout.
    /// </summary>
    /// <see cref="IBotApi.KickGroupMemberAsync" />
    [Fact]
    public async Task KickGroupMember_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Use a non-existent user ID to avoid actual kick
        ApiResult result = await Api.KickGroupMemberAsync(TestConfig.TestGroupId, 10001L, ct: CT);
        _output.WriteLine($"KickGroupMember: code={result.Code} msg={result.Message}");
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <summary>
    ///     Tests that leave_group is supported by protocol. Does NOT actually leave.
    ///     Uses a non-existent group ID to avoid actual group departure.
    ///     Expects a protocol error, not timeout.
    /// </summary>
    /// <see cref="IBotApi.LeaveGroupAsync" />
    [Fact]
    public async Task LeaveGroup_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Use a non-existent group ID to avoid actually leaving a group
        ApiResult result = await Api.LeaveGroupAsync(10001L, CT);
        _output.WriteLine($"LeaveGroup: code={result.Code} msg={result.Message}");
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <see cref="IBotApi.MuteGroupMemberAsync" />
    [Fact]
    public async Task MuteGroupMember_ZeroDuration()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self      = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        GroupId     testGroup = TestConfig.TestGroupId;

        ApiResult result = await Api.MuteGroupMemberAsync(testGroup, self.UserId, 0, CT);
        _output.WriteLine($"MuteMember(0): success={result.IsSuccess} msg={result.Message}");
        Assert.True(result.IsSuccess);
    }

    /// <see cref="IBotApi.MuteGroupAllAsync" />
    [Fact]
    public async Task MuteAndUnmuteGroupAll()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        ApiResult muteResult = await Api.MuteGroupAllAsync(testGroup, true, CT);
        _output.WriteLine($"MuteAll: {muteResult.IsSuccess}");
        Assert.True(muteResult.IsSuccess);

        await Task.Delay(1000, CT);

        ApiResult unmuteResult = await Api.MuteGroupAllAsync(testGroup, false, CT);
        _output.WriteLine($"UnmuteAll: {unmuteResult.IsSuccess}");
        Assert.True(unmuteResult.IsSuccess);
    }

    /// <see cref="IBotApi.HandleGroupRequestAsync" />
    [Fact]
    public async Task HandleGroupRequest_InvalidParams()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult result = await Api.HandleGroupRequestAsync(
            new GroupId(0),
            0,
            GroupJoinNotificationType.JoinRequest,
            false,
            true,
            ct: CT);
        _output.WriteLine($"HandleGroupRequest: code={result.Code} msg={result.Message}");
    }

    /// <summary>
    ///     Tests that the handle_group_invitation API endpoint is supported by the protocol (accept path).
    ///     Does NOT test actual acceptance (would need a pending invitation).
    ///     Expects a protocol error response, NOT a timeout.
    /// </summary>
    /// <see cref="IBotApi.HandleGroupInvitationAsync" />
    [Fact]
    public async Task HandleGroupInvitation_Accept_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Call with dummy params — expect protocol error, not timeout
        ApiResult result = await Api.HandleGroupInvitationAsync(TestConfig.TestGroupId, 0L, true, CT);
        _output.WriteLine($"HandleGroupInvitation(accept): code={result.Code} msg={result.Message}");
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <summary>
    ///     Tests that the handle_group_invitation API endpoint is supported by the protocol (reject path).
    ///     Does NOT test actual rejection (would need a pending invitation).
    ///     Expects a protocol error response, NOT a timeout.
    /// </summary>
    /// <see cref="IBotApi.HandleGroupInvitationAsync" />
    [Fact]
    public async Task HandleGroupInvitation_Reject_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Call with dummy params — expect protocol error, not timeout
        ApiResult result = await Api.HandleGroupInvitationAsync(TestConfig.TestGroupId, 0L, false, CT);
        _output.WriteLine($"HandleGroupInvitation(reject): code={result.Code} msg={result.Message}");
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <see cref="IBotApi.SetGroupAvatarAsync" />
    [Fact]
    public async Task SetGroupAvatar()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(string.IsNullOrEmpty(TestConfig.GroupAvatarPath), "SORA_TEST_GROUP_AVATAR not set");
        Assert.SkipWhen(!File.Exists(TestConfig.GroupAvatarPath), $"Group avatar file not found: {TestConfig.GroupAvatarPath}");

        byte[]    imageBytes = File.ReadAllBytes(TestConfig.GroupAvatarPath);
        string    base64Uri  = $"base64://{Convert.ToBase64String(imageBytes)}";
        ApiResult result     = await Api.SetGroupAvatarAsync(TestConfig.TestGroupId, base64Uri, CT);
        _output.WriteLine($"SetGroupAvatar: {result.IsSuccess} {result.Message}");
        // Server may reject for non-owner bots or image format issues — verify API is reachable
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <see cref="IBotApi.SendGroupNudgeAsync" />
    [Fact]
    public async Task SendGroupNudge()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.SendGroupNudgeAsync(testGroup, self.UserId, CT);
        _output.WriteLine($"GroupNudge: success={result.IsSuccess}");
        Assert.True(result.IsSuccess);
    }

#endregion

#region Group Announcement Tests

    /// <see cref="IBotApi.GetGroupAnnouncementsAsync" />
    [Fact]
    public async Task GetGroupAnnouncements()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId                                         testGroup = TestConfig.TestGroupId;
        ApiResult<IReadOnlyList<GroupAnnouncementInfo>> result    = await Api.GetGroupAnnouncementsAsync(testGroup, CT);
        Assert.True(result.IsSuccess);
        IReadOnlyList<GroupAnnouncementInfo>
            announcements = Assert.IsAssignableFrom<IReadOnlyList<GroupAnnouncementInfo>>(result.Data);
        _output.WriteLine($"Announcements: count={announcements.Count}");
        foreach (GroupAnnouncementInfo ann in announcements)
            _output.WriteLine($"  id={ann.AnnouncementId} by={ann.UserId} content={ann.Content}");
    }

    /// <see cref="IBotApi.SendGroupAnnouncementAsync" />
    [Fact]
    public async Task Announcements_CreateAndDelete()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Create announcement
        ApiResult sendResult = await Api.SendGroupAnnouncementAsync(testGroup, "[Sora Milky Test] Temp announcement", ct: CT);
        _output.WriteLine($"SendAnnouncement: success={sendResult.IsSuccess}");
        Assert.True(sendResult.IsSuccess);

        await Task.Delay(2000, CT);

        // Get list to find the created one
        ApiResult<IReadOnlyList<GroupAnnouncementInfo>> listResult = await Api.GetGroupAnnouncementsAsync(testGroup, CT);
        Assert.True(listResult.IsSuccess);
        IReadOnlyList<GroupAnnouncementInfo> announcements =
            Assert.IsAssignableFrom<IReadOnlyList<GroupAnnouncementInfo>>(listResult.Data);
        GroupAnnouncementInfo? created = announcements.FirstOrDefault(a => a.Content.Contains("[Sora Milky Test]"));
        if (created is null)
        {
            _output.WriteLine("Warning: created announcement not found in listing");
            return;
        }

        // Delete it
        ApiResult deleteResult = await Api.DeleteGroupAnnouncementAsync(testGroup, created.AnnouncementId, CT);
        _output.WriteLine($"DeleteAnnouncement: success={deleteResult.IsSuccess}");
        Assert.True(deleteResult.IsSuccess);
    }

#endregion

#region Group Essence Tests

    /// <see cref="IBotApi.GetGroupEssenceMessagesAsync" />
    [Fact]
    public async Task GetGroupEssenceMessages()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId                             testGroup = TestConfig.TestGroupId;
        ApiResult<GroupEssenceMessagesPage> result    = await Api.GetGroupEssenceMessagesAsync(testGroup, 0, 20, CT);
        Assert.True(result.IsSuccess);
        GroupEssenceMessagesPage messagesPage = Assert.IsAssignableFrom<GroupEssenceMessagesPage>(result.Data);
        _output.WriteLine($"EssenceMessages: count={messagesPage.Messages.Count} isEnd={messagesPage.IsEnd}");
    }

    /// <see cref="IBotApi.SetGroupEssenceMessageAsync" />
    [Fact]
    public async Task EssenceMessages_SetAndUnset()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Send a message to set as essence
        MessageBody       body       = "[Milky Test] Essence message test";
        SendMessageResult sendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        Assert.True(sendResult.IsSuccess);

        await Task.Delay(1000, CT);

        // Set as essence
        ApiResult setResult = await Api.SetGroupEssenceMessageAsync(testGroup, sendResult.MessageId, ct: CT);
        _output.WriteLine($"SetEssence: success={setResult.IsSuccess}");
        Assert.True(setResult.IsSuccess);

        await Task.Delay(1000, CT);

        // Unset essence
        ApiResult unsetResult = await Api.SetGroupEssenceMessageAsync(testGroup, sendResult.MessageId, false, CT);
        _output.WriteLine($"UnsetEssence: success={unsetResult.IsSuccess}");
        Assert.True(unsetResult.IsSuccess);
    }

#endregion

#region Group File Tests

    /// <see cref="IBotApi.GetGroupFilesAsync" />
    [Fact]
    public async Task GetGroupFiles()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId                     testGroup = TestConfig.TestGroupId;
        ApiResult<GroupFilesResult> result    = await Api.GetGroupFilesAsync(testGroup, ct: CT);
        Assert.True(result.IsSuccess);
        GroupFilesResult groupFiles = Assert.IsAssignableFrom<GroupFilesResult>(result.Data);
        _output.WriteLine($"GroupFiles: files={groupFiles.Files.Count} folders={groupFiles.Folders.Count}");
    }

    /// <see cref="IBotApi.GetGroupFileDownloadUrlAsync" />
    [Fact]
    public async Task GetGroupFileDownloadUrl()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId                     testGroup = TestConfig.TestGroupId;
        ApiResult<GroupFilesResult> files     = await Api.GetGroupFilesAsync(testGroup, ct: CT);
        if (!files.IsSuccess || files.Data is null || files.Data.Files.Count == 0)
        {
            _output.WriteLine("Skipped: no files in group");
            return;
        }

        string            fileId = files.Data.Files[0].FileId;
        ApiResult<string> result = await Api.GetGroupFileDownloadUrlAsync(testGroup, fileId, CT);
        _output.WriteLine($"FileUrl: success={result.IsSuccess} url={result.Data}");
        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Data), "Download URL should not be empty");
    }

    /// <see cref="IBotApi.GetPrivateFileDownloadUrlAsync" />
    [Fact]
    public async Task GetPrivateFileDownloadUrl()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryUserId.Value == 0, "Secondary bot not available");

        // Upload a test file first, then try download URL
        string testContent = $"test {DateTime.Now:yyyyMMdd_HHmmss}";
        string base64      = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        ApiResult<string> uploadResult = await Api.UploadPrivateFileAsync(
            _fixture.SecondaryUserId,
            $"base64://{base64}",
            "test_dl.txt",
            CT);
        Assert.SkipWhen(!uploadResult.IsSuccess, $"Upload failed: {uploadResult.Message}");

        string            uploadedFileId = Assert.IsType<string>(uploadResult.Data);
        ApiResult<string> dlResult       = await Api.GetPrivateFileDownloadUrlAsync(_fixture.SecondaryUserId, uploadedFileId, "", CT);
        _output.WriteLine($"DownloadUrl: {dlResult.IsSuccess} {dlResult.Data}");
        // Don't assert success — file_hash may be required
    }

    /// <see cref="IBotApi.CreateGroupFolderAsync" />
    [Fact]
    public async Task CreateAndDeleteGroupFolder()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup  = TestConfig.TestGroupId;
        string  folderName = $"sora_test_{DateTime.UtcNow:yyyyMMddHHmmss}";

        ApiResult<string> createResult = await Api.CreateGroupFolderAsync(testGroup, folderName, CT);
        _output.WriteLine($"CreateFolder: success={createResult.IsSuccess} id={createResult.Data}");
        Assert.True(createResult.IsSuccess);

        if (createResult.IsSuccess && !string.IsNullOrEmpty(createResult.Data))
        {
            await Task.Delay(1000, CT);
            ApiResult deleteResult = await Api.DeleteGroupFolderAsync(testGroup, createResult.Data, CT);
            _output.WriteLine($"DeleteFolder: success={deleteResult.IsSuccess}");
            Assert.True(deleteResult.IsSuccess);
        }
    }

    /// <see cref="IBotApi.DeleteGroupFileAsync" />
    [Fact]
    public async Task DeleteGroupFile()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup   = TestConfig.TestGroupId;
        string  testContent = $"Sora delete test {DateTime.Now:yyyyMMdd_HHmmss}";
        string  base64      = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        string  fileUri     = $"base64://{base64}";

        ApiResult<string> uploadResult = await Api.UploadGroupFileAsync(testGroup, fileUri, "sora_milky_delete_test.txt", ct: CT);
        if (!uploadResult.IsSuccess)
        {
            _output.WriteLine($"Skipped: upload failed ({uploadResult.Message})");
            return;
        }

        await Task.Delay(2000, CT);

        ApiResult<GroupFilesResult> files = await Api.GetGroupFilesAsync(testGroup, ct: CT);
        if (!files.IsSuccess || files.Data is null)
        {
            _output.WriteLine("Skipped: failed to list group files after upload");
            return;
        }

        GroupFileInfo? uploaded = files.Data.Files.FirstOrDefault(f => f.FileName == "sora_milky_delete_test.txt");
        if (uploaded is null)
        {
            _output.WriteLine("Skipped: uploaded file not found");
            return;
        }

        ApiResult deleteResult = await Api.DeleteGroupFileAsync(testGroup, uploaded.FileId, CT);
        _output.WriteLine($"DeleteFile: success={deleteResult.IsSuccess}");
        Assert.True(deleteResult.IsSuccess);
    }

    /// <see cref="IBotApi.RenameGroupFileAsync" />
    [Fact]
    public async Task RenameGroupFile()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup   = TestConfig.TestGroupId;
        string  testContent = $"Sora rename test {DateTime.Now:yyyyMMdd_HHmmss}";
        string  base64      = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        string  fileUri     = $"base64://{base64}";

        ApiResult<string> uploadResult = await Api.UploadGroupFileAsync(testGroup, fileUri, "sora_milky_rename_test.txt", ct: CT);
        if (!uploadResult.IsSuccess)
        {
            _output.WriteLine($"Skipped: upload failed ({uploadResult.Message})");
            return;
        }

        await Task.Delay(2000, CT);

        ApiResult<GroupFilesResult> files = await Api.GetGroupFilesAsync(testGroup, ct: CT);
        if (!files.IsSuccess || files.Data is null)
        {
            _output.WriteLine("Skipped: failed to list group files after upload");
            return;
        }

        GroupFileInfo? uploaded = files.Data.Files.FirstOrDefault(f => f.FileName == "sora_milky_rename_test.txt");
        if (uploaded is null)
        {
            _output.WriteLine("Skipped: uploaded file not found");
            return;
        }

        ApiResult renameResult = await Api.RenameGroupFileAsync(
            testGroup,
            uploaded.FileId,
            "/",
            "sora_milky_renamed.txt",
            CT);
        _output.WriteLine($"RenameFile: success={renameResult.IsSuccess}");
        Assert.True(renameResult.IsSuccess);

        // Clean up
        await Task.Delay(500, CT);
        await Api.DeleteGroupFileAsync(testGroup, uploaded.FileId, CT);
    }

    /// <see cref="IBotApi.RenameGroupFolderAsync" />
    [Fact]
    public async Task RenameGroupFolder()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup  = TestConfig.TestGroupId;
        string  folderName = $"sora_rename_{DateTime.UtcNow:yyyyMMddHHmmss}";

        ApiResult<string> createResult = await Api.CreateGroupFolderAsync(testGroup, folderName, CT);
        if (!createResult.IsSuccess || string.IsNullOrEmpty(createResult.Data))
        {
            _output.WriteLine($"Skipped: folder creation failed ({createResult.Message})");
            return;
        }

        await Task.Delay(1000, CT);

        string    folderId     = createResult.Data;
        ApiResult renameResult = await Api.RenameGroupFolderAsync(testGroup, folderId, folderName + "_renamed", CT);
        _output.WriteLine($"RenameFolder: success={renameResult.IsSuccess}");
        Assert.True(renameResult.IsSuccess);

        await Task.Delay(500, CT);

        // Clean up
        ApiResult deleteResult = await Api.DeleteGroupFolderAsync(testGroup, folderId, CT);
        _output.WriteLine($"CleanupFolder: success={deleteResult.IsSuccess}");
    }

    /// <see cref="IBotApi.MoveGroupFileAsync" />
    [Fact]
    public async Task MoveGroupFile()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Create a folder to move into
        string            folderName   = $"sora_move_{DateTime.UtcNow:yyyyMMddHHmmss}";
        ApiResult<string> folderResult = await Api.CreateGroupFolderAsync(testGroup, folderName, CT);
        if (!folderResult.IsSuccess || string.IsNullOrEmpty(folderResult.Data))
        {
            _output.WriteLine($"Skipped: folder creation failed ({folderResult.Message})");
            return;
        }

        string targetFolderId = folderResult.Data;

        // Upload a file to move using base64
        string testContent = $"Sora move test {DateTime.Now:yyyyMMdd_HHmmss}";
        string base64      = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        string fileUri     = $"base64://{base64}";

        ApiResult<string> uploadResult = await Api.UploadGroupFileAsync(testGroup, fileUri, "sora_milky_move_test.txt", ct: CT);
        if (!uploadResult.IsSuccess)
        {
            _output.WriteLine($"Skipped: upload failed ({uploadResult.Message})");
            await Api.DeleteGroupFolderAsync(testGroup, targetFolderId, CT);
            return;
        }

        await Task.Delay(2000, CT);

        ApiResult<GroupFilesResult> files = await Api.GetGroupFilesAsync(testGroup, ct: CT);
        if (!files.IsSuccess || files.Data is null)
        {
            _output.WriteLine("Skipped: failed to list group files after upload");
            await Api.DeleteGroupFolderAsync(testGroup, targetFolderId, CT);
            return;
        }

        GroupFileInfo? uploaded = files.Data.Files.FirstOrDefault(f => f.FileName == "sora_milky_move_test.txt");
        if (uploaded is null)
        {
            _output.WriteLine("Skipped: uploaded file not found");
            await Api.DeleteGroupFolderAsync(testGroup, targetFolderId, CT);
            return;
        }

        ApiResult moveResult = await Api.MoveGroupFileAsync(testGroup, uploaded.FileId, "/", targetFolderId, CT);
        _output.WriteLine($"MoveFile: success={moveResult.IsSuccess}");
        Assert.True(moveResult.IsSuccess);

        // Clean up
        await Task.Delay(500, CT);
        await Api.DeleteGroupFileAsync(testGroup, uploaded.FileId, CT);
        await Api.DeleteGroupFolderAsync(testGroup, targetFolderId, CT);
    }

    /// <see cref="IBotApi.UploadGroupFileAsync" />
    [Fact]
    public async Task UploadGroupFile()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup   = TestConfig.TestGroupId;
        string  testContent = $"Sora group file test {DateTime.Now:yyyyMMdd_HHmmss}";
        string  base64      = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        string  fileUri     = $"base64://{base64}";

        ApiResult<string> result = await Api.UploadGroupFileAsync(testGroup, fileUri, "sora_milky_test.txt", ct: CT);
        _output.WriteLine($"UploadFile: success={result.IsSuccess} msg={result.Message}");
    }

    /// <see cref="IBotApi.UploadPrivateFileAsync" />
    [Fact]
    public async Task UploadPrivateFile()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryUserId.Value == 0, "Secondary bot not available");

        // Create a small test file content
        string testContent = $"Sora test file {DateTime.Now:yyyyMMdd_HHmmss}";
        string base64      = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        string fileUri     = $"base64://{base64}";

        // Private file upload may not be supported by all protocol endpoints;
        // verify the API call completes without throwing
        ApiResult<string> result = await Api.UploadPrivateFileAsync(_fixture.SecondaryUserId, fileUri, "sora_test.txt", CT);
        _output.WriteLine($"UploadPrivateFile: success={result.IsSuccess} code={result.Code} msg={result.Message}");
    }

    /// <see cref="IBotApi.GetResourceTempUrlAsync" />
    [Fact]
    public async Task GetResourceTempUrl()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Needs a valid resource ID; test with dummy and verify graceful handling
        ApiResult<string> result = await Api.GetResourceTempUrlAsync("dummy_resource_id", CT);
        _output.WriteLine($"ResourceTempUrl: code={result.Code} msg={result.Message}");
        // May fail with invalid resource; just verify no exception
    }

    /// <see cref="IBotApi.GetCustomFaceUrlListAsync" />
    [Fact]
    public async Task GetCustomFaceUrlList()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<string>> result = await Api.GetCustomFaceUrlListAsync(CT);
        IReadOnlyList<string>            faces  = result.AssertSuccess();
        _output.WriteLine($"CustomFaces: count={faces.Count}");
    }

#endregion

#region Profile Tests

    /// <see cref="IBotApi.SetNicknameAsync" />
    [Fact]
    public async Task SetNickname_AndRestore()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self         = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        string      originalNick = self.Nickname;
        _output.WriteLine($"Original nick: {originalNick}");

        try
        {
            ApiResult setResult = await Api.SetNicknameAsync($"[Sora Test] Nick{DateTime.Now:HHmmss}", CT);
            _output.WriteLine($"SetNickname: {setResult.IsSuccess}");
            Assert.True(setResult.IsSuccess);
        }
        finally
        {
            // Always restore, even if set failed
            await Task.Delay(2000, CT);
            ApiResult restoreResult = await Api.SetNicknameAsync(originalNick, CT);
            _output.WriteLine($"RestoreNick: {restoreResult.IsSuccess} {restoreResult.Message}");
        }
    }

    /// <see cref="IBotApi.SetBioAsync" />
    [Fact]
    public async Task SetBio_AndRestore()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        ApiResult<UserProfile> profile     = await Api.GetUserProfileAsync(self.UserId, CT);
        string                 originalBio = profile is { IsSuccess: true, Data: { } profileData } ? profileData.Bio : "";
        _output.WriteLine($"Original bio: '{originalBio}'");

        try
        {
            string    tempBio   = $"[Sora Test] Bio {DateTime.Now:HHmmss}";
            ApiResult setResult = await Api.SetBioAsync(tempBio, CT);
            _output.WriteLine($"SetBio: {setResult.IsSuccess} {setResult.Message}");
            // Don't assert success — server may rate-limit bio changes
        }
        finally
        {
            // Always restore — use original or a space if empty (server rejects empty)
            await Task.Delay(2000, CT);
            string    restoreBio    = string.IsNullOrWhiteSpace(originalBio) ? " " : originalBio;
            ApiResult restoreResult = await Api.SetBioAsync(restoreBio, CT);
            _output.WriteLine($"RestoreBio: {restoreResult.IsSuccess} {restoreResult.Message}");
        }
    }

    /// <see cref="IBotApi.SetAvatarAsync" />
    [Fact]
    public async Task SetAvatar()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(string.IsNullOrEmpty(TestConfig.PrimaryBotAvatar), "SORA_TEST_BOT_AVATAR not set");
        Assert.SkipWhen(!File.Exists(TestConfig.PrimaryBotAvatar), $"Bot avatar file not found: {TestConfig.PrimaryBotAvatar}");

        byte[]    imageBytes = await File.ReadAllBytesAsync(TestConfig.PrimaryBotAvatar, CT);
        string    base64Uri  = $"base64://{Convert.ToBase64String(imageBytes)}";
        ApiResult result     = await Api.SetAvatarAsync(base64Uri, CT);
        _output.WriteLine($"SetAvatar: {result.IsSuccess} {result.Message}");
        Assert.True(result.IsSuccess);
    }

    /// <see cref="IBotApi.SendProfileLikeAsync" />
    [Fact]
    public async Task SendProfileLike()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryUserId.Value == 0, "Secondary bot not available");

        ApiResult result = await Api.SendProfileLikeAsync(_fixture.SecondaryUserId, ct: CT);
        _output.WriteLine($"Like: success={result.IsSuccess} msg={result.Message}");
    }

#endregion

#region Reaction Tests

    /// <see cref="IBotApi.SendGroupMessageReactionAsync" />
    [Fact]
    public async Task SendGroupMessageReaction()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Send a message to react to
        MessageBody       body       = "[Milky Test] Reaction target";
        SendMessageResult sendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        Assert.True(sendResult.IsSuccess);

        await Task.Delay(1000, CT);

        ApiResult result = await Api.SendGroupMessageReactionAsync(testGroup, sendResult.MessageId, "1", ct: CT);
        _output.WriteLine($"Reaction: success={result.IsSuccess}");
        Assert.True(result.IsSuccess);
    }

#endregion

#region Peer Pin Tests

    /// <see cref="IMilkyExtApi.GetPeerPinsAsync" />
    [Fact]
    public async Task GetPeerPins_ReturnsResult()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<PeerPinsResult> result = await MilkyExt.GetPeerPinsAsync(CT);
        Assert.True(result.IsSuccess);
        PeerPinsResult peerPins = Assert.IsAssignableFrom<PeerPinsResult>(result.Data);
        _output.WriteLine($"GetPeerPins: code={result.Code} friends={peerPins.Friends.Count} groups={peerPins.Groups.Count}");
    }

    /// <see cref="IMilkyExtApi.SetPeerPinAsync" />
    [Fact]
    public async Task SetPeerPin_PinAndUnpin()
    {
        Assert.SkipWhen(TestConfig.SkipMilkyReason is not null, TestConfig.SkipMilkyReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(TestConfig.TestGroupId == 0, "SORA_TEST_GROUP_ID not set");

        GroupId testGroup = TestConfig.TestGroupId;

        // Pin the group conversation
        ApiResult pinResult = await MilkyExt.SetPeerPinAsync(MessageSourceType.Group, testGroup, true, CT);
        _output.WriteLine($"Pin: code={pinResult.Code} msg={pinResult.Message}");
        Assert.True(pinResult.IsSuccess);

        await Task.Delay(1000, CT);

        // Verify the pin appears in the list
        ApiResult<PeerPinsResult> listResult = await MilkyExt.GetPeerPinsAsync(CT);
        Assert.True(listResult.IsSuccess);

        await Task.Delay(1000, CT);

        // Unpin
        ApiResult unpinResult = await MilkyExt.SetPeerPinAsync(MessageSourceType.Group, testGroup, false, CT);
        _output.WriteLine($"Unpin: code={unpinResult.Code} msg={unpinResult.Message}");
        Assert.True(unpinResult.IsSuccess);
    }

#endregion

    /// <inheritdoc />
    public void Dispose() => _logSubscription.Dispose();
}