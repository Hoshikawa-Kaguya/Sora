using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Sora.EventArgs.OnebotEvent.MessageEvent;
using Sora.EventArgs.OnebotEvent.MetaEvent;
using Sora.Tool;

namespace Sora.JsonAdapter
{
    /// <summary>
    /// 基类事件分发
    /// 判断和分发基类事件
    /// </summary>
    public class EventAdapter
    {
        #region 静态记录表
        /// <summary>
        /// 心跳包记录
        /// </summary>
        internal static readonly Dictionary<Guid,long> HeartBeatList = new Dictionary<Guid, long>();
        #endregion

        #region 事件委托
        /// <summary>
        /// Onebot事件回调
        /// </summary>
        /// <typeparam name="TEventArgs">事件参数</typeparam>
        /// <param name="sender">产生事件的客户端</param>
        /// <param name="eventArgs">事件参数</param>
        /// <returns></returns>
        public delegate ValueTask OnebotAsyncCallBackHandler<in TEventArgs>(Guid sender, TEventArgs eventArgs)where TEventArgs : System.EventArgs;
        #endregion

        #region 事件分发

        /// <summary>
        /// 事件分发
        /// </summary>
        /// <param name="messageJson">消息json对象</param>
        /// <param name="connection">客户端链接接口</param>
        internal void Adapter(JObject messageJson, Guid connection)
        {
            switch (GetBaseEventType(messageJson))
            {
                //元事件类型
                case "meta_event":
                    MetaAdapter(messageJson, connection);
                    break;
                case "message":
                    MessageAdapter(messageJson, connection);
                    break;
                default:
                    ConsoleLog.Debug("Sora",$"msg_r\nconnectionId = {connection}\nmessage = {messageJson}");
                    break;
            }
        }
        #endregion

        #region 元事件处理和分发
        /// <summary>
        /// 元事件处理和分发
        /// </summary>
        /// <param name="messageJson">消息</param>
        /// <param name="connection">连接GUID</param>
        private static void MetaAdapter(JObject messageJson, Guid connection)
        {
            switch (GetMetaEventType(messageJson))
            {
                //心跳包
                case "heartbeat":
                    HeartBeatEventArgs heartBeat = messageJson.ToObject<HeartBeatEventArgs>();
                    ConsoleLog.Debug("Sora",$"Get hreatbeat from [{connection}]");
                    if (heartBeat != null)
                    {
                        //刷新心跳包记录
                        if (HeartBeatList.Any(conn => conn.Key == connection))
                        {
                            HeartBeatList[connection] = heartBeat.Time;
                        }
                        else
                        {
                            HeartBeatList.Add(connection,heartBeat.Time);
                        }
                    }
                    break;
                //生命周期
                case "lifecycle":
                    LifeCycleEventArgs lifeCycle = messageJson.ToObject<LifeCycleEventArgs>();
                    if (lifeCycle != null) ConsoleLog.Debug("Sore", $"Lifecycle event[{lifeCycle.SubType}] form [{connection}]");
                    break;
            }
        }
        #endregion

        #region 消息事件处理和分发
        /// <summary>
        /// 消息事件处理和分发
        /// </summary>
        /// <param name="messageJson">消息</param>
        /// <param name="connection">连接GUID</param>
        private void MessageAdapter(JObject messageJson, Guid connection)
        {
            switch (GetMessageType(messageJson))
            {
                //私聊事件
                case "private":
                    PrivateMessageEventArgs privateMessage = messageJson.ToObject<PrivateMessageEventArgs>();
                    if(privateMessage == null) break;
                    privateMessage.ParseSender();
                    ConsoleLog.Debug("Sora",$"Private msg {privateMessage.GetSender().Nick}({privateMessage.UserId}) : {privateMessage.RawMessage}");
                    break;
                //群聊事件
                case "group":
                    GroupMessageEventArgs groupMessage = messageJson.ToObject<GroupMessageEventArgs>();
                    if(groupMessage == null) break;
                    groupMessage.ParseSender();
                    ConsoleLog.Debug("Sora",$"Group msg({groupMessage.GroupId}) form {groupMessage.GetSender().Nick}({groupMessage.UserId}) : {groupMessage.RawMessage}");
                    break;
            }
        }
        #endregion

        #region 事件类型获取
        /// <summary>
        /// 获取上报事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static string GetBaseEventType(JObject messageJson)
        {
            messageJson.TryGetValue("post_type", out JToken typeJson);
            if (typeJson == null) return string.Empty;
            return typeJson.ToString();
        }

        /// <summary>
        /// 获取元事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static string GetMetaEventType(JObject messageJson)
        {
            messageJson.TryGetValue("meta_event_type", out JToken metaTypeJson);
            if (metaTypeJson == null) return string.Empty;
            return metaTypeJson.ToString();
        }

        /// <summary>
        /// 获取消息事件类型
        /// </summary>
        /// <param name="messageJson">消息Json对象</param>
        private static string GetMessageType(JObject messageJson)
        {
            messageJson.TryGetValue("message_type", out JToken typeJson);
            if (typeJson == null) return string.Empty;
            return typeJson.ToString();
        }
        #endregion
    }
}
