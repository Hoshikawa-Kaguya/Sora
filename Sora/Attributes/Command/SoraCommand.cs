using System;
using System.Text.RegularExpressions;
using Sora.Enumeration;
using Sora.Enumeration.EventParamsType;

namespace Sora.Attributes.Command;

/// <summary>
/// 指令
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SoraCommand : Attribute
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
        get => _commandExpressions ?? Array.Empty<string>();
        init => _commandExpressions = value ?? throw new NullReferenceException("CommandExpression cannot be null");
    }

    /// <summary>
    /// <para>限制指令响应的群组</para>
    /// <para>私聊时无效</para>
    /// </summary>
    public long[] SourceGroups { get; init; } = Array.Empty<long>();

    /// <summary>
    /// 限制指令响应的用户
    /// </summary>
    public long[] SourceUsers { get; init; } = Array.Empty<long>();

    /// <summary>
    /// 限制响应的bot账号来源
    /// </summary>
    public long[] SourceLogins { get; init; } = Array.Empty<long>();

    /// <summary>
    /// 指令响应的源
    /// </summary>
    public MessageSourceMatchFlag SourceType { get; init; } = MessageSourceMatchFlag.All;

    /// <summary>
    /// <para>权限限制</para>
    /// <para>此为QQ群聊权限，私聊时无效</para>
    /// </summary>
    public MemberRoleType PermissionLevel { get; init; } = MemberRoleType.Member;

    /// <summary>
    /// <para>机器人管理员指令</para>
    /// <para>此权限会在PermissionLevel之后进行判定</para>
    /// </summary>
    public bool SuperUserCommand { get; init; } = false;

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
    /// <para>优先级</para>
    /// <para>数值越高优先级越高</para>
    /// </summary>
    public int Priority { get; init; } = 0;

    /// <summary>
    /// 正则匹配选项
    /// </summary>
    public RegexOptions RegexOptions { get; init; } = RegexOptions.None;

#endregion
}