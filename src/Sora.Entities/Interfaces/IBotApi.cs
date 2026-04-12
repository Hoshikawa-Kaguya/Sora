namespace Sora.Entities.Interfaces;

/// <summary>
///     Cross-platform bot API surface.
///     Protocol-specific features live in <see cref="IAdapterExtension" /> implementations
///     accessible via <see cref="GetExtension{T}" />.
/// </summary>
public interface IBotApi
{
#region Extension Access

    /// <summary>
    ///     Gets a typed protocol-specific API extension.
    ///     Returns null if the adapter doesn't support the requested extension.
    /// </summary>
    /// <typeparam name="T">Extension interface type (e.g., IOneBot11Api, IMilkyApi).</typeparam>
    /// <returns>The extension instance, or null if not supported by this adapter.</returns>
    T? GetExtension<T>() where T : class, IAdapterExtension;

#endregion

#region Identity

    /// <summary>Gets the bot's own identity information.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The bot's identity (user ID and nickname).</returns>
    ValueTask<ApiResult<BotIdentity>> GetSelfInfoAsync(CancellationToken ct = default);

    /// <summary>Gets implementation/version information about the backend.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The implementation info.</returns>
    ValueTask<ApiResult<ImplInfo>> GetImplInfoAsync(CancellationToken ct = default);

    /// <summary>Gets cookies for a specified domain.</summary>
    /// <param name="domain">The domain to get cookies for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The cookie string.</returns>
    ValueTask<ApiResult<string>> GetCookiesAsync(
        string            domain,
        CancellationToken ct = default);

    /// <summary>Gets a CSRF token.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The CSRF token string.</returns>
    ValueTask<ApiResult<string>> GetCsrfTokenAsync(CancellationToken ct = default);

    /// <summary>Gets a temporary URL for a resource by its ID.</summary>
    /// <param name="resourceId">The resource ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The temporary resource URL.</returns>
    ValueTask<ApiResult<string>> GetResourceTempUrlAsync(
        string            resourceId,
        CancellationToken ct = default);

    /// <summary>Gets the list of custom face (sticker) URLs.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of custom face URLs.</returns>
    ValueTask<ApiResult<IReadOnlyList<string>>> GetCustomFaceUrlListAsync(CancellationToken ct = default);

#endregion

#region Messaging

    /// <summary>Gets a message by scene, peer ID, and message sequence.</summary>
    /// <param name="scene">The message source type (private or group).</param>
    /// <param name="peerId">The peer ID (user ID for private, group ID for group).</param>
    /// <param name="messageSeq">The message sequence number.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The message context with full content.</returns>
    ValueTask<ApiResult<MessageContext>> GetMessageAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId         messageSeq,
        CancellationToken ct = default);

    /// <summary>Gets message history for a conversation.</summary>
    /// <param name="scene">The message source type (private or group).</param>
    /// <param name="peerId">The peer ID (user ID for private, group ID for group).</param>
    /// <param name="startMessageSeq">Starting message sequence for pagination (null for latest).</param>
    /// <param name="limit">Maximum number of messages to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The history messages result with pagination info.</returns>
    ValueTask<ApiResult<HistoryMessagesResult>> GetHistoryMessagesAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId?        startMessageSeq = null,
        int               limit           = 20,
        CancellationToken ct              = default);

    /// <summary>Gets the content of a merged forward message.</summary>
    /// <param name="forwardId">The forward message ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of messages contained in the forward.</returns>
    ValueTask<ApiResult<IReadOnlyList<MessageContext>>> GetForwardMessagesAsync(
        string            forwardId,
        CancellationToken ct = default);

    /// <summary>Sends a private (direct) message.</summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="message">Message content to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The send result including the assigned message ID.</returns>
    ValueTask<SendMessageResult> SendFriendMessageAsync(
        UserId            userId,
        MessageBody       message,
        CancellationToken ct = default);

    /// <summary>Sends a group message.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="message">Message content to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The send result including the assigned message ID.</returns>
    ValueTask<SendMessageResult> SendGroupMessageAsync(
        GroupId           groupId,
        MessageBody       message,
        CancellationToken ct = default);

    /// <summary>Recalls a group message.</summary>
    /// <param name="groupId">The group ID.</param>
    /// <param name="messageId">The message ID to recall.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> RecallGroupMessageAsync(
        GroupId           groupId,
        MessageId         messageId,
        CancellationToken ct = default);

    /// <summary>Recalls a private message.</summary>
    /// <param name="userId">The user ID of the private conversation.</param>
    /// <param name="messageId">The message ID to recall.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> RecallPrivateMessageAsync(
        UserId            userId,
        MessageId         messageId,
        CancellationToken ct = default);

    /// <summary>Marks messages as read up to the specified message.</summary>
    /// <param name="scene">The message source type (private or group).</param>
    /// <param name="peerId">The peer ID (user ID for private, group ID for group).</param>
    /// <param name="messageSeq">The message sequence to mark as read up to.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> MarkMessageAsReadAsync(
        MessageSourceType scene,
        long              peerId,
        MessageId         messageSeq,
        CancellationToken ct = default);

