using System;

namespace Sora.Attributes.Command;

/// <summary>
/// 指令组
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandSeries : Attribute
{
    /// <summary>
    /// <para>指令组名</para>
    /// <para>可以用于标识和使能控制</para>
    /// <para>值为空字符串时默认为类名</para>
    /// </summary>
    public string SeriesName { get; init; } = string.Empty;

    /// <summary>
    /// 指令组前缀
    /// </summary>
    public string GroupPrefix { get; init; } = string.Empty;
}