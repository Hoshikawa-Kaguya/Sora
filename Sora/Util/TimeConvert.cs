using System;

namespace Sora.Util;

/// <summary>
/// <para>DateTime和时间戳的转换</para>
/// </summary>
public static class TimeConvert
{
    private static readonly DateTime _unixStartTime = new(1970,
                                                          1,
                                                          1,
                                                          8,
                                                          0,
                                                          0,
                                                          0);

    /// <summary>
    /// DateTime转时间戳
    /// <param name="date">时间</param>
    /// <param name="isMilliseconds">是否精确到毫秒（13位时间戳）</param>
    /// </summary>
    public static long ToTimeStamp(this DateTime date, bool isMilliseconds = false)
    {
        return isMilliseconds
            ? (long)(date - _unixStartTime).TotalMilliseconds
            : (long)(date - _unixStartTime).TotalSeconds;
    }

    /// <summary>
    /// 时间戳转DateTime
    /// <param name="timeStamp">时间戳</param>
    /// <param name="isMilliseconds">是否精确到毫秒（13位时间戳）</param>
    /// </summary>
    public static DateTime ToDateTime(this long timeStamp, bool isMilliseconds = false)
    {
        return isMilliseconds ? _unixStartTime.AddMilliseconds(timeStamp) : _unixStartTime.AddSeconds(timeStamp);
    }
}