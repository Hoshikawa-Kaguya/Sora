using System;

namespace Sora.Attributes
{
    /// <summary>
    /// 代码审核特性类
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class Reviewed : Attribute
    {
        /// <summary>
        /// 审核者
        /// </summary>
        public string Person { get; }

        /// <summary>
        /// 时间
        /// </summary>
        public string Time { get; }

        /// <summary>
        /// 表明您已经审核了该代码
        /// </summary>
        /// <param name="person">审核人</param>
        /// <param name="dt">时间</param>
        public Reviewed(string person, string dt)
        {
            Person = person;
            Time   = dt;
        }
    }

    /// <summary>
    /// 需要审查特性类
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class NeedReview : Attribute
    {
        /// <summary>
        /// 修改的位置(行号或ALL)
        /// </summary>
        public string ModifiedLines { get; }

        /// <summary>
        /// 表明您认为这段代码需要审查
        /// </summary>
        /// <param name="lines">修改行号/ALL</param>
        public NeedReview(string lines)
        {
            ModifiedLines = lines;
        }
    }
}