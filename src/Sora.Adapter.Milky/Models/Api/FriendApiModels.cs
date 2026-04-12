using Newtonsoft.Json;

namespace Sora.Adapter.Milky.Models.Api;

/// <summary>Input parameters for the send_friend_nudge API.</summary>
internal sealed class SendFriendNudgeInput
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("is_self")]
    public bool IsSelf { get; set; }
}

/// <summary>Input parameters for the send_profile_like API.</summary>
internal sealed class SendProfileLikeInput
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }

    [JsonProperty("count")]
    public int Count { get; set; } = 1;
}

/// <summary>Input parameters for the delete_friend API.</summary>
internal sealed class DeleteFriendInput
{
    [JsonProperty("user_id")]
    public long UserId { get; set; }
}

/// <summary>Input parameters for the get_friend_requests API.</summary>
internal sealed class GetFriendRequestsInput
{
    [JsonProperty("limit")]
    public int Limit { get; set; } = 20;

    [JsonProperty("is_filtered")]
    public bool IsFiltered { get; set; }
}

/// <summary>Milky friend request entity.</summary>
internal sealed class MilkyFriendRequest
{
    [JsonProperty("initiator_id")]
    public long InitiatorId { get; set; }

    [JsonProperty("initiator_uid")]
    public string? InitiatorUid { get; set; }

    [JsonProperty("target_user_id")]
    public long TargetUserId { get; set; }

    [JsonProperty("target_user_uid")]
    public string? TargetUserUid { get; set; }

    [JsonProperty("state")]
    public string? State { get; set; }

    [JsonProperty("comment")]
    public string? Comment { get; set; }

    [JsonProperty("via")]
    public string? Via { get; set; }

    [JsonProperty("time")]
    public long Time { get; set; }

    [JsonProperty("is_filtered")]
    public bool IsFiltered { get; set; }
}

/// <summary>Output data from the get_friend_requests API.</summary>
internal sealed class GetFriendRequestsOutput
{
    [JsonProperty("requests")]
    public List<MilkyFriendRequest> Requests { get; set; } = [];
}

/// <summary>Input parameters for the accept_friend_request API.</summary>
internal sealed class AcceptFriendRequestInput
{
    [JsonProperty("initiator_uid")]
    public string InitiatorUid { get; set; } = "";

    [JsonProperty("is_filtered")]
    public bool IsFiltered { get; set; }
}

/// <summary>Input parameters for the reject_friend_request API.</summary>
internal sealed class RejectFriendRequestInput
{
    [JsonProperty("initiator_uid")]
    public string InitiatorUid { get; set; } = "";

    [JsonProperty("reason")]
    public string Reason { get; set; } = "";

    [JsonProperty("is_filtered")]
    public bool IsFiltered { get; set; }
}