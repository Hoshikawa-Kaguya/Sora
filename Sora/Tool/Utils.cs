using System;

namespace Sora.Tool
{
    /// <summary>
    /// 工具类
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// 获取当前时间戳
        /// 时间戳单位(秒)
        /// </summary>
        public static long GetNowTimeStamp() =>(long) (DateTime.Now - new DateTime(1970, 1, 1, 8, 0, 0, 0)).TotalSeconds;
    }
}
