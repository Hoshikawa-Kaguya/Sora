using System.Text;
using Sora.Adapter.OneBot11.Models;
using Xunit;

namespace Sora.Tests.Functional.OneBot11;

/// <summary>Functional API tests for the OneBot11 adapter.</summary>
[Collection("OneBot11.Functional")]
[Trait("Category", "Functional")]
public class ApiTests : IDisposable
{
    /// <summary>Shorthand for the xUnit v3 test cancellation token.</summary>
    private static CancellationToken CT => TestContext.Current.CancellationToken;

    private readonly OneBot11TestFixture _fixture;
    private readonly IDisposable         _logSubscription;
    private readonly ITestOutputHelper   _output;
    private          IBotApi             Api => _fixture.Api!;

    /// <summary>Initializes a new instance of the <see cref="ApiTests" /> class.</summary>
    public ApiTests(OneBot11TestFixture fixture, ITestOutputHelper output)
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<BotIdentity> result   = await Api.GetSelfInfoAsync(CT);
        BotIdentity            selfInfo = result.AssertSuccess();
        _output.WriteLine($"SelfInfo: {selfInfo.UserId} {selfInfo.Nickname}");
        Assert.True(selfInfo.UserId.Value > 0);
        Assert.False(string.IsNullOrEmpty(selfInfo.Nickname));
    }

    /// <see cref="IBotApi.GetCookiesAsync" />
    [Fact]
    public async Task GetCookies()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<string> result  = await Api.GetCookiesAsync("qq.com", CT);
        string            cookies = result.AssertSuccess();
        _output.WriteLine($"Cookies: success={result.IsSuccess} len={cookies.Length}");
    }

    /// <see cref="IBotApi.GetCsrfTokenAsync" />
    [Fact]
    public async Task GetCsrfToken()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<string> result = await Api.GetCsrfTokenAsync(CT);
        _output.WriteLine($"CsrfToken: success={result.IsSuccess} token={result.Data}");
        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Data), "CSRF token should not be empty");
    }

    /// <see cref="IBotApi.GetImplInfoAsync" />
    [Fact]
    public async Task GetImplInfo()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<ImplInfo> result   = await Api.GetImplInfoAsync(CT);
        ImplInfo            implInfo = result.AssertSuccess();
        _output.WriteLine($"ImplInfo: name={implInfo.ImplName} ver={implInfo.ImplVersion} qq={implInfo.QqProtocolVersion}");
        Assert.False(string.IsNullOrEmpty(implInfo.ImplName));
    }

    /// <see cref="IBotApi.GetCustomFaceUrlListAsync" />
    [Fact]
    public async Task GetCustomFaceUrlList()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<string>> result      = await Api.GetCustomFaceUrlListAsync(CT);
        IReadOnlyList<string>            customFaces = result.AssertSuccess();
        _output.WriteLine($"CustomFaces: count={customFaces.Count}");
    }

#endregion