#endregion

#region User & Friend

    /// <summary>Gets user profile information.</summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="noCache">Whether to bypass cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Basic user information.</returns>
    ValueTask<ApiResult<UserInfo>> GetUserInfoAsync(
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default);

    /// <summary>Gets detailed user profile (richer than GetUserInfoAsync).</summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Detailed user profile information.</returns>
    ValueTask<ApiResult<UserProfile>> GetUserProfileAsync(
        UserId            userId,
        CancellationToken ct = default);

    /// <summary>Gets detailed info about a specific friend.</summary>
    /// <param name="userId">Friend's user ID.</param>
    /// <param name="noCache">Whether to bypass cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Friend information.</returns>
    ValueTask<ApiResult<FriendInfo>> GetFriendInfoAsync(
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default);

    /// <summary>Gets the friend list.</summary>
    /// <param name="noCache">Whether to bypass cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of friends.</returns>
    ValueTask<ApiResult<IReadOnlyList<FriendInfo>>> GetFriendListAsync(
        bool              noCache = false,
        CancellationToken ct      = default);

    /// <summary>Gets pending friend requests.</summary>
    /// <param name="limit">Maximum number of requests to return.</param>
    /// <param name="isFiltered">Whether to include filtered (suspicious) requests.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of friend request information.</returns>
    ValueTask<ApiResult<IReadOnlyList<FriendRequestInfo>>> GetFriendRequestsAsync(
        int               limit      = 20,
        bool              isFiltered = false,
        CancellationToken ct         = default);

    /// <summary>Handles a friend add request.</summary>
    /// <param name="fromUserId">The user who sent the friend request.</param>
    /// <param name="isFiltered">Whether the request is filtered (suspicious/low-trust).</param>
    /// <param name="approve">Whether to approve the request.</param>
    /// <param name="remark">Remark/alias to set for the new friend.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> HandleFriendRequestAsync(
        UserId            fromUserId,
        bool              isFiltered,
        bool              approve,
        string            remark = "",
        CancellationToken ct     = default);

    /// <summary>Deletes a friend.</summary>
    /// <param name="userId">Friend's user ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> DeleteFriendAsync(
        UserId            userId,
        CancellationToken ct = default);

    /// <summary>Sends a nudge (poke) to a friend.</summary>
    /// <param name="userId">Target friend's user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SendFriendNudgeAsync(UserId userId, CancellationToken ct = default);

#endregion

