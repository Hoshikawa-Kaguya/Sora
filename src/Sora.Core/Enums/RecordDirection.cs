namespace Sora.Core.Enums;

/// <summary>Direction of a recorded chat message.</summary>
public enum RecordDirection
{
    /// <summary>A message received from the protocol that was not sent by the bot account itself.</summary>
    Inbound,

    /// <summary>A message the bot successfully submitted to the protocol through a Sora send API.</summary>
    Outbound
}
