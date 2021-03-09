using Newtonsoft.Json;

namespace Sora.Entities.Info
{
    /// <summary>
    /// 私聊消息发送者
    /// </summary>
    public struct PrivateSenderInfo
    {
        /// <summary>
        /// 发送者 QQ 号
        /// </summary>
        [JsonProperty(PropertyName = "user_id")]
        public long UserId { get; internal init; }

        /// <summary>
        /// 昵称
        /// </summary>
        [JsonProperty(PropertyName = "nickname")]
        public string Nick { get; internal init; }

        /// <summary>
        /// 性别
        /// </summary>
        [JsonProperty(PropertyName = "sex")]
        public string Sex { get; internal init; }

        /// <summary>
        /// 年龄
        /// </summary>
        [JsonProperty(PropertyName = "age")]
        public int Age { get; internal init; }

        /// <summary>
        /// 来源群号
        /// </summary>
        [JsonProperty(PropertyName = "group_id")]
        public long? GroupId { get; internal init; }
    }
}