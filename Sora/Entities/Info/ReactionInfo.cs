using Newtonsoft.Json;

namespace Sora.Entities.Info;

/// <summary>
/// 表情信息
/// </summary>
public readonly struct ReactionInfo
{
    /// <summary>
    /// 表情ID
    /// </summary>
    [JsonProperty(PropertyName = "emoji_id")]
    public string EmojiId { get; internal init; }

    /// <summary>
    /// 表情对应数值ID
    /// </summary>
    [JsonProperty(PropertyName = "emoji_index")]
    public int EmojiIndex { get; internal init; }

    /// <summary>
    /// 表情类型
    /// </summary>
    [JsonProperty(PropertyName = "emoji_type")]
    public int EmojiType { get; internal init; }

    /// <summary>
    /// 表情名字
    /// </summary>
    [JsonProperty(PropertyName = "emoji_name")]
    public string EmojiName { get; internal init; }

    /// <summary>
    /// 当前表情被贴数量
    /// </summary>
    [JsonProperty(PropertyName = "count")]
    public int Count { get; internal init; }

    /// <summary>
    /// BOT是否点击
    /// </summary>
    [JsonProperty(PropertyName = "clicked")]
    public bool BotClicked { get; internal init; }
}