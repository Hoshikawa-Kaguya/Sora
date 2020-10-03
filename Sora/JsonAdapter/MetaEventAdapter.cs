using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Sora.EventArgs.OnebotEvent.MetaEvent;
using Sora.Tool;
using Sora.TypeEnum.EventTypeEnum;

namespace Sora.JsonAdapter
{
    internal class MetaEventAdapter : EventAdapter
    {
        /// <summary>
        /// 心跳包记录
        /// </summary>
        internal static readonly Dictionary<Guid,long> HeartBeatList = new Dictionary<Guid, long>();

        /// <summary>
        /// 元事件处理和分发
        /// </summary>
        /// <param name="messageJson">消息</param>
        /// <param name="eventType">事件类型</param>
        /// <param name="connection">客户端连接接口</param>
        internal void Adapter(JObject messageJson, MetaEventType eventType, Guid connection)
        {
            switch (eventType)
            {
                //心跳包
                case MetaEventType.heartbeat:
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
                case MetaEventType.lifecycle:
                    LifeCycleEventArgs lifeCycle = messageJson.ToObject<LifeCycleEventArgs>();
                    if (lifeCycle != null) ConsoleLog.Debug("Sore", $"Lifecycle event[{lifeCycle.SubType}] form [{connection}]");
                    break;
            }
        }
    }
}
