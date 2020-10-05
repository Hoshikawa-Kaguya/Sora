using System;
using Newtonsoft.Json;
using Sora.Enumeration;
using Sora.Enumeration.ApiEnum;

namespace Sora.EventArgs.OnebotEvent.ApiEvent
{
    /// <summary>
    /// API请求基类
    /// </summary>
    internal abstract class BaseApiMsgEventArgs
    {
        /// <summary>
        /// API请求类型
        /// 会自动生成初始值不需要设置
        /// </summary>
        [JsonProperty(PropertyName = "action")]
        [JsonConverter(typeof(EnumToDescriptionConverter))]
        internal APIType ApiType { get; set; }

        /// <summary>
        /// 请求标识符
        /// 会自动生成初始值不需要设置
        /// </summary>
        [JsonProperty(PropertyName = "echo")]
        internal Guid Echo { get; set; } = Guid.NewGuid();
    }
}
