using System;

namespace Sora.Attributes.Command;

/// <summary>
/// 指令组
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandGroup : Attribute
{
}