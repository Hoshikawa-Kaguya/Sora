using System;
using Newtonsoft.Json;

namespace Sora.Module.ApiMessageModel
{
    /// <summary>
    /// 私聊消息发送者
    /// </summary>
    internal sealed class PrivateSender
    {
        /// <summary>
        /// 发送者 QQ 号
        /// </summary>
        [JsonProperty(PropertyName = "user_id")]
        internal long UserId { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        [JsonProperty(PropertyName = "nickname")]
        internal string Nick { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        [JsonProperty(PropertyName = "sex")]
        internal string Sex { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        [JsonProperty(PropertyName = "age")]
        internal int Age { get; set; }
    }
}
