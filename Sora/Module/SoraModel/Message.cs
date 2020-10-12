using System;
using System.Collections.Generic;
using Sora.Module.CQCodes;
using Sora.Module.SoraModel.Base;

namespace Sora.Module.SoraModel
{
    public sealed class Message : BaseModel
    {
        #region 属性
        /// <summary>
        /// 消息ID
        /// </summary>
        public int MessageId { get; private set; }

        /// <summary>
        /// 纯文本信息
        /// </summary>
        public string RawText { get; private set; }

        /// <summary>
        /// 消息段列表
        /// </summary>
        public List<CQCode> MessageList { get; private set; }

        /// <summary>
        /// 消息时间戳
        /// </summary>
        public long Time { get; private set; }

        /// <summary>
        /// 消息字体id
        /// </summary>
        public int Font { get; private set; }
        #endregion

        #region 构造函数
        public Message(Guid connectionGuid, int msgId, string text, List<CQCode> cqCodeList, long time, int font) : base(connectionGuid)
        {

        }
        #endregion
    }
}
