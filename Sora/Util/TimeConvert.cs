using System;

namespace Sora.Util;

/// <summary>
/// <para>DateTime和时间戳的转换</para>
/// </summary>
public static class TimeConvert
{
    private static readonly DateTime UnixStartTime = new(1970, 1, 1, 8, 0, 0, 0);

    /// <summary>
    /// DateTime转时间戳
    /// <param name="date">时间</param>
    /// <param name="isMilliSeconds">是否精确到毫秒（13位时间戳）</param>
    /// </summary>
    public static long ToTimeStamp(this DateTime date, bool isMilliSeconds = false)
    {
        return isMilliSeconds
            ? (long) (date - UnixStartTime).TotalMilliseconds
            : (long) (date - UnixStartTime).TotalSeconds;
    }

    /// <summary>
    /// 时间戳转DateTime
    /// <param name="timeStamp">时间戳</param>
    /// <param name="isMilliSeconds">是否精确到毫秒（13位时间戳）</param>
    /// </summary>
    public static DateTime ToDateTime(this long timeStamp, bool isMilliSeconds = false)
    {
        return isMilliSeconds
            ? UnixStartTime.AddMilliseconds(timeStamp)
            : UnixStartTime.AddSeconds(timeStamp);
    }
}