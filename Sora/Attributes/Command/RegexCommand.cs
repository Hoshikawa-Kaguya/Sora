using System;
using System.Text.RegularExpressions;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;

namespace Sora.Attributes.Command;

/// <summary>
/// 指令
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RegexCommand : Attribute
{
    #region 私有字段

    private readonly string[] _commandExpressions;

    #endregion

    #region 属性

    /// <summary>
    /// <para>正则指令表达式</para>
    /// <para>默认为全字匹配(注意:由于使用的是正则匹配模式，部分符号需要转义，如<see langword="\?"/>)</para>
    /// <para>也可以为正则表达式</para>
    /// </summary>
    public string[] CommandExpressions
    {
        get => _commandExpressions ?? null;
        init => _commandExpressions = value ?? throw new NullReferenceException("CommandExpression cannot be null");
    }

    //TODO
    /// <summary>
    /// 指令响应的特定群组
    /// </summary>
    public long[] SourceGroups { get; init; } = Array.Empty<long>();

    //TODO
    /// <summary>
    /// 指令响应的特定用户
    /// </summary>
    public long[] SourceUsers { get; init; } = Array.Empty<long>();

    /// <summary>
    /// 指令响应的源
    /// </summary>
    public SourceFlag SourceType { get; init; } = SourceFlag.None;

    /// <summary>
    /// <para>权限限制</para>
    /// <para>私聊和群聊的默认值<see cref="MemberRoleType.Member"/></para>
    /// <para>当用户被设置SuperUser时值为<see cref="MemberRoleType.SuperUser"/></para>
    /// </summary>
    public MemberRoleType PermissionLevel { get; init; } = MemberRoleType.Member;

    /// <summary>
    /// <para>指令描述</para>
    /// <para>默认为空字符串</para>
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// <para>匹配类型</para>
    /// <para>默认为全字匹配</para>
    /// </summary>
    public MatchType MatchType { get; init; } = MatchType.Full;

    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// 正则匹配选项
    /// </summary>
    public RegexOptions RegexOptions { get; init; } = RegexOptions.None;

    /// <summary>
    /// 指令执行异常处理
    /// </summary>
    public readonly Action<Exception> ExceptionHandler = null;

    #endregion

    #region 构造方法

    /// <summary>
    /// 构造方法
    /// </summary>
    public RegexCommand(string[] commands)
    {
        CommandExpressions = commands;
        Regex.CacheSize++;
    }

    /// <summary>
    /// 构造方法
    /// </summary>
    public RegexCommand()
    {
        Regex.CacheSize++;
    }

    #endregion
}