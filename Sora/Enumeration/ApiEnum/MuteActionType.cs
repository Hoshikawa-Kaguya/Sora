using System.ComponentModel;

namespace Sora.Enumeration.ApiEnum
{
    /// <summary>
    /// 禁言操作类型
    /// </summary>
    public enum MuteActionType
    {
        /// <summary>
        /// 启用
        /// </summary>
        [Description("ban")]
        Enable,
        /// <summary>
        /// 解除
        /// </summary>
        [Description("lift_ban")]
        Disable
    }
}
