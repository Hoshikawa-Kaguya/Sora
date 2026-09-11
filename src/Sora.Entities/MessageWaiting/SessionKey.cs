namespace Sora.Entities.MessageWaiting;

internal readonly record struct SessionKey()
{
    public readonly Guid              ConnectionId;
    public readonly UserId            SenderId;
    public readonly GroupId           GroupId;
    public readonly MessageSourceType SourceType;

    public SessionKey(Guid connectionId, UserId senderId, GroupId groupId, MessageSourceType sourceType)
        : this()
    {
        ConnectionId = connectionId;
        SenderId     = senderId;
        GroupId      = sourceType == MessageSourceType.Group ? groupId : default;
        SourceType   = sourceType;
    }
}