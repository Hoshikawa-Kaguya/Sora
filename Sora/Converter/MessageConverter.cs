using System;
using System.Collections.Generic;
using Sora.Entities;
using Sora.Entities.MessageElement;

namespace Sora.Converter
{
    /// <summary>
    /// 消息预处理扩展函数
    /// </summary>
    internal static class MessageConverter
    {
        /// <summary>
        /// 预处理消息队列
        /// </summary>
        /// <param name="content">消息数据</param>
        internal static MessageBody ToCQCodeList(this object[] content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            MessageBody msgList = new();
            foreach (var msgObj in content)
            {
                switch (msgObj)
                {
                    case CQCode cqCode:
                        msgList.Add(cqCode);
                        break;
                    case IEnumerable<CQCode> cqCodes:
                        msgList.AddRange(cqCodes);
                        break;
                    case null:
                        msgList.Add(CQCodes.CQText("null"));
                        break;
                    default:
                        msgList.Add(CQCodes.CQText(msgObj.ToString()));
                        break;
                }
            }

            return msgList;
        }
    }
}