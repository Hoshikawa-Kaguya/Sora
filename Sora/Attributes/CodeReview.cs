using System;

namespace Sora.Attributes;
//TODO:代码审核规范（必读）
//对于有疑问的代码可以使用TODO进行疑问，相应维护者应该及时进行回复
//下面所有用法前面都请带上TODO和冒号
//例：Problem：这里需要检查是否可能存在的空值（这里TODO后面需要加冒号）
//答：Checked：这里不存在空值异常，可以正常使用
//并留下这段注释，以便以后复查时理解。
//
//对于改进，请使用Advise
//相应维护者看见后应该做出回应，无论是进行相应修改还是给出Reply（不采纳原因）
//提出建议者应该在看见Reply后把Reply删除以证明自己看见了
//或者也可以继续提出其他Advise。
//例：Advise：采用自定义数据结构
//答：Reply：不采用，因为只有这一处修改（这句话看见后，包括Advise和Reply都会被删除）

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