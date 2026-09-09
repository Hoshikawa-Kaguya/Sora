namespace Sora.Command.InternalEntities;

/// <summary>指令的过滤器信息</summary>
internal record CommandFilterInfo
{
    /// <summary>全部的attr信息.</summary>
    public required IReadOnlyList<Attribute> Attributes { get; set; }

    /// <summary>前置过滤器.</summary>
    public required IReadOnlyList<CommandBeforeFilterAttribute> Before { get; set; }

    /// <summary>后置过滤器.</summary>
    public required IReadOnlyList<CommandAfterFilterAttribute> After { get; set; }
}