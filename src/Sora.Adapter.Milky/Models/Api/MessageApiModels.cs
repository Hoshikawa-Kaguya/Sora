using Newtonsoft.Json;

namespace Sora.Adapter.Milky.Models.Api;

/// <summary>Input parameters for the send_private_message API.</summary>
internal sealed class SendPrivateMessageInput
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("message")]
    public List<MilkySegment> Message { get; set; } = [];
}

/// <summary>Input parameters for the send_group_message API.</summary>
internal sealed class SendGroupMessageInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("message")]
    public List<MilkySegment> Message { get; set; } = [];
}

/// <summary>Output data from message send APIs.</summary>
internal sealed class SendMessageOutput
{
    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("time")]
    public long Time { get; set; }
}

/// <summary>Input parameters for the recall_private_message API.</summary>
internal sealed class RecallPrivateMessageInput
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }
}

/// <summary>Input parameters for the recall_group_message API.</summary>
internal sealed class RecallGroupMessageInput
{
    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }
}

/// <summary>Input parameters for the get_message API.</summary>
internal sealed class GetMessageInput
{
    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("peer_id")]
    public long PeerId { get; set; }

    [JsonProperty("message_scene")]
    public string MessageScene { get; set; } = "";
}

/// <summary>Output data from the get_message API.</summary>
internal sealed class GetMessageOutput
{
    [JsonProperty("message")]
    public MilkyMessage Message { get; set; } = new();
}

/// <summary>Input parameters for the get_history_messages API.</summary>
internal sealed class GetHistoryMessagesInput
{
    [JsonProperty("peer_id")]
    public long PeerId { get; set; }

    [JsonProperty("start_message_seq")]
    public long StartMessageSeq { get; set; }

    [JsonProperty("message_scene")]
    public string MessageScene { get; set; } = "";

    [JsonProperty("limit")]
    public int Limit { get; set; } = 20;
}

/// <summary>Output data from the get_history_messages API.</summary>
internal sealed class GetHistoryMessagesOutput
{
    [JsonProperty("next_message_seq")]
    public long NextMessageSeq { get; set; }

    [JsonProperty("messages")]
    public List<MilkyMessage> Messages { get; set; } = [];
}

/// <summary>Input parameters for the get_resource_temp_url API.</summary>
internal sealed class GetResourceTempUrlInput
{
    [JsonProperty("resource_id")]
    public string ResourceId { get; set; } = "";
}

/// <summary>Output data from the get_resource_temp_url API.</summary>
internal sealed class GetResourceTempUrlOutput
{
    [JsonProperty("url")]
    public string? Url { get; set; }
}

/// <summary>Input parameters for the get_forwarded_messages API.</summary>
internal sealed class GetForwardedMessagesInput
{
    [JsonProperty("forward_id")]
    public string ForwardId { get; set; } = "";
}

/// <summary>Milky forwarded message entity.</summary>
internal sealed class MilkyForwardedMessage
{
    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("sender_name")]
    public string? SenderName { get; set; }

    [JsonProperty("segments")]
    public List<MilkySegment> Segments { get; set; } = [];

    [JsonProperty("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonProperty("time")]
    public long Time { get; set; }
}

/// <summary>Output data from the get_forwarded_messages API.</summary>
internal sealed class GetForwardedMessagesOutput
{
    [JsonProperty("messages")]
    public List<MilkyForwardedMessage> Messages { get; set; } = [];
}

/// <summary>Input parameters for the mark_message_as_read API.</summary>
internal sealed class MarkMessageAsReadInput
{
    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("peer_id")]
    public long PeerId { get; set; }

    [JsonProperty("message_scene")]
    public string MessageScene { get; set; } = "";
}