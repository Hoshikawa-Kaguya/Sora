using System;
using System.IO;
using System.Linq;
using Sora.Enumeration;

namespace Sora.Entities.Segment;

/// <summary>
/// 消息段扩展
/// </summary>
public static class SegmentHelper
{
    #region 扩展构建方法

    /// <summary>
    /// 生成AT 消息段
    /// </summary>
    /// <param name="uid">uid</param>
    public static SoraSegment ToAt(this long uid)
    {
        return SoraSegment.At(uid);
    }

    /// <summary>
    /// 生成AT 消息段
    /// </summary>
    /// <param name="uid">uid</param>
    public static SoraSegment ToAt(this int uid)
    {
        return SoraSegment.At(uid);
    }

    #endregion

    #region 消息字符串处理

    /// <summary>
    /// 处理传入数据
    /// </summary>
    /// <param name="dataStr">数据字符串</param>
    /// <returns>
    /// <para><see langword="retStr"/>处理后数据字符串</para>
    /// <para><see langword="isMatch"/>是否为合法数据字符串</para>
    /// </returns>
    internal static (string retStr, bool isMatch) ParseDataStr(string dataStr)
    {
        if (string.IsNullOrEmpty(dataStr)) return (null, false);
        dataStr = dataStr.Replace('\\', '/');
        //当字符串太长时跳过正则检查
        if (dataStr.Length > 1000) return (dataStr, true);

        var type = StaticVariable.FileRegices.Single(i => i.Value.IsMatch(dataStr)).Key;

        switch (type)
        {
            case FileType.UnixFile: //linux/osx
                if (Environment.OSVersion.Platform != PlatformID.Unix   &&
                    Environment.OSVersion.Platform != PlatformID.MacOSX &&
                    !File.Exists(dataStr))
                    return (dataStr, false);
                else
                    return ($"file:///{dataStr}", true);
            case FileType.WinFile: //win
                if (Environment.OSVersion.Platform == PlatformID.Win32NT && File.Exists(dataStr))
                    return ($"file:///{dataStr}", true);
                else
                    return (dataStr, false);
            default:
                return (dataStr, true);
        }
    }

    #endregion
}