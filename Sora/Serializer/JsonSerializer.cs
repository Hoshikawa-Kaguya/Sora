using Newtonsoft.Json;
using Sora.Entities;
using Sora.Entities.Segment;

namespace Sora.Serializer;

/// <summary>
/// 消息段Json序列化
/// </summary>
public static class JsonSerializer
{
#region Serialize

    /// <summary>
    /// 序列化为json
    /// </summary>
    public static string SerializeToJson(this MessageBody message, Formatting formatting = Formatting.None)
    {
        return JsonConvert.SerializeObject(message, formatting);
    }

    /// <summary>
    /// 序列化为json
    /// </summary>
    public static string SerializeToJson(this SoraSegment message, Formatting formatting = Formatting.None)
    {
        return JsonConvert.SerializeObject(message, formatting);
    }

#endregion

#region Deserialize

    /// <summary>
    /// 反序列化为MessageBody
    /// </summary>
    public static MessageBody DeserializeJsonMessage(this string json)
    {
        return JsonConvert.DeserializeObject<MessageBody>(json);
    }

    /// <summary>
    /// 反序列化为SoraSegment
    /// </summary>
    public static SoraSegment DeserializeJsonSegment(this string json)
    {
        return JsonConvert.DeserializeObject<SoraSegment>(json);
    }

#endregion
}