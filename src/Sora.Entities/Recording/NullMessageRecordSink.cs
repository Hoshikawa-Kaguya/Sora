namespace Sora.Entities.Recording;

/// <summary>Sink used before the service attaches its own; it records nothing.</summary>
internal sealed class NullMessageRecordSink : IMessageRecordSink
{
    /// <summary>Gets the shared instance.</summary>
    internal static NullMessageRecordSink Instance { get; } = new();

    /// <inheritdoc />
    public bool IsEnabled => false;

    /// <inheritdoc />
    public void Write(MessageRecord record)
    {
    }
}