#region Group Info

    /// <summary>Gets group information.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="noCache">Whether to bypass cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Group information.</returns>
    ValueTask<ApiResult<GroupInfo>> GetGroupInfoAsync(
        GroupId           groupId,
        bool              noCache = false,
        CancellationToken ct      = default);

    /// <summary>Gets the group list.</summary>
    /// <param name="noCache">Whether to bypass cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of groups the bot is a member of.</returns>
    ValueTask<ApiResult<IReadOnlyList<GroupInfo>>> GetGroupListAsync(
        bool              noCache = false,
        CancellationToken ct      = default);

    /// <summary>Gets a specific group member's information.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="userId">Target member's user ID.</param>
    /// <param name="noCache">Whether to bypass cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Group member information.</returns>
    ValueTask<ApiResult<GroupMemberInfo>> GetGroupMemberInfoAsync(
        GroupId           groupId,
        UserId            userId,
        bool              noCache = false,
        CancellationToken ct      = default);

    /// <summary>Gets the member list of a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="noCache">Whether to bypass cache.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of group members.</returns>
    ValueTask<ApiResult<IReadOnlyList<GroupMemberInfo>>> GetGroupMemberListAsync(
        GroupId           groupId,
        bool              noCache = false,
        CancellationToken ct      = default);

    /// <summary>Gets announcements for a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of group announcements.</returns>
    ValueTask<ApiResult<IReadOnlyList<GroupAnnouncementInfo>>> GetGroupAnnouncementsAsync(
        GroupId           groupId,
        CancellationToken ct = default);

    /// <summary>Gets essence (pinned) messages for a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="pageIndex">Zero-based page index.</param>
    /// <param name="pageSize">Number of messages per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The essence messages page with content and pagination info.</returns>
    ValueTask<ApiResult<GroupEssenceMessagesPage>> GetGroupEssenceMessagesAsync(
        GroupId           groupId,
        int               pageIndex,
        int               pageSize,
        CancellationToken ct = default);

    /// <summary>Gets group notifications (join requests, invitations, etc.).</summary>
    /// <param name="startNotificationSeq">Starting notification sequence for pagination (null for latest).</param>
    /// <param name="isFiltered">Whether to include filtered (suspicious) notifications.</param>
    /// <param name="limit">Maximum number of notifications to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The notifications result with pagination info.</returns>
    ValueTask<ApiResult<GroupNotificationsResult>> GetGroupNotificationsAsync(
        long?             startNotificationSeq = null,
        bool              isFiltered           = false,
        int               limit                = 20,
        CancellationToken ct                   = default);

#endregion

