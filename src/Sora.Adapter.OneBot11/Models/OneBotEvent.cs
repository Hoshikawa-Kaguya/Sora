using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sora.Adapter.OneBot11.Models;

/// <summary>OneBot v11 event base model.</summary>
internal sealed class OneBotEvent
{
    // Identity & routing
    [JsonProperty("self_id")]
    public long SelfId { get; set; }

    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("sender_id")]
    public long SenderId { get; set; }

    [JsonProperty("group_id")]
    public long GroupId { get; set; }

    [JsonProperty("target_id")]
    public long TargetId { get; set; }

    [JsonProperty("operator_id")]
    public long OperatorId { get; set; }

    [JsonProperty("invitor_id")]
    public long InvitorId { get; set; }

    [JsonProperty("source_group_id")]
    public long SourceGroupId { get; set; }

    // Type discriminators
    [JsonProperty("post_type")]
    public string? PostType { get; set; }

    [JsonProperty("message_type")]
    public string? MessageType { get; set; }

    [JsonProperty("meta_event_type")]
    public string? MetaEventType { get; set; }

    [JsonProperty("notice_type")]
    public string? NoticeType { get; set; }

    [JsonProperty("request_type")]
    public string? RequestType { get; set; }

    [JsonProperty("sub_type")]
    public string? SubType { get; set; }

    [JsonProperty("honor_type")]
    public string? HonorType { get; set; }

    [JsonProperty("scene_type")]
    public int SceneType { get; set; }

    // Message content
    [JsonProperty("message_id")]
    public int MessageId { get; set; }

    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("message")]
    public JToken? Message { get; set; }

    [JsonProperty("raw_message")]
    public string? RawMessage { get; set; }

    [JsonProperty("font")]
    public int Font { get; set; }

    // Card/title changes
    [JsonProperty("card_old")]
    public string? CardOld { get; set; }

    [JsonProperty("card_new")]
    public string? CardNew { get; set; }

    [JsonProperty("title")]
    public string? Title { get; set; }

    // Durations & counts
    [JsonProperty("duration")]
    public long Duration { get; set; }

    [JsonProperty("interval")]
    public long Interval { get; set; }

    [JsonProperty("times")]
    public int Times { get; set; }

    [JsonProperty("temp_source")]
    public int TempSource { get; set; }

    // URLs & resources
    [JsonProperty("file_set_id")]
    public string? FileSetId { get; set; }

    [JsonProperty("file_url")]
    public string? FileUrl { get; set; }

    // Request/notice fields
    [JsonProperty("flag")]
    public string? Flag { get; set; }

    [JsonProperty("comment")]
    public string? Comment { get; set; }

    [JsonProperty("is_add", NullValueHandling = NullValueHandling.Ignore)]
    public bool IsAdd { get; set; }

    // Timestamps
    [JsonProperty("time")]
    public long Time { get; set; }

    // Operator info
    [JsonProperty("operator_nick")]
    public string? OperatorNick { get; set; }

    [JsonProperty("via")]
    public string? Via { get; set; }

    // Nested objects
    [JsonProperty("file")]
    public OneBotFile? File { get; set; }

    [JsonProperty("anonymous")]
    public OneBotAnonymous? Anonymous { get; set; }

    [JsonProperty("sender")]
    public OneBotSender? Sender { get; set; }

    [JsonProperty("likes")]
    public JToken? Likes { get; set; }

    [JsonProperty("raw_info")]
    public JToken? RawInfo { get; set; }

    [JsonProperty("status")]
    public JToken? Status { get; set; }
}