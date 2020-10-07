using System;
using System.ComponentModel;

namespace Sora.Enumeration
{
    /// <summary>
    /// 消息段类型
    /// </summary>
    [DefaultValue (Unknown)]
    public enum CQFunction
    {
        Unknown,
        #region 基础消息段
        /// <summary>
        /// 纯文本
        /// </summary>
        [Description("text")]
        Text,
        /// <summary>
        /// QQ 表情
        /// </summary>
        [Description("face")]
        Face,
        /// <summary>
        /// 图片
        /// </summary>
        [Description("image")]
        Image,
        /// <summary>
        /// 语音
        /// </summary>
        [Description("record")]
        Record,
        /// <summary>
        /// 短视频
        /// </summary>
        [Obsolete]
        [Description("video")]
        Video,
        /// <summary>
        /// @某人
        /// </summary>
        [Description("at")]
        At,
        /// <summary>
        /// 链接分享
        /// </summary>
        [Description("share")]
        Share,
        /// <summary>
        /// 回复
        /// </summary>
        [Description("reply")]
        Reply,
        /// <summary>
        /// <para>合并转发</para>
        /// <para>只能接收</para>
        /// </summary>
        [Description("forward")]
        Forward,
        #endregion
        #region Go扩展消息段
        /// <summary>
        /// 群戳一戳
        /// </summary>
        [Description("poke")]
        Poke,
        /// <summary>
        /// <para>合并转发节点</para>
        /// <para>也可能是自定义节点</para>
        /// </summary>
        [Description("node")]
        Node,
        /// <summary>
        /// XML 消息
        /// </summary>
        [Description("xml")]
        Xml,
        /// <summary>
        /// JSON 消息
        /// </summary>
        [Description("json")]
        Json,
        #endregion
    }
}
