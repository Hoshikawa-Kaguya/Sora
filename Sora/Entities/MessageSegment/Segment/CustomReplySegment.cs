using System;
using Newtonsoft.Json;
using YukariToolBox.Time;

namespace Sora.Entities.MessageSegment.Segment
{
    /// <summary>
    /// 自定义回复
    /// </summary>
    public class CustomReplySegment : BaseSegment
    {
        /// <summary>
        /// 自定义回复的信息
        /// </summary>
        [JsonProperty(PropertyName = "text")]
        public string Text { get; internal set; }

        /// <summary>
        /// 自定义回复时的自定义QQ
        /// </summary>
        [JsonProperty(PropertyName = "qq")]
        public long Uid { get; internal set; }

        [JsonProperty(PropertyName = "time")] private long TimeStamp { get; set; }

        /// <summary>
        /// 自定义回复时的时间
        /// </summary>
        [JsonIgnore]
        public DateTime Time
        {
            get => TimeStamp.ToDateTime();
            init => TimeStamp = value.ToTimeStamp();
        }

        /// <summary>
        /// 起始消息序号, 可通过 <see langword="GetMessages"/> 获得
        /// </summary>
        [JsonProperty(PropertyName = "seq")]
        public long MessageSequence { get; internal set; }
    }
}