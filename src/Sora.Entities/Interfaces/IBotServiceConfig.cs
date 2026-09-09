namespace Sora.Entities.Interfaces;

/// <summary>
///     Base configuration for a bot service.
/// </summary>
public interface IBotServiceConfig
{
    /// <summary>List of super user IDs with elevated permissions.</summary>
    UserId[] SuperUsers { get; }

    /// <summary>List of blocked user IDs.</summary>
    UserId[] BlockUsers { get; }

    /// <summary>Whether to enable the command manager.</summary>
    bool EnableCommandManager { get; }

    /// <summary>Whether to automatically mark messages as read after receiving them.</summary>
    bool AutoMarkMessageRead => true;

    /// <summary>Drop messages that self sent.</summary>
    bool DropSelfMessage => true;
}