#region Group Management

    /// <summary>Handles a group join/invite request.</summary>
    /// <param name="groupId">The target group.</param>
    /// <param name="notificationSeq">The notification sequence number from the request event.</param>
    /// <param name="joinNotificationType">The type of group join notification.</param>
    /// <param name="isFiltered">Whether the request is filtered (suspicious/low-trust).</param>
    /// <param name="approve">Whether to approve the request.</param>
    /// <param name="reason">Rejection reason (empty if approving).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> HandleGroupRequestAsync(
        GroupId                   groupId,
        long                      notificationSeq,
        GroupJoinNotificationType joinNotificationType,
        bool                      isFiltered,
        bool                      approve,
        string                    reason = "",
        CancellationToken         ct     = default);

    /// <summary>Handles a group invitation (accept or reject).</summary>
    /// <param name="groupId">The group being invited to.</param>
    /// <param name="invitationSeq">The invitation sequence number from the invitation event.</param>
    /// <param name="approve">Whether to accept the invitation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> HandleGroupInvitationAsync(
        GroupId           groupId,
        long              invitationSeq,
        bool              approve,
        CancellationToken ct = default);

    /// <summary>Sets the group name.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="name">New group name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SetGroupNameAsync(
        GroupId           groupId,
        string            name,
        CancellationToken ct = default);

    /// <summary>Sets the group avatar image.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="imageUri">Avatar image URI (file://, http(s)://, or base64://).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SetGroupAvatarAsync(
        GroupId           groupId,
        string            imageUri,
        CancellationToken ct = default);

    /// <summary>Sets or removes a group member as admin.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="userId">Target member's user ID.</param>
    /// <param name="enable">True to promote to admin, false to demote.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SetGroupAdminAsync(
        GroupId           groupId,
        UserId            userId,
        bool              enable,
        CancellationToken ct = default);

    /// <summary>Sets a group member's card/remark.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="userId">Target member's user ID.</param>
    /// <param name="card">New card/remark text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SetGroupMemberCardAsync(
        GroupId           groupId,
        UserId            userId,
        string            card,
        CancellationToken ct = default);

    /// <summary>Sets a group member's special title.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="userId">Target member's user ID.</param>
    /// <param name="title">Special title text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SetGroupMemberSpecialTitleAsync(
        GroupId           groupId,
        UserId            userId,
        string            title,
        CancellationToken ct = default);

    /// <summary>Sets or unsets a group message as an essence message.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="messageSeq">The message sequence to set/unset.</param>
    /// <param name="isSet">True to set as essence, false to unset.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SetGroupEssenceMessageAsync(
        GroupId           groupId,
        MessageId         messageSeq,
        bool              isSet = true,
        CancellationToken ct    = default);

    /// <summary>Sends an announcement to a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="content">Announcement text content.</param>
    /// <param name="imageUri">Optional image URI to attach.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SendGroupAnnouncementAsync(
        GroupId           groupId,
        string            content,
        string?           imageUri = null,
        CancellationToken ct       = default);

    /// <summary>Deletes a group announcement.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="announcementId">The announcement ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> DeleteGroupAnnouncementAsync(
        GroupId           groupId,
        string            announcementId,
        CancellationToken ct = default);

    /// <summary>Kicks a member from a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="userId">Member to kick.</param>
    /// <param name="rejectFuture">Whether to reject future join requests from this user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> KickGroupMemberAsync(
        GroupId           groupId,
        UserId            userId,
        bool              rejectFuture = false,
        CancellationToken ct           = default);

    /// <summary>Mutes a group member for the specified duration.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="userId">Member to mute.</param>
    /// <param name="durationSeconds">Mute duration in seconds (0 to unmute).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> MuteGroupMemberAsync(
        GroupId           groupId,
        UserId            userId,
        int               durationSeconds,
        CancellationToken ct = default);

    /// <summary>Enables or disables group-wide mute.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="enable">True to enable, false to disable group mute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> MuteGroupAllAsync(
        GroupId           groupId,
        bool              enable,
        CancellationToken ct = default);

    /// <summary>Leaves (or dismisses) a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> LeaveGroupAsync(
        GroupId           groupId,
        CancellationToken ct = default);

    /// <summary>Sends a nudge (poke) to a group member.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="userId">Target member's user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SendGroupNudgeAsync(GroupId groupId, UserId userId, CancellationToken ct = default);

#endregion

