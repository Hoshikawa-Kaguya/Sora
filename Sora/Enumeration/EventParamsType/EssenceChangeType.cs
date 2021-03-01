using System.ComponentModel;

namespace Sora.Enumeration.EventParamsType
{
    /// <summary>
    /// 精华信息变动类型
    /// </summary>
    public enum EssenceChangeType
    {
        /// <summary>
        /// 添加
        /// </summary>
        [Description("add")] Add,

        /// <summary>
        /// 删除
        /// </summary>
        [Description("delete")] Delete
    }
}