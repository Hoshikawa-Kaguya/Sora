using System.ComponentModel;

namespace Sora.Enumeration.ApiEnum
{
    /// <summary>
    /// 客户端类型
    /// </summary>
    [DefaultValue(Unknown)]
    public enum ClientType
    {
        /// <summary>
        /// 非onebot协议客户端
        /// </summary>
        Unknown,
        /// <summary>
        /// go-cqhttp
        /// </summary>
        GoCqhttp,
        /// <summary>
        /// 其他
        /// </summary>
        Other
    }
}
