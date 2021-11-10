using System.ComponentModel;

namespace Sora.Enumeration;

/// <summary>
/// 指令执行类型
/// </summary>
[DefaultValue(Method)]
internal enum InvokeType
{
    /// <summary>
    /// Method
    /// </summary>
    Method,

    /// <summary>
    /// Action
    /// </summary>
    Action
}