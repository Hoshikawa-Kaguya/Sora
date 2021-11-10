using Sora.Enumeration.EventParamsType;

namespace Sora.Attributes.Command;

/// <summary>
/// 群组指令
/// </summary>
public sealed class GroupCommand : RegexCommand
{
    #region 属性

    /// <summary>
    /// <para>成员权限限制</para>
    /// <para>默认值:<see cref="MemberRoleType.Member"/></para>
    /// </summary>
    public MemberRoleType PermissionLevel { get; init; } = MemberRoleType.Member;

    #endregion

    #region 构造方法

    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="command">
    /// <para>正则指令表达式</para>
    /// <para>默认为全字匹配(注意:由于使用的是正则匹配模式，部分符号需要转义，如<see langword="\?"/>)</para>
    /// <para>也可以为正则表达式</para>
    /// </param>
    public GroupCommand(string[] command) : base(command)
    {
    }

    /// <summary>
    /// 构造方法
    /// </summary>
    public GroupCommand()
    {
    }

    #endregion
}