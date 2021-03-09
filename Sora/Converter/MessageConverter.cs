using System;
using System.Collections.Generic;
using Sora.Entities.CQCodes;

namespace Sora.Converter
{
    /// <summary>
    /// 消息预处理扩展函数
    /// </summary>
    internal static class MessageConverter
    {
        #region 常用扩展函数

        /// <summary>
        /// 预处理消息队列
        /// </summary>
        /// <param name="content">消息数据</param>
        internal static List<CQCode> ToCQCodeList(this object[] content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));
            List<CQCode> msgList = new();
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
                    default:
                        msgList.Add(CQCode.CQText(msgObj.ToString()));
                        break;
                }
            }

            return msgList;
        }

        #endregion
    }
}