using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sora.Model
{
    public class ServerConfig
    {
        /// <summary>
        /// 反向服务器端口
        /// </summary>
        public int Port { get; set; } = 8080;

        /// <summary>
        /// 鉴权Token
        /// </summary>
        public string AccessToken { get; set; } = "";

        /// <summary>
        /// API请求路径
        /// </summary>
        public string ApiPath { get; set; } = "api";

        /// <summary>
        /// Event请求路径
        /// </summary>
        public string EventPath { get; set; } = "event";

        /// <summary>
        /// 
        /// </summary>
        public string UniversalPath { get; set; } = "";

        /// <summary>
        /// 心跳包间隔(ms)
        /// </summary>
        public int HeartBeat { get; set; } = 5000;
    }
}
