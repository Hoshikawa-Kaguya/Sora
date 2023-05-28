using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Sora.Util;

namespace Sora.Entities.Info;

/// <summary>
/// 群公告
/// </summary>
public readonly struct GroupNoticeInfo
{
    /// <summary>
    /// 公告ID
    /// </summary>
    [JsonProperty(PropertyName = "notice_id")]
    public string NoticeId { get; init; }

    /// <summary>
    /// 发布者
    /// </summary>
    [JsonProperty(PropertyName = "sender_id")]
    public long UserId { get; init; }

    /// <summary>
    /// 发布时间
    /// </summary>
    [JsonIgnore]
    public DateTime PublishTime { get; init; }


    [JsonProperty(PropertyName = "publish_time")]
    internal long PublishTimeStamp
    {
        get => PublishTime.ToTimeStamp();
        init => PublishTime = value.ToDateTime();
    }

    /// <summary>
    /// 公告消息
    /// </summary>
    [JsonProperty(PropertyName = "message")]
    public NoticeMessage Message { get; init; }
}

/// <summary>
/// 公告消息
/// </summary>
public readonly struct NoticeMessage
{
    /// <summary>
    /// 公告文字
    /// </summary>
    [JsonProperty(PropertyName = "text")]
    public string Text { get; init; }

    /// <summary>
    /// 公告图片
    /// </summary>
    [JsonProperty(PropertyName = "images")]
    public List<NoticeImage> NoticeImages { get; init; }
}

/// <summary>
/// 公告图片
/// </summary>
public readonly struct NoticeImage
{
    /// <summary>
    /// 高
    /// </summary>
    [JsonProperty(PropertyName = "height")]
    public int? Height { get; init; }

    /// <summary>
    /// 宽
    /// </summary>
    [JsonProperty(PropertyName = "width")]
    public int? Width { get; init; }

    /// <summary>
    /// 图片ID
    /// </summary>
    [JsonProperty(PropertyName = "id")]
    public string ImageId { get; init; }
}