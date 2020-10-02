using System;
using Newtonsoft.Json.Linq;
using Sora.Tool;
using Sora.TypeEnum.EventTypeEnum;

namespace Sora.JsonAdapter
{
    /// <summary>
    /// 基类事件分发
    /// 判断和分发基类事件
    /// </summary>
    internal static class EventAdapter
    {
        /// <summary>
        /// 事件类型判断和分发
        /// </summary>
        /// <param name="messageJson">消息json对象</param>
        /// <param name="connection">客户端链接接口</param>
        public static void Adapter(JObject messageJson,Guid connection)
        {
            switch (GetBaseEventType(messageJson))
            {
                //元事件类型
                case BaseEventType.meta_event:
                    MetaEventAdapter.Adapter(messageJson, GetMetaEventType(messageJson), connection);
                    break;
                default:
                    ConsoleLog.Debug("Sora",$"msg_r\nconnectionId = {connection}\nmessage = {messageJson}");
                    return;
            }
        }

        /// <summary>
        /// 获取上报事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static BaseEventType GetBaseEventType(JObject messageJson)
        {
            messageJson.TryGetValue("post_type", out JToken typeJson);
            if (typeJson == null) return 0;
            Enum.TryParse(typeJson.ToString(), out BaseEventType baseEvent);
            return baseEvent;
        }

        /// <summary>
        /// 获取元事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static MetaEventType GetMetaEventType(JObject messageJson)
        {
            messageJson.TryGetValue("meta_event_type", out JToken metaTypeJson);
            if (metaTypeJson == null) return 0;
            Enum.TryParse(metaTypeJson.ToString(), out MetaEventType metaEvent);
            return metaEvent;
        }
    }
}