#region File Operations

    /// <summary>Lists files and folders in a group directory.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="parentFolderId">Parent folder ID ("/" for root).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The group files result with files and folders.</returns>
    ValueTask<ApiResult<GroupFilesResult>> GetGroupFilesAsync(
        GroupId           groupId,
        string            parentFolderId = "/",
        CancellationToken ct             = default);

    /// <summary>Gets the download URL for a group file.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="fileId">The file ID to download.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The download URL string.</returns>
    ValueTask<ApiResult<string>> GetGroupFileDownloadUrlAsync(
        GroupId           groupId,
        string            fileId,
        CancellationToken ct = default);

    /// <summary>Gets the download URL for a private file.</summary>
    /// <param name="userId">The user ID of the private conversation.</param>
    /// <param name="fileId">The file ID to download.</param>
    /// <param name="fileHash">The file hash for verification.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The download URL string.</returns>
    ValueTask<ApiResult<string>> GetPrivateFileDownloadUrlAsync(
        UserId            userId,
        string            fileId,
        string            fileHash,
        CancellationToken ct = default);

    /// <summary>Creates a folder in a group file directory.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="folderName">Name of the folder to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created folder ID.</returns>
    ValueTask<ApiResult<string>> CreateGroupFolderAsync(
        GroupId           groupId,
        string            folderName,
        CancellationToken ct = default);

    /// <summary>Uploads a file to a group directory.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="fileUri">File URI (file://, http(s)://, or base64://).</param>
    /// <param name="fileName">Display file name.</param>
    /// <param name="parentFolderId">Parent folder ID ("/" for root).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The uploaded file ID.</returns>
    ValueTask<ApiResult<string>> UploadGroupFileAsync(
        GroupId           groupId,
        string            fileUri,
        string            fileName,
        string            parentFolderId = "/",
        CancellationToken ct             = default);

    /// <summary>Uploads a file to a private conversation.</summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="fileUri">File URI (file://, http(s)://, or base64://).</param>
    /// <param name="fileName">Display file name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The uploaded file ID.</returns>
    ValueTask<ApiResult<string>> UploadPrivateFileAsync(
        UserId            userId,
        string            fileUri,
        string            fileName,
        CancellationToken ct = default);

    /// <summary>Deletes a file from a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="fileId">The file ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> DeleteGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        CancellationToken ct = default);

    /// <summary>Deletes a folder from a group file directory.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="folderId">The folder ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> DeleteGroupFolderAsync(
        GroupId           groupId,
        string            folderId,
        CancellationToken ct = default);

    /// <summary>Moves a file within a group file directory.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="fileId">The file ID to move.</param>
    /// <param name="parentFolderId">Current parent folder ID.</param>
    /// <param name="targetFolderId">Target folder ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> MoveGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        string            parentFolderId,
        string            targetFolderId,
        CancellationToken ct = default);

    /// <summary>Renames a file in a group file directory.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="fileId">The file ID to rename.</param>
    /// <param name="parentFolderId">Parent folder ID.</param>
    /// <param name="newFileName">New file name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> RenameGroupFileAsync(
        GroupId           groupId,
        string            fileId,
        string            parentFolderId,
        string            newFileName,
        CancellationToken ct = default);

    /// <summary>Renames a folder in a group file directory.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="folderId">The folder ID to rename.</param>
    /// <param name="newName">New folder name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> RenameGroupFolderAsync(
        GroupId           groupId,
        string            folderId,
        string            newName,
        CancellationToken ct = default);

#endregion

#region Profile

    /// <summary>Sets the bot's avatar.</summary>
    /// <param name="uri">Avatar image URI (file://, http(s)://, or base64://).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SetAvatarAsync(
        string            uri,
        CancellationToken ct = default);

    /// <summary>Sets the bot's bio/signature.</summary>
    /// <param name="bio">New bio text.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SetBioAsync(
        string            bio,
        CancellationToken ct = default);

    /// <summary>Sets the bot's nickname.</summary>
    /// <param name="nickname">New nickname.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SetNicknameAsync(
        string            nickname,
        CancellationToken ct = default);

    /// <summary>Sends profile likes (kudos) to a user.</summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="count">Number of likes to send.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SendProfileLikeAsync(UserId userId, int count = 1, CancellationToken ct = default);

#endregion

#region Reactions

    /// <summary>Sends a message reaction (emoji) in a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="messageSeq">The message sequence to react to.</param>
    /// <param name="faceId">The face/emoji ID for the reaction.</param>
    /// <param name="isAdd">True to add the reaction, false to remove it.</param>
    /// <param name="reactionType">Reaction type: <c>"face"</c> (default) or <c>"emoji"</c>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SendGroupMessageReactionAsync(
        GroupId           groupId,
        MessageId         messageSeq,
        string            faceId,
        bool              isAdd        = true,
        string            reactionType = "face",
        CancellationToken ct           = default);

#endregion
}