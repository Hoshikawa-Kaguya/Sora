using System;
using System.Reflection;
using Sora.Enumeration;

namespace Sora.Attributes.Command
{
    public sealed class WaitiableCommand
    {
        #region 属性

        public MethodInfo ParentMethod { get; init; }

        private readonly string[] commandExpressions;

        /// <summary>
        /// <para>正则指令表达式</para>
        /// <para>默认为全字匹配(注意:由于使用的是正则匹配模式，部分符号需要转义，如<see langword="\?"/>)</para>
        /// <para>也可以为正则表达式</para>
        /// </summary>
        public string[] CommandExpressions
        {
            get => commandExpressions ?? throw new NullReferenceException("CommandExpression cannot be null");
            init => commandExpressions = value ?? throw new NullReferenceException("CommandExpression cannot be null");
        }

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

        #endregion

        #region 构造方法

        /// <summary>
        /// 构造方法
        /// </summary>
        public WaitiableCommand(MethodInfo parent,string[] commands,MatchType matchType)
        {
            ParentMethod       = parent;
            CommandExpressions = commands;
            MatchType          = matchType;
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        public WaitiableCommand()
        {
        }

        #endregion
    }
}