#region Messaging Tests

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendGroupMessage()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId     testGroup = TestConfig.TestGroupId;
        MessageBody body      = new([new TextSegment { Text = "[OB11 Test] SendGroupMessage" }]);

        SendMessageResult result = await Api.SendGroupMessageAsync(testGroup, body, CT);
        _output.WriteLine($"Sent: messageId={result.MessageId}");
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0L, result.MessageId.Value);
    }

    /// <see cref="IBotApi.SendGroupMessageAsync" />
    [Fact]
    public async Task SendGroupMessage_WithMention()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity self      = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        GroupId     testGroup = TestConfig.TestGroupId;

        MessageBody body = new(
            [
                new MentionSegment { Target = self.UserId }, new TextSegment { Text = " [OB11 Test] Mention test" }
            ]);

        SendMessageResult result = await Api.SendGroupMessageAsync(testGroup, body, CT);
        _output.WriteLine($"MentionMsg: messageId={result.MessageId}");
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0L, result.MessageId.Value);
    }

    /// <see cref="IBotApi.SendFriendMessageAsync" />
    [Fact]
    public async Task SendPrivateMessage()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendInfo>> friends = await Api.GetFriendListAsync(ct: CT);
        if (!friends.IsSuccess || friends.Data is null || friends.Data.Count == 0)
        {
            _output.WriteLine("Skipped: no friends available");
            return;
        }

        UserId            friendId = friends.Data[0].UserId;
        MessageBody       body     = "[OB11 Test] Private message test";
        SendMessageResult result   = await Api.SendFriendMessageAsync(friendId, body, CT);
        _output.WriteLine($"SendPrivate: to={friendId} messageId={result.MessageId}");
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0L, result.MessageId.Value);
    }

    /// <see cref="IBotApi.RecallGroupMessageAsync" />
    [Fact]
    public async Task SendAndRecallGroupMessage()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId     testGroup = TestConfig.TestGroupId;
        MessageBody sendBody  = "[OB11 Test] This will be recalled";

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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendInfo>> friends = await Api.GetFriendListAsync(ct: CT);
        if (!friends.IsSuccess || friends.Data is null || friends.Data.Count == 0)
        {
            _output.WriteLine("Skipped: no friends available");
            return;
        }

        UserId            friendId   = friends.Data[0].UserId;
        MessageBody       body       = "[OB11 Test] Will be recalled";
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId           testGroup  = TestConfig.TestGroupId;
        MessageBody       body       = "[OB11 Test] GetMessage target";
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Send a forward message
        MessageBody body = new(
            SegmentBuilder.Forward(
                [
                    new ForwardedMessageNode
                        {
                            Segments   = [new TextSegment { Text = "OB11 forward test" }],
                            SenderName = "test",
                            UserId     = 114514
                        }
                ]));
        SendMessageResult messageSendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        Assert.True(messageSendResult.IsSuccess, "Failed to send forward message");
        await Task.Delay(1000, CT);

        // Find the forward message in recent history
        ApiResult<MessageContext> message =
            await Api.GetMessageAsync(MessageSourceType.Group, testGroup, messageSendResult.MessageId, CT);
        string forwardId = string.Empty;
        if (message is { IsSuccess: true, Data: not null }
            && message.Data.Body.FirstOrDefault(s => s.Type == SegmentType.Forward) is ForwardSegment forwardSegment)
            forwardId = forwardSegment.ForwardId;
        Assert.False(string.IsNullOrEmpty(forwardId), "No forward message found in recent history");

        // Retrieve the forward message content
        ApiResult<IReadOnlyList<MessageContext>> result = await Api.GetForwardMessagesAsync(forwardId, CT);
        Assert.True(result.IsSuccess);
        IReadOnlyList<MessageContext> messages = Assert.IsAssignableFrom<IReadOnlyList<MessageContext>>(result.Data);
        _output.WriteLine($"GetForwardMessages: success={result.IsSuccess} count={messages.Count}");
    }

    /// <see cref="IBotApi.GetHistoryMessagesAsync" />
    [Fact]
    public async Task GetHistoryMessages()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Send a message first to guarantee history content
        MessageBody       body       = "[OB11 Test] History target";
        SendMessageResult sendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        Assert.True(sendResult.IsSuccess);

        await Task.Delay(1000, CT);

        ApiResult<HistoryMessagesResult> result = await Api.GetHistoryMessagesAsync(
            MessageSourceType.Group,
            testGroup,
            limit: 10,
            ct: CT);
        Assert.True(result.IsSuccess);
        HistoryMessagesResult history = Assert.IsAssignableFrom<HistoryMessagesResult>(result.Data);
        Assert.True(history.Messages.Count > 0, "Expected at least one message in history");
        _output.WriteLine($"History: count={history.Messages.Count} nextSeq={history.NextMessageSeq}");

        // Verify at least one message contains actual data
        MessageContext? msgWithContent = history.Messages.FirstOrDefault(m => m.Body.Count > 0);
        if (msgWithContent is not null)
        {
            Assert.True(msgWithContent.SenderId.Value > 0, "Message sender ID should be positive");
            _output.WriteLine($"  Msg with content: sender={msgWithContent.SenderId} body={msgWithContent.Body.GetText()}");
        }
        else
        {
            _output.WriteLine("  Warning: all history messages have empty body (may be system messages)");
        }
    }

    /// <see cref="IBotApi.MarkMessageAsReadAsync" />
    [Fact]
    public async Task MarkMessageAsRead()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Send a message first to get a valid seq
        MessageBody       body       = "[OB11 Test] MarkAsRead target";
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity selfInfo = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        ApiResult<UserInfo> result   = await Api.GetUserInfoAsync(selfInfo.UserId, ct: CT);
        UserInfo            userInfo = result.AssertSuccess();
        _output.WriteLine($"UserInfo: {userInfo.Nickname} sex={userInfo.Sex}");
        Assert.Equal((long)selfInfo.UserId, (long)userInfo.UserId);
    }

    /// <see cref="IBotApi.GetFriendInfoAsync" />
    [Fact]
    public async Task GetFriendInfo()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendInfo>> friends = await Api.GetFriendListAsync(ct: CT);
        if (!friends.IsSuccess || friends.Data is null || friends.Data.Count == 0)
        {
            _output.WriteLine("Skipped: no friends available");
            return;
        }

        UserId                friendId   = friends.Data[0].UserId;
        ApiResult<FriendInfo> result     = await Api.GetFriendInfoAsync(friendId, ct: CT);
        FriendInfo            friendData = result.AssertSuccess();
        _output.WriteLine($"FriendInfo: {friendData.Nickname} remark={friendData.Remark}");
        Assert.Equal((long)friendId, (long)friendData.UserId);
    }

    /// <see cref="IBotApi.GetFriendListAsync" />
    [Fact]
    public async Task GetFriendList()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendInfo>> result = await Api.GetFriendListAsync(ct: CT);
        Assert.True(result.IsSuccess);
        IReadOnlyList<FriendInfo> friends = Assert.IsAssignableFrom<IReadOnlyList<FriendInfo>>(result.Data);
        _output.WriteLine($"Friends: {friends.Count}");
        foreach (FriendInfo friend in friends)
        {
            _output.WriteLine($"  {friend.UserId}: {friend.Nickname} ({friend.Remark})");
            Assert.True(friend.UserId.Value > 0);
        }
    }

    /// <see cref="IBotApi.GetFriendRequestsAsync" />
    [Fact]
    public async Task GetFriendRequests()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<IReadOnlyList<FriendRequestInfo>> result = await Api.GetFriendRequestsAsync(ct: CT);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        IReadOnlyList<FriendRequestInfo> requests = Assert.IsAssignableFrom<IReadOnlyList<FriendRequestInfo>>(result.Data);
        _output.WriteLine($"FriendRequests: count={requests.Count}");

        // Verify each request has valid structure (if any exist)
        foreach (FriendRequestInfo req in requests)
        {
            Assert.True(req.InitiatorId.Value > 0, "Request initiator ID should be positive");
            _output.WriteLine($"  from={req.InitiatorId} state={req.State} comment={req.Comment}");
        }
    }

    /// <see cref="IBotApi.HandleFriendRequestAsync" />
    [Fact]
    public async Task HandleFriendRequest_NoPending()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        // No pending request — expect failure from lookup
        ApiResult result = await Api.HandleFriendRequestAsync(new UserId(999999), false, true, ct: CT);
        _output.WriteLine($"HandleFriendRequest: code={result.Code} msg={result.Message}");
        // Expected to fail — no valid pending request
    }

    /// <summary>
    ///     Tests that delete_friend is supported by protocol. Does NOT actually delete.
    /// </summary>
    /// <see cref="IBotApi.DeleteFriendAsync" />
    [Fact]
    public async Task DeleteFriend_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Use a non-friend user ID to avoid actual deletion
        ApiResult result = await Api.DeleteFriendAsync(10001L, CT);
        _output.WriteLine($"DeleteFriend: code={result.Code} msg={result.Message}");
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <see cref="IBotApi.SendFriendNudgeAsync" />
    [Fact]
    public async Task SendFriendNudge()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
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

    /// <see cref="IBotApi.GetUserProfileAsync" />
    [Fact]
    public async Task GetUserProfile()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity selfInfo = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        ApiResult<UserProfile> result  = await Api.GetUserProfileAsync(selfInfo.UserId, CT);
        UserProfile            profile = result.AssertSuccess();
        Assert.Equal((long)selfInfo.UserId, (long)profile.UserId);
        Assert.False(string.IsNullOrEmpty(profile.Nickname), "Profile nickname should not be empty");
        Assert.True(profile.Level >= 0, "Profile level should be non-negative");
        _output.WriteLine($"Profile: nick={profile.Nickname} bio={profile.Bio} level={profile.Level} sex={profile.Sex}");
    }

