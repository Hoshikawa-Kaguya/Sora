namespace Sora.Core.Enums;

/// <summary>
///     Represents a group member's role/permission level.
/// </summary>
public enum MemberRole
{
    /// <summary>Role is unknown.</summary>
    Unknown = 0,

    /// <summary>Regular group member.</summary>
    Member = 1,

    /// <summary>Group administrator.</summary>
    Admin = 2,

    /// <summary>Group owner.</summary>
    Owner = 3
}