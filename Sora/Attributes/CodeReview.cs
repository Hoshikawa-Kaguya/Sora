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
        public string Person { get; set; }

        /// <summary>
        /// 时间
        /// </summary>
        public string Time { get; set; }

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
}