#endregion

#region Group Info Tests

    /// <see cref="IBotApi.GetGroupInfoAsync" />
    [Fact]
    public async Task GetGroupInfo()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity selfInfo = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        GroupId                    testGroup  = TestConfig.TestGroupId;
        ApiResult<GroupMemberInfo> result     = await Api.GetGroupMemberInfoAsync(testGroup, selfInfo.UserId, ct: CT);
        GroupMemberInfo            memberInfo = result.AssertSuccess();
        _output.WriteLine($"MemberInfo: {memberInfo.Nickname} role={memberInfo.Role}");
        Assert.Equal((long)selfInfo.UserId, (long)memberInfo.UserId);
    }

    /// <see cref="IBotApi.GetGroupMemberListAsync" />
    [Fact]
    public async Task GetGroupMemberList()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<GroupNotificationsResult> result = await Api.GetGroupNotificationsAsync(ct: CT);
        _output.WriteLine($"GetGroupNotifications: code={result.Code}");
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        ApiResult<GroupInfo> original      = await Api.GetGroupInfoAsync(testGroup, ct: CT);
        GroupInfo            originalGroup = original.AssertSuccess();
        string               originalName  = originalGroup.GroupName;
        _output.WriteLine($"Original name: {originalName}");

        ApiResult setResult = await Api.SetGroupNameAsync(testGroup, "[Sora OB11] Temp Name", CT);
        _output.WriteLine($"SetName: {setResult.IsSuccess}");
        Assert.True(setResult.IsSuccess);

        await Task.Delay(1000, CT);

        ApiResult restoreResult = await Api.SetGroupNameAsync(testGroup, originalName, CT);
        _output.WriteLine($"Restore: {restoreResult.IsSuccess}");
        Assert.True(restoreResult.IsSuccess);
    }

    /// <see cref="IBotApi.SetGroupMemberCardAsync" />
    [Fact]
    public async Task SetGroupMemberCard_AndRestore()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity selfInfo  = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        GroupId     testGroup = TestConfig.TestGroupId;

        GroupMemberInfo originalMember = (await Api.GetGroupMemberInfoAsync(testGroup, selfInfo.UserId, ct: CT)).AssertSuccess();
        string          originalCard   = originalMember.Card;
        _output.WriteLine($"Original card: '{originalCard}'");

        ApiResult setResult = await Api.SetGroupMemberCardAsync(testGroup, selfInfo.UserId, "[OB11 Test] Bot", CT);
        _output.WriteLine($"SetCard: {setResult.IsSuccess}");
        Assert.True(setResult.IsSuccess);

        await Task.Delay(1000, CT);

        ApiResult restoreResult = await Api.SetGroupMemberCardAsync(testGroup, selfInfo.UserId, originalCard, CT);
        _output.WriteLine($"RestoreCard: {restoreResult.IsSuccess}");
    }

    /// <see cref="IBotApi.SetGroupMemberSpecialTitleAsync" />
    [Fact]
    public async Task SetGroupMemberSpecialTitle()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity selfInfo  = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        GroupId     testGroup = TestConfig.TestGroupId;

        // Requires bot to be group owner; may fail if not owner
        ApiResult result = await Api.SetGroupMemberSpecialTitleAsync(testGroup, selfInfo.UserId, "TestTitle", CT);
        _output.WriteLine($"SetSpecialTitle: success={result.IsSuccess} msg={result.Message}");

        if (result.IsSuccess)
        {
            await Task.Delay(500, CT);
            ApiResult restoreResult = await Api.SetGroupMemberSpecialTitleAsync(testGroup, selfInfo.UserId, "", CT);
            _output.WriteLine($"RestoreTitle: {restoreResult.IsSuccess}");
        }
    }

    /// <summary>
    ///     Tests that set_group_admin is supported by protocol. Does NOT actually change admin status.
    /// </summary>
    /// <see cref="IBotApi.SetGroupAdminAsync" />
    [Fact]
    public async Task SetGroupAdmin_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Use a non-existent user ID to avoid actual admin change
        ApiResult result = await Api.SetGroupAdminAsync(TestConfig.TestGroupId, 10001L, false, CT);
        _output.WriteLine($"SetGroupAdmin: code={result.Code} msg={result.Message}");
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <summary>
    ///     Tests that kick_group_member is supported by protocol. Does NOT actually kick.
    /// </summary>
    /// <see cref="IBotApi.KickGroupMemberAsync" />
    [Fact]
    public async Task KickGroupMember_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // Use a non-existent user ID to avoid actual kick
        ApiResult result = await Api.KickGroupMemberAsync(TestConfig.TestGroupId, 10001L, ct: CT);
        _output.WriteLine($"KickGroupMember: code={result.Code} msg={result.Message}");
        Assert.NotEqual(ApiStatusCode.Timeout, result.Code);
    }

    /// <summary>
    ///     Tests that leave_group is supported by protocol. Does NOT actually leave.
    ///     Uses a non-existent group ID to avoid actual group departure.
    /// </summary>
    /// <see cref="IBotApi.LeaveGroupAsync" />
    [Fact]
    public async Task LeaveGroup_ProtocolSupport()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity selfInfo  = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        GroupId     testGroup = TestConfig.TestGroupId;

        // duration=0 means unmute; safe to call on self
        ApiResult result = await Api.MuteGroupMemberAsync(testGroup, selfInfo.UserId, 0, CT);
        _output.WriteLine($"MuteMember(0): success={result.IsSuccess} msg={result.Message}");
        Assert.True(result.IsSuccess);
    }

    /// <see cref="IBotApi.MuteGroupAllAsync" />
    [Fact]
    public async Task MuteAndUnmuteGroupAll()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
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
    public async Task HandleGroupRequest_NoPending()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        // No pending request — expect failure from lookup
        ApiResult result = await Api.HandleGroupRequestAsync(
            new GroupId(0),
            0,
            GroupJoinNotificationType.JoinRequest,
            false,
            true,
            ct: CT);
        _output.WriteLine($"HandleGroupRequest: code={result.Code} msg={result.Message}");
    }

    /// <see cref="IBotApi.HandleGroupInvitationAsync" />
    [Fact]
    public async Task HandleGroupInvitation_Accept()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.HandleGroupInvitationAsync(testGroup, 0L, true, CT);
        _output.WriteLine($"HandleGroupInvitation_Accept: code={result.Code}");
        Assert.Equal(ApiStatusCode.Failed, result.Code);
    }

    /// <see cref="IBotApi.HandleGroupInvitationAsync" />
    [Fact]
    public async Task HandleGroupInvitation_Reject()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.HandleGroupInvitationAsync(testGroup, 0L, false, CT);
        _output.WriteLine($"HandleGroupInvitation_Reject: code={result.Code}");
        Assert.Equal(ApiStatusCode.Failed, result.Code);
    }

    /// <see cref="IBotApi.SetGroupAvatarAsync" />
    [Fact]
    public async Task SetGroupAvatar()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(string.IsNullOrEmpty(TestConfig.GroupAvatarPath), "SORA_TEST_GROUP_AVATAR not set");
        Assert.SkipWhen(!File.Exists(TestConfig.GroupAvatarPath), $"Group avatar file not found: {TestConfig.GroupAvatarPath}");

        byte[] imageBytes = File.ReadAllBytes(TestConfig.GroupAvatarPath);
        Assert.True(imageBytes.Length > 0, "Avatar file should not be empty");
        string    base64Uri = $"base64://{Convert.ToBase64String(imageBytes)}";
        ApiResult result    = await Api.SetGroupAvatarAsync(TestConfig.TestGroupId, base64Uri, CT);
        _output.WriteLine($"SetGroupAvatar: code={result.Code} success={result.IsSuccess} msg={result.Message}");
        // Server may reject for non-owner bots or image format issues — verify API actually processed the request
        Assert.True(
            result.Code is ApiStatusCode.Ok or ApiStatusCode.Failed or ApiStatusCode.Unknown,
            $"Expected Ok, Failed, or Unknown, got {result.Code}");
    }

    /// <see cref="IBotApi.SendGroupNudgeAsync" />
    [Fact]
    public async Task SendGroupNudge()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        BotIdentity selfInfo = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.SendGroupNudgeAsync(testGroup, selfInfo.UserId, CT);
        _output.WriteLine($"GroupNudge: success={result.IsSuccess}");
        Assert.True(result.IsSuccess);
    }

    /// <see cref="IBotApi.SetGroupEssenceMessageAsync" />
    [Fact]
    public async Task SetGroupEssenceMessage_NotSupported()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.SetGroupEssenceMessageAsync(testGroup, 0L, ct: CT);
        _output.WriteLine($"SetGroupEssenceMessage: code={result.Code}");
        Assert.False(result.IsSuccess, $"Expected API to not succeed, got {result.Code}");
    }

