using Sora.Enumeration;
using Sora.Model.CQCodeModel;
using Sora.Tool;

namespace Sora.Model.Message
{
    internal static class MessageParse
    {
        /// <summary>
        /// 处理接收到的消息段
        /// </summary>
        /// <param name="message">消息段</param>
        /// <returns>消息段列表</returns>
        internal static CQCode ParseMessage(OnebotMessage message)
        {
            if (message == null || message.RawData.Count == 0) return null;
            switch (message.MsgType)
            {
                case CQFunction.Text:
                    return new CQCode(CQFunction.Text,message.RawData.ToObject<Text>());
                case CQFunction.Face:
                    return new CQCode(CQFunction.Face,message.RawData.ToObject<Face>());
                case CQFunction.Image:
                    return new CQCode(CQFunction.Image,message.RawData.ToObject<Image>());
                case CQFunction.Record:
                    return new CQCode(CQFunction.Record,message.RawData.ToObject<Record>());
                default:
                    ConsoleLog.Error("我叼你妈的","");
                    return new CQCode(CQFunction.Unknown, message.RawData);
            }
        }
    }
}
