using Sora.Enumeration;

namespace Sora.Command.Attributes
{
    /// <summary>
    /// 私聊指令
    /// </summary>
    public sealed class PrivateCommand : Command
    {
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="command">指令表达式</param>
        /// <param name="matchType">匹配类型</param>
        /// <param name="desc">说明(在执行失败时可能会用到)</param>
        public PrivateCommand(string command, MatchType matchType, string desc = "") :
            base(command, matchType, desc)
        {
        }
    }
}