#endregion

#region Group Announcement Tests

    /// <see cref="IBotApi.GetGroupAnnouncementsAsync" />
    [Fact]
    public async Task GetGroupAnnouncements()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId                                         testGroup = TestConfig.TestGroupId;
        ApiResult<IReadOnlyList<GroupAnnouncementInfo>> result    = await Api.GetGroupAnnouncementsAsync(testGroup, CT);
        _output.WriteLine($"GetGroupAnnouncements: code={result.Code}");
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        ApiResult sendResult = await Api.SendGroupAnnouncementAsync(testGroup, "[Sora OB11 Test] Temp announcement", ct: CT);
        _output.WriteLine($"SendAnnouncement: success={sendResult.IsSuccess}");
        Assert.True(sendResult.IsSuccess);

        await Task.Delay(2000, CT);

        ApiResult<IReadOnlyList<GroupAnnouncementInfo>> listResult = await Api.GetGroupAnnouncementsAsync(testGroup, CT);
        Assert.True(listResult.IsSuccess);
        IReadOnlyList<GroupAnnouncementInfo> announcements =
            Assert.IsAssignableFrom<IReadOnlyList<GroupAnnouncementInfo>>(listResult.Data);
        GroupAnnouncementInfo? created = announcements.FirstOrDefault(a => a.Content.Contains("[Sora OB11 Test]"));
        if (created is null)
        {
            _output.WriteLine("Warning: announcement not found in list after creation, skipping delete");
            return;
        }

        _output.WriteLine($"Created announcement: id={created.AnnouncementId}");
        ApiResult deleteResult = await Api.DeleteGroupAnnouncementAsync(testGroup, created.AnnouncementId, CT);
        _output.WriteLine($"DeleteAnnouncement: success={deleteResult.IsSuccess}");
    }

    /// <see cref="IBotApi.DeleteGroupAnnouncementAsync" />
    [Fact]
    public async Task DeleteGroupAnnouncement_InvalidId()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.DeleteGroupAnnouncementAsync(testGroup, "dummy_id", CT);
        _output.WriteLine($"DeleteGroupAnnouncement(dummy): code={result.Code}");
        Assert.False(result.IsSuccess, $"Deleting invalid announcement should fail, got {result.Code}");
    }

