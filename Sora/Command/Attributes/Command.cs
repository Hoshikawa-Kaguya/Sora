using System;
using Sora.Enumeration;

namespace Sora.Command.Attributes
{
    /// <summary>
    /// 指令
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class Command : Attribute
    {
        #region 属性

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

        /// <summary>
        /// <para>是否在指令后触发事件</para>
        /// <para>默认不触发，如果需要触发请改为<see langword="true"/></para>
        /// </summary>
        public bool TriggerEventAfterCommand { get; init; } = false;

        #endregion

        #region 构造方法

        /// <summary>
        /// 构造方法
        /// </summary>
        protected Command(string[] commands)
        {
            CommandExpressions = commands;
        }

        /// <summary>
        /// 构造方法
        /// </summary>
        protected Command()
        {
        }

        #endregion
    }
}