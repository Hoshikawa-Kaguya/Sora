using System.IO;
using Sora.Entities;
using Sora.Entities.Segment;

namespace Sora.Serializer;

/// <summary>
/// 消息段ProtoBuf序列化
/// </summary>
public static class ProtoSerializer
{
#region Serialize

    /// <summary>
    /// 序列化为ProtoBuf
    /// </summary>
    public static MemoryStream SerializeToPb(this MessageBody message)
    {
        MemoryStream ms = new();
        ProtoBuf.Serializer.Serialize(ms, message);
        return ms;
    }

    /// <summary>
    /// 序列化为ProtoBuf
    /// </summary>
    public static MemoryStream SerializeToPb(this SoraSegment message)
    {
        MemoryStream ms = new();
        ProtoBuf.Serializer.Serialize(ms, message);
        return ms;
    }

#endregion

#region Deserialize

    /// <summary>
    /// 反序列化为MessageBody
    /// </summary>
    public static MessageBody DeserializePbMessage(this MemoryStream protoMsg)
    {
        protoMsg.Position = 0;
        MessageBody mb = ProtoBuf.Serializer.Deserialize<MessageBody>(protoMsg);
        return mb;
    }

    /// <summary>
    /// 反序列化为SoraSegment
    /// </summary>
    public static SoraSegment DeserializePbSegment(this MemoryStream protoMsg)
    {
        protoMsg.Position = 0;
        SoraSegment segment = ProtoBuf.Serializer.Deserialize<SoraSegment>(protoMsg);
        return segment;
    }

#endregion
}