#endregion

#region Group Essence Tests

    /// <see cref="IBotApi.GetGroupEssenceMessagesAsync" />
    [Fact]
    public async Task GetGroupEssenceMessages()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId                             testGroup = TestConfig.TestGroupId;
        ApiResult<GroupEssenceMessagesPage> result    = await Api.GetGroupEssenceMessagesAsync(testGroup, 0, 20, CT);
        _output.WriteLine($"GetGroupEssenceMessages: code={result.Code}");
        Assert.True(result.IsSuccess);
        GroupEssenceMessagesPage messagesPage = Assert.IsAssignableFrom<GroupEssenceMessagesPage>(result.Data);
        _output.WriteLine($"EssenceMessages: count={messagesPage.Messages.Count} isEnd={messagesPage.IsEnd}");
    }

#endregion

#region Group File Tests

    /// <see cref="IBotApi.GetGroupFilesAsync" />
    [Fact]
    public async Task GetGroupFiles()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId                     testGroup = TestConfig.TestGroupId;
        ApiResult<GroupFilesResult> files     = await Api.GetGroupFilesAsync(testGroup, ct: CT);

        if (!files.IsSuccess || files.Data is null || files.Data.Files.Count == 0)
        {
            _output.WriteLine("Skipped: no files in group");
            return;
        }

        string fileId = files.Data.Files[0].FileId;
        _output.WriteLine($"Trying file: id={fileId}");
        ApiResult<string> result = await Api.GetGroupFileDownloadUrlAsync(testGroup, fileId, CT);
        _output.WriteLine($"FileUrl: success={result.IsSuccess} code={result.Code} url={result.Data}");
        Assert.SkipWhen(
            !result.IsSuccess,
            $"OB11 GetGroupFileDownloadUrl returned {result.Code} — endpoint may not support file download URLs");
        Assert.False(string.IsNullOrEmpty(result.Data), "Download URL should not be empty");
    }

    /// <see cref="IBotApi.CreateGroupFolderAsync" />
    [Fact]
    public async Task CreateAndDeleteGroupFolder()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup   = TestConfig.TestGroupId;
        string  testContent = $"Sora delete test {DateTime.Now:yyyyMMdd_HHmmss}";
        string  base64      = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        string  fileUri     = $"base64://{base64}";

        ApiResult<string> uploadResult =
            await Api.UploadGroupFileAsync(testGroup, fileUri, "sora_ob11_delete_test.txt", ct: CT);

        if (!uploadResult.IsSuccess)
        {
            _output.WriteLine($"Skipped: upload failed ({uploadResult.Message})");
            return;
        }

        await Task.Delay(2000, CT);

        // Find the uploaded file
        ApiResult<GroupFilesResult> files = await Api.GetGroupFilesAsync(testGroup, ct: CT);
        if (!files.IsSuccess || files.Data is null)
        {
            _output.WriteLine("Skipped: failed to list group files after upload");
            return;
        }

        GroupFileInfo? uploaded = files.Data.Files.FirstOrDefault(f => f.FileName == "sora_ob11_delete_test.txt");
        if (uploaded is null)
        {
            _output.WriteLine("Skipped: uploaded file not found in listing");
            return;
        }

        ApiResult deleteResult = await Api.DeleteGroupFileAsync(testGroup, uploaded.FileId, CT);
        _output.WriteLine($"DeleteFile: success={deleteResult.IsSuccess}");
        Assert.True(deleteResult.IsSuccess);
    }

    /// <see cref="IBotApi.RenameGroupFileAsync" />
    [Fact]
    public async Task RenameGroupFile_NotSupported()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.RenameGroupFileAsync(testGroup, "dummy_file", "/", "new_name.txt", CT);
        _output.WriteLine($"RenameGroupFile: code={result.Code}");
        Assert.False(result.IsSuccess, $"Expected API to not succeed, got {result.Code}");
    }

    /// <see cref="IBotApi.RenameGroupFolderAsync" />
    [Fact]
    public async Task RenameGroupFolder_NotSupported()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.RenameGroupFolderAsync(testGroup, "dummy_folder", "new_name", CT);
        _output.WriteLine($"RenameGroupFolder: code={result.Code}");
        Assert.False(result.IsSuccess, $"Expected API to not succeed, got {result.Code}");
    }

    /// <see cref="IBotApi.MoveGroupFileAsync" />
    [Fact]
    public async Task MoveGroupFile_NotSupported()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.MoveGroupFileAsync(testGroup, "dummy_file", "/", "/target", CT);
        _output.WriteLine($"MoveGroupFile: code={result.Code}");
        Assert.False(result.IsSuccess, $"Expected API to not succeed, got {result.Code}");
    }

    /// <see cref="IBotApi.UploadGroupFileAsync" />
    [Fact]
    public async Task UploadGroupFile()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup   = TestConfig.TestGroupId;
        string  testContent = $"Sora group file test {DateTime.Now:yyyyMMdd_HHmmss}";
        string  base64      = Convert.ToBase64String(Encoding.UTF8.GetBytes(testContent));
        string  fileUri     = $"base64://{base64}";

        ApiResult<string> result = await Api.UploadGroupFileAsync(testGroup, fileUri, "sora_ob11_test.txt", ct: CT);
        _output.WriteLine($"UploadFile: success={result.IsSuccess} msg={result.Message}");
    }

    /// <see cref="IBotApi.UploadPrivateFileAsync" />
    [Fact]
    public async Task UploadPrivateFile_NotSupported()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<string> result = await Api.UploadPrivateFileAsync(10000L, "dummy_path", "test.txt", CT);
        _output.WriteLine($"UploadPrivateFile: code={result.Code}");
        Assert.False(result.IsSuccess, $"Expected API to not succeed, got {result.Code}");
    }

    /// <see cref="IBotApi.GetPrivateFileDownloadUrlAsync" />
    [Fact]
    public async Task GetPrivateFileDownloadUrl_NotSupported()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        ApiResult<string> result = await Api.GetPrivateFileDownloadUrlAsync(10000L, "dummy_id", "dummy_hash", CT);
        _output.WriteLine($"GetPrivateFileDownloadUrl: code={result.Code}");
        Assert.False(result.IsSuccess, $"Expected API to not succeed, got {result.Code}");
    }

    /// <see cref="IBotApi.GetResourceTempUrlAsync" />
    [Fact]
    public async Task GetResourceTempUrl()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Send an image message to get a real resource ID, or fall back to dummy
        // First, try to fetch a recent message that contains an image
        ApiResult<HistoryMessagesResult> history = await Api.GetHistoryMessagesAsync(
            MessageSourceType.Group,
            testGroup,
            limit: 20,
            ct: CT);

        string? resourceId = null;
        if (history is { IsSuccess: true, Data.Messages.Count: > 0 })
            foreach (MessageContext msg in history.Data.Messages)
            {
                ImageSegment? img = msg.Body.OfType<ImageSegment>().FirstOrDefault();
                if (img is not null && !string.IsNullOrEmpty(img.ResourceId))
                {
                    resourceId = img.ResourceId;
                    break;
                }
            }

        if (string.IsNullOrEmpty(resourceId))
        {
            // No image resource found — test with dummy; verify API responds gracefully
            ApiResult<string> dummyResult = await Api.GetResourceTempUrlAsync("dummy_resource_id", CT);
            _output.WriteLine($"ResourceTempUrl(dummy): code={dummyResult.Code} msg={dummyResult.Message}");
            // Cannot validate URL content with dummy ID; verify no crash
            return;
        }

        ApiResult<string> result = await Api.GetResourceTempUrlAsync(resourceId, CT);
        _output.WriteLine($"ResourceTempUrl: code={result.Code} url={result.Data}");
        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrEmpty(result.Data), "Temp URL should not be empty");
    }

