using System.ComponentModel;

namespace Sora.Enumeration.ApiType
{
    /// <summary>
    /// <para>链接安全性</para>
    /// <para>此为腾讯接口的安全等级，不太靠谱</para>
    /// </summary>
    [DefaultValue(Unknown)]
    public enum SecurityLevelType
    {
        /// <summary>
        /// 安全
        /// </summary>
        Safe = 1,

        /// <summary>
        /// 未知
        /// </summary>
        Unknown = 2,

        /// <summary>
        /// 危险
        /// </summary>
        Danger = 3
    }
}