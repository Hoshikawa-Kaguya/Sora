namespace Sora.Attributes.Command;

/// <summary>
/// 私聊指令
/// </summary>
public sealed class PrivateCommand : RegexCommand
{
    /// <summary>
    /// 构造方法
    /// </summary>
    /// <param name="command">
    /// <para>正则指令表达式</para>
    /// <para>默认为全字匹配(注意:由于使用的是正则匹配模式，部分符号需要转义，如<see langword="\?"/>)</para>
    /// <para>也可以为正则表达式</para>
    /// </param>
    public PrivateCommand(string[] command) : base(command)
    {
    }

    /// <summary>
    /// 构造方法
    /// </summary>
    public PrivateCommand()
    {
    }
}