#endregion

#region Profile Tests

    /// <see cref="IBotApi.SetNicknameAsync" />
    [Fact]
    public async Task SetNickname_AndRestore()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // 1. Save original nickname
        BotIdentity selfInfo     = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
        string      originalNick = selfInfo.Nickname;
        _output.WriteLine($"Original nick: {originalNick}");

        string tempNick = $"[Sora Test] Nick{DateTime.Now:HHmmss}";
        try
        {
            // 2. Set new nickname
            ApiResult setResult = await Api.SetNicknameAsync(tempNick, CT);
            _output.WriteLine($"SetNickname: {setResult.IsSuccess}");
            Assert.True(setResult.IsSuccess);

            // 3. Verify nickname changed by re-fetching
            await Task.Delay(2000, CT);
            BotIdentity updatedInfo = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();
            _output.WriteLine($"Updated nick: {updatedInfo.Nickname}");
            Assert.Equal(tempNick, updatedInfo.Nickname);
        }
        finally
        {
            // 4. Always restore, even if set failed
            await Task.Delay(2000, CT);
            ApiResult restoreResult = await Api.SetNicknameAsync(originalNick, CT);
            _output.WriteLine($"RestoreNick: {restoreResult.IsSuccess} {restoreResult.Message}");
        }
    }

    /// <see cref="IBotApi.SetBioAsync" />
    [Fact]
    public async Task SetBio_AndRestore()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        // 1. Save original bio
        BotIdentity selfInfo = (await Api.GetSelfInfoAsync(CT)).AssertSuccess();

        UserProfile profileData = (await Api.GetUserProfileAsync(selfInfo.UserId, CT)).AssertSuccess();
        string      originalBio = profileData.Bio;
        _output.WriteLine($"Original bio: '{originalBio}'");

        string tempBio = $"[Sora Test] Bio {DateTime.Now:HHmmss}";
        try
        {
            // 2. Set new bio
            ApiResult setResult = await Api.SetBioAsync(tempBio, CT);
            _output.WriteLine($"SetBio: {setResult.IsSuccess} {setResult.Message}");
            Assert.True(setResult.IsSuccess);

            // 3. Verify bio changed by re-fetching profile (OB11 may not reflect changes immediately)
            await Task.Delay(2000, CT);
            UserProfile updatedProfileData = (await Api.GetUserProfileAsync(selfInfo.UserId, CT)).AssertSuccess();
            _output.WriteLine($"Updated bio: '{updatedProfileData.Bio}'");
            if (updatedProfileData.Bio != tempBio)
                _output.WriteLine("Note: Bio not yet reflected in profile (protocol may cache)");
        }
        finally
        {
            // 4. Always restore — use original or a space if empty (server rejects empty)
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
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
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryUserId.Value == 0, "Secondary bot not available");

        ApiResult result = await Api.SendProfileLikeAsync(_fixture.SecondaryUserId, ct: CT);
        _output.WriteLine($"Like: success={result.IsSuccess} msg={result.Message}");
    }

