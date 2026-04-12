using Newtonsoft.Json;

namespace Sora.Adapter.Milky.Models;

/// <summary>Milky incoming message.</summary>
internal sealed class MilkyMessage
{
    [JsonProperty("message_seq")]
    public long MessageSeq { get; set; }

    [JsonProperty("peer_id")]
    public long PeerId { get; set; }

    [JsonProperty("sender_id")]
    public long SenderId { get; set; }

    [JsonProperty("message_scene")]
    public string? MessageScene { get; set; }

    [JsonProperty("segments")]
    public List<MilkySegment> Segments { get; set; } = [];

    [JsonProperty("time")]
    public long Time { get; set; }

    [JsonProperty("friend")]
    public MilkyFriendEntity? Friend { get; set; }

    [JsonProperty("group")]
    public MilkyGroupEntity? Group { get; set; }

    [JsonProperty("group_member")]
    public MilkyGroupMemberEntity? GroupMember { get; set; }
}