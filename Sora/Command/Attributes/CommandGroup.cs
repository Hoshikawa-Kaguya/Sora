using System;

namespace Sora.Command.Attributes
{
    /// <summary>
    /// 指令组
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandGroup : Attribute
    {
    }
}