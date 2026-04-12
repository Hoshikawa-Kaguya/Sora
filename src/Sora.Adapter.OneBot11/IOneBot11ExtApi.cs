using Sora.Adapter.OneBot11.Models;

namespace Sora.Adapter.OneBot11;

/// <summary>
///     OneBot v11 extension API providing protocol-specific operations not available in <see cref="IBotApi" />.
///     Access via <see cref="IBotApi.GetExtension{T}" /> where T is <see cref="IOneBot11ExtApi" />.
/// </summary>
public interface IOneBot11ExtApi : IAdapterExtension
{
#region Util

    /// <summary>Fetches the list of custom face (sticker) URLs.</summary>
    /// <param name="count">Maximum number of custom faces to return (default 48).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of custom face URLs.</returns>
    ValueTask<ApiResult<IReadOnlyList<string>>> FetchCustomFaceAsync(
        int               count = 48,
        CancellationToken ct    = default);

    /// <summary>Gets the friend list organized by categories.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of friend categories, each containing a list of friends.</returns>
    ValueTask<ApiResult<IReadOnlyList<FriendCategory>>> GetFriendsWithCategoryAsync(
        CancellationToken ct = default);

    /// <summary>Gets the list of muted members in a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The list of muted group members.</returns>
    ValueTask<ApiResult<IReadOnlyList<GroupMemberInfo>>> GetGroupShutListAsync(
        GroupId           groupId,
        CancellationToken ct = default);

    /// <summary>Performs OCR (text recognition) on an image.</summary>
    /// <param name="imageUri">Image URI or file path to recognize.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>OCR result with recognized text.</returns>
    ValueTask<ApiResult<OcrResult>> OcrImageAsync(
        string            imageUri,
        CancellationToken ct = default);

    /// <summary>Converts a voice message to text.</summary>
    /// <param name="messageId">The voice message ID to transcribe.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The transcribed text content.</returns>
    ValueTask<ApiResult<string>> VoiceMsgToTextAsync(
        MessageId         messageId,
        CancellationToken ct = default);

#endregion

#region Message Forwarding

    /// <summary>Forwards a single message to a friend.</summary>
    /// <param name="userId">Target friend's user ID.</param>
    /// <param name="messageId">Message ID to forward.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The forwarded message's new ID.</returns>
    ValueTask<ApiResult<MessageId>> ForwardFriendSingleMsgAsync(
        UserId            userId,
        MessageId         messageId,
        CancellationToken ct = default);

    /// <summary>Forwards a single message to a group.</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="messageId">Message ID to forward.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The forwarded message's new ID.</returns>
    ValueTask<ApiResult<MessageId>> ForwardGroupSingleMsgAsync(
        GroupId           groupId,
        MessageId         messageId,
        CancellationToken ct = default);

#endregion

#region Friend & Group Settings

    /// <summary>Sets a friend's category (group).</summary>
    /// <param name="userId">Target friend's user ID.</param>
    /// <param name="categoryId">Category ID to assign the friend to.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask<ApiResult> SetFriendCategoryAsync(
        UserId            userId,
        int               categoryId,
        CancellationToken ct = default);

    /// <summary>Sets a friend's remark (alias).</summary>
    /// <param name="userId">Target friend's user ID.</param>
    /// <param name="remark">New remark text (empty to clear).</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask<ApiResult> SetFriendRemarkAsync(
        UserId            userId,
        string            remark,
        CancellationToken ct = default);

    /// <summary>Sets a group's remark (alias).</summary>
    /// <param name="groupId">Target group ID.</param>
    /// <param name="remark">New remark text (empty to clear).</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask<ApiResult> SetGroupRemarkAsync(
        GroupId           groupId,
        string            remark,
        CancellationToken ct = default);

#endregion

#region Status & Actions

    /// <summary>Sets the bot's online status.</summary>
    /// <param name="status">Main status code.</param>
    /// <param name="extStatus">Extended status code.</param>
    /// <param name="batteryStatus">Battery status code.</param>
    /// <param name="ct">Cancellation token.</param>
    ValueTask<ApiResult> SetOnlineStatusAsync(
        int               status,
        int               extStatus,
        int               batteryStatus,
        CancellationToken ct = default);

    /// <summary>
    ///     Sends a nudge (poke) to a friend with an optional target user.
    ///     When <paramref name="targetId" /> is specified, the poke targets that user
    ///     through the friend identified by <paramref name="userId" />.
    /// </summary>
    /// <param name="userId">Friend's user ID (the chat context).</param>
    /// <param name="targetId">
    ///     Optional target user ID to poke. When <c>null</c>, pokes the friend themselves.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The API result indicating success or failure.</returns>
    ValueTask<ApiResult> SendFriendNudgeAsync(
        UserId            userId,
        UserId?           targetId = null,
        CancellationToken ct       = default);

#endregion
}