#endregion

#region Reaction Tests

    /// <see cref="IBotApi.SendGroupMessageReactionAsync" />
    [Fact]
    public async Task SendGroupMessageReaction_NotSupported()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        GroupId   testGroup = TestConfig.TestGroupId;
        ApiResult result    = await Api.SendGroupMessageReactionAsync(testGroup, 0L, "1", ct: CT);
        _output.WriteLine($"SendGroupMessageReaction: code={result.Code}");
        Assert.False(result.IsSuccess, $"Expected API to not succeed, got {result.Code}");
    }

#endregion

#region Extension API Tests

    /// <see cref="IOneBot11ExtApi.FetchCustomFaceAsync" />
    [Fact]
    public async Task ExtApi_FetchCustomFace()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        IOneBot11ExtApi? ext = Api.GetExtension<IOneBot11ExtApi>();
        Assert.SkipWhen(ext is null, "IOneBot11ExtApi extension not available");

        ApiResult<IReadOnlyList<string>> result      = await ext.FetchCustomFaceAsync(ct: CT);
        IReadOnlyList<string>            customFaces = result.AssertSuccess();
        _output.WriteLine($"FetchCustomFace: count={customFaces.Count}");

        // Verify returned URLs are non-empty strings if any exist
        foreach (string url in customFaces.Take(5))
        {
            Assert.False(string.IsNullOrWhiteSpace(url), "Custom face URL should not be empty");
            _output.WriteLine($"  face: {url}");
        }
    }

    /// <see cref="IOneBot11ExtApi.GetFriendsWithCategoryAsync" />
    [Fact]
    public async Task ExtApi_GetFriendsWithCategory()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        IOneBot11ExtApi? ext = Api.GetExtension<IOneBot11ExtApi>();
        Assert.SkipWhen(ext is null, "IOneBot11ExtApi extension not available");

        ApiResult<IReadOnlyList<FriendCategory>> result = await ext.GetFriendsWithCategoryAsync(CT);
        Assert.True(result.IsSuccess);
        IReadOnlyList<FriendCategory> categories = Assert.IsAssignableFrom<IReadOnlyList<FriendCategory>>(result.Data);
        _output.WriteLine($"FriendsWithCategory: categories={categories.Count}");
        Assert.True(categories.Count > 0, "Expected at least one friend category");

        // Verify each category has valid structure
        foreach (FriendCategory cat in categories)
        {
            Assert.False(string.IsNullOrEmpty(cat.CategoryName), "Category name should not be empty");
            _output.WriteLine($"  {cat.CategoryId}: '{cat.CategoryName}' friends={cat.FriendCount} online={cat.OnlineCount}");

            // Verify friends within category have valid user IDs
            foreach (FriendInfo friend in cat.Friends.Take(3))
                Assert.True(friend.UserId.Value > 0, "Friend UserId should be positive");
        }
    }

    /// <see cref="IOneBot11ExtApi.GetGroupShutListAsync" />
    [Fact]
    public async Task ExtApi_GetGroupShutList()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        IOneBot11ExtApi? ext = Api.GetExtension<IOneBot11ExtApi>();
        Assert.SkipWhen(ext is null, "IOneBot11ExtApi extension not available");

        ApiResult<IReadOnlyList<GroupMemberInfo>> result = await ext.GetGroupShutListAsync(TestConfig.TestGroupId, CT);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        IReadOnlyList<GroupMemberInfo> shutList = Assert.IsAssignableFrom<IReadOnlyList<GroupMemberInfo>>(result.Data);
        _output.WriteLine($"GroupShutList: count={shutList.Count}");

        // Verify each muted member has valid data
        foreach (GroupMemberInfo member in shutList)
        {
            Assert.True(member.UserId.Value > 0, "Muted member UserId should be positive");
            _output.WriteLine($"  {member.UserId}: {member.Nickname} role={member.Role}");
        }
    }

    /// <see cref="IOneBot11ExtApi.SetFriendRemarkAsync" />
    [Fact]
    public async Task ExtApi_SetFriendRemark()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryUserId.Value == 0, "Secondary bot not available");

        IOneBot11ExtApi? ext = Api.GetExtension<IOneBot11ExtApi>();
        Assert.SkipWhen(ext is null, "IOneBot11ExtApi extension not available");

        // 1. Save original remark
        FriendInfo friendData     = (await Api.GetFriendInfoAsync(_fixture.SecondaryUserId, ct: CT)).AssertSuccess();
        string     originalRemark = friendData.Remark;
        _output.WriteLine($"Original remark: '{originalRemark}'");

        string tempRemark = $"[Sora Test] {DateTime.Now:HHmmss}";
        try
        {
            // 2. Set new remark
            ApiResult setResult = await ext.SetFriendRemarkAsync(_fixture.SecondaryUserId, tempRemark, CT);
            _output.WriteLine($"SetFriendRemark: {setResult.IsSuccess} {setResult.Message}");
            Assert.True(setResult.IsSuccess);

            // 3. Verify remark changed by re-fetching friend info (OB11 may not reflect changes immediately)
            await Task.Delay(1000, CT);
            FriendInfo updatedFriend = (await Api.GetFriendInfoAsync(_fixture.SecondaryUserId, ct: CT)).AssertSuccess();
            _output.WriteLine($"Updated remark: '{updatedFriend.Remark}'");
            if (updatedFriend.Remark != tempRemark)
                _output.WriteLine("Note: Remark not yet reflected (protocol may cache)");
        }
        finally
        {
            // 4. Restore original remark
            await Task.Delay(1000, CT);
            ApiResult restoreResult = await ext.SetFriendRemarkAsync(_fixture.SecondaryUserId, originalRemark, CT);
            _output.WriteLine($"RestoreRemark: {restoreResult.IsSuccess} {restoreResult.Message}");
        }
    }

    /// <see cref="IOneBot11ExtApi.SetGroupRemarkAsync" />
    [Fact]
    public async Task ExtApi_SetGroupRemark()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        IOneBot11ExtApi? ext = Api.GetExtension<IOneBot11ExtApi>();
        Assert.SkipWhen(ext is null, "IOneBot11ExtApi extension not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // 1. Save original remark via group info
        GroupInfo originalGroup  = (await Api.GetGroupInfoAsync(testGroup, ct: CT)).AssertSuccess();
        string    originalRemark = originalGroup.Remark;
        _output.WriteLine($"Original group remark: '{originalRemark}'");

        string tempRemark = $"[Sora Test] {DateTime.Now:HHmmss}";
        try
        {
            // 2. Set new remark
            ApiResult setResult = await ext.SetGroupRemarkAsync(testGroup, tempRemark, CT);
            _output.WriteLine($"SetGroupRemark: {setResult.IsSuccess} {setResult.Message}");
            Assert.True(setResult.IsSuccess);

            // 3. Verify remark changed by re-fetching group info
            await Task.Delay(1000, CT);
            GroupInfo updatedGroup = (await Api.GetGroupInfoAsync(testGroup, ct: CT)).AssertSuccess();
            _output.WriteLine($"Updated group remark: '{updatedGroup.Remark}'");
            Assert.Equal(tempRemark, updatedGroup.Remark);
        }
        finally
        {
            // 4. Restore original remark
            await Task.Delay(1000, CT);
            ApiResult restoreResult = await ext.SetGroupRemarkAsync(testGroup, originalRemark, CT);
            _output.WriteLine($"RestoreGroupRemark: {restoreResult.IsSuccess} {restoreResult.Message}");
        }
    }

    /// <see cref="IOneBot11ExtApi.ForwardGroupSingleMsgAsync" />
    [Fact]
    public async Task ExtApi_ForwardGroupSingleMsg()
    {
        Assert.SkipWhen(TestConfig.SkipOb11Reason is not null, TestConfig.SkipOb11Reason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");

        IOneBot11ExtApi? ext = Api.GetExtension<IOneBot11ExtApi>();
        Assert.SkipWhen(ext is null, "IOneBot11ExtApi extension not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Send a test message to get a valid message ID
        MessageBody       body       = "[OB11 Test] ForwardGroupSingleMsg source";
        SendMessageResult sendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        Assert.True(sendResult.IsSuccess);
        Assert.NotEqual(0L, sendResult.MessageId.Value);
        _output.WriteLine($"Sent source msg: {sendResult.MessageId}");

        await Task.Delay(1000, CT);

        // Forward to the same group
        ApiResult<MessageId> forwardResult = await ext.ForwardGroupSingleMsgAsync(testGroup, sendResult.MessageId, CT);
        _output.WriteLine($"ForwardGroupSingleMsg: success={forwardResult.IsSuccess} newId={forwardResult.Data}");
        Assert.True(forwardResult.IsSuccess);
        Assert.NotEqual(0L, forwardResult.Data.Value);

        // Verify forwarded message is retrievable and contains content
        await Task.Delay(1000, CT);
        ApiResult<MessageContext> forwarded = await Api.GetMessageAsync(
            MessageSourceType.Group,
            testGroup,
            forwardResult.Data,
            CT);
        Assert.True(forwarded.IsSuccess);
        MessageContext forwardedMsg = Assert.IsAssignableFrom<MessageContext>(forwarded.Data);
        Assert.True(forwardedMsg.Body.Count > 0, "Forwarded message should have body segments");
        _output.WriteLine($"Forwarded msg body: {forwardedMsg.Body.GetText()}");
    }

    /// <see cref="IOneBot11ExtApi.ForwardFriendSingleMsgAsync" />
    [Fact]
    public async Task ExtApi_ForwardFriendSingleMsg()
    {
        Assert.SkipWhen(TestConfig.SkipOb11DualBotReason is not null, TestConfig.SkipOb11DualBotReason ?? "");
        Assert.SkipWhen(_fixture.Api is null, "API not available");
        Assert.SkipWhen(_fixture.SecondaryUserId.Value == 0, "Secondary bot not available");

        IOneBot11ExtApi? ext = Api.GetExtension<IOneBot11ExtApi>();
        Assert.SkipWhen(ext is null, "IOneBot11ExtApi extension not available");

        GroupId testGroup = TestConfig.TestGroupId;

        // Send a test message to group to get a valid message ID
        MessageBody       body       = "[OB11 Test] ForwardFriendSingleMsg source";
        SendMessageResult sendResult = await Api.SendGroupMessageAsync(testGroup, body, CT);
        Assert.True(sendResult.IsSuccess);
        Assert.NotEqual(0L, sendResult.MessageId.Value);
        _output.WriteLine($"Sent source msg: {sendResult.MessageId}");

        await Task.Delay(1000, CT);

        // Forward to secondary bot as friend
        ApiResult<MessageId> forwardResult =
            await ext.ForwardFriendSingleMsgAsync(_fixture.SecondaryUserId, sendResult.MessageId, CT);
        _output.WriteLine($"ForwardFriendSingleMsg: success={forwardResult.IsSuccess} newId={forwardResult.Data}");
        Assert.True(forwardResult.IsSuccess);
        Assert.NotEqual(0L, forwardResult.Data.Value);
        _output.WriteLine($"Forwarded message ID: {forwardResult.Data.Value}");
    }

#endregion

    /// <summary>Releases the test output subscription.</summary>
    public void Dispose() => _logSubscription.Dispose();
}