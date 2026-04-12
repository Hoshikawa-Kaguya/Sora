using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Adapter.OneBot11.Models;
using Sora.Adapter.OneBot11.Segments;

namespace Sora.Adapter.OneBot11.Converter;

/// <summary>
///     Converts between OneBot v11 message segments and Sora segments.
/// </summary>
internal static class MessageConverter
{
    private static readonly Lazy<ILogger> LoggerLazy = new(() => SoraLogger.CreateLogger(typeof(MessageConverter).FullName!));
    private static          ILogger       Logger => LoggerLazy.Value;

    private static readonly JsonSerializer NullIgnoreSerializer = JsonSerializer.Create(
        new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

#region Converter Entry

    /// <summary>Converts OB11 segment array to Sora MessageBody.</summary>
    /// <param name="messageToken">The OB11 message array token.</param>
    /// <returns>The converted Sora message body.</returns>
    public static MessageBody ToMessageBody(JToken? messageToken)
    {
        if (messageToken is not JArray arr) return [];
        return MessageBody.FromIncoming(
            arr.Select(item => item.ToObject<OneBotSegment>())
               .OfType<OneBotSegment>()
               .Select(ConvertIncoming)
               .OfType<Segment>());
    }

    /// <summary>Converts a Sora MessageBody to OB11 segment list.</summary>
    /// <param name="body">The Sora message body.</param>
    /// <returns>A list of OB11 segments.</returns>
    public static List<OneBotSegment> ToOneBotSegments(MessageBody body)
    {
        // Onebot11 specific segment (can not send with other segment)
        DiceSegment? diceSegment = body.OfType<DiceSegment>().FirstOrDefault();
        if (diceSegment is not null) return [ConvertOutgoing(diceSegment)!];
        RpsSegment? rpsSegment = body.OfType<RpsSegment>().FirstOrDefault();
        if (rpsSegment is not null) return [ConvertOutgoing(rpsSegment)!];

        return body.Select(ConvertOutgoing).OfType<OneBotSegment>().ToList();
    }

    /// <summary>
    ///     Converts a ForwardSegment's nested messages to OB11 node list
    ///     for use with <c>send_group_forward_msg</c> / <c>send_private_forward_msg</c>.
    /// </summary>
    /// <param name="forward">The forward segment containing nested messages.</param>
    /// <returns>A list of OB11 node objects.</returns>
    public static List<JObject> ConvertForwardNodes(ForwardSegment forward)
    {
        List<JObject> nodes = [];
        foreach (ForwardedMessageNode node in forward.Messages)
        {
            List<OneBotSegment> content = [];
            foreach (Segment seg in node.Segments)
            {
                OneBotSegment? obSeg = ConvertOutgoing(seg);
                if (obSeg is not null)
                    content.Add(obSeg);
            }

            JObject nodeObj = JObject.FromObject(
                new
                    {
                        type = "node",
                        data = new
                            {
                                name = node.SenderName,
                                uin  = ((long)node.UserId).ToString(),
                                content
                            }
                    });
            nodes.Add(nodeObj);
        }

        return nodes;
    }

#endregion

#region Incoming Segment Conversions

    /// <summary>Converts an incoming OB11 segment to a Sora segment.</summary>
    /// <param name="seg">The OB11 segment.</param>
    /// <returns>The converted Sora segment, or null if the type is unsupported.</returns>
    private static Segment? ConvertIncoming(OneBotSegment seg)
    {
        return seg.Type switch
                   {
                       "text" => new TextSegment { Text = seg.Data?.Value<string>("text") ?? "" },
                       "image" => new ImageSegment
                           {
                               Url     = seg.Data?.Value<string>("url") ?? "",
                               FileUri = seg.Data?.Value<string>("file") ?? "",
                               Summary = seg.Data?.Value<string>("summary") ?? "",
                               SubType = (seg.Data?.Value<int>("subType") ?? 0) == 1
                                   ? ImageSubType.Sticker
                                   : ImageSubType.Normal
                           },
                       "at" => seg.Data?.Value<string>("qq") == "all"
                           ? new MentionAllSegment()
                           : new MentionSegment
                               {
                                   Target = long.Parse(seg.Data?.Value<string>("qq") ?? "0"),
                                   Name   = seg.Data?.Value<string>("name") ?? ""
                               },
                       "reply" => new ReplySegment
                           {
                               TargetId = long.TryParse(seg.Data?.Value<string>("id"), out long replyId)
                                   ? (MessageId)replyId
                                   : seg.Data?.Value<int>("id") ?? 0
                           },
                       "face" => new FaceSegment
                           {
                               FaceId  = seg.Data?.Value<string>("id") ?? "0",
                               IsLarge = seg.Data?.Value<int>("sub_type") == 3
                           },
                       "record" => new AudioSegment
                           {
                               Url     = seg.Data?.Value<string>("url") ?? "",
                               FileUri = seg.Data?.Value<string>("file") ?? ""
                           },
                       "video" => new VideoSegment
                           {
                               Url     = seg.Data?.Value<string>("url") ?? "",
                               FileUri = seg.Data?.Value<string>("file") ?? ""
                           },
                       "forward" => new ForwardSegment { ForwardId = seg.Data?.Value<string>("id") ?? "" },
                       "json" => new LightAppSegment
                           {
                               JsonPayload = seg.Data?.Value<string>("data") ?? ""
                           },
                       "mface" => new MarketFaceSegment
                           {
                               EmojiPackageId = seg.Data?.Value<long>("emoji_package_id") ?? 0,
                               EmojiId        = seg.Data?.Value<string>("emoji_id") ?? "",
                               Key            = seg.Data?.Value<string>("key") ?? "",
                               Summary        = seg.Data?.Value<string>("summary") ?? "",
                               Url            = seg.Data?.Value<string>("url") ?? ""
                           },
                       "file" => new FileSegment
                           {
                               FileId   = seg.Data?.Value<string>("file_id") ?? "",
                               FileName = seg.Data?.Value<string>("name") ?? "",
                               FileSize = long.TryParse(seg.Data?.Value<string>("file_size"), out long fSize) ? fSize : 0
                           },
                       "xml" => new XmlSegment
                           {
                               XmlPayload = seg.Data?.Value<string>("data") ?? ""
                           },
                       "dice" => new DiceSegment
                           {
                               Result = seg.Data?.Value<string>("result") ?? ""
                           },
                       "rps" => new RpsSegment
                           {
                               Result = seg.Data?.Value<string>("result") ?? ""
                           },
                       "markdown" => new MarkdownSegment
                           {
                               Content = seg.Data?.Value<string>("content") ?? ""
                           },
                       "flash_file" => new FlashFileMessageSegment
                           {
                               Title     = seg.Data?.Value<string>("title") ?? "",
                               FileSetId = seg.Data?.Value<string>("file_set_id") ?? "",
                               SceneType = seg.Data?.Value<int>("scene_type") ?? 0
                           },
                       _ => LogUnknownIncoming(seg.Type)
                   };
    }

#endregion

#region Outgoing Segment Conversions

    /// <summary>Converts an outgoing Sora segment to an OB11 segment.</summary>
    /// <param name="seg">The Sora segment.</param>
    /// <returns>The converted OB11 segment, or null if the type is unsupported.</returns>
    private static OneBotSegment? ConvertOutgoing(Segment seg)
    {
        return seg switch
                   {
                       MarkdownSegment md when !string.IsNullOrEmpty(md.Content) => new OneBotSegment
                           {
                               Type = "markdown",
                               Data = JObject.FromObject(new { content = md.Content })
                           },
                       TextSegment t when !string.IsNullOrEmpty(t.Text) => new OneBotSegment
                           {
                               Type = "text",
                               Data = JObject.FromObject(new { text = t.Text })
                           },
                       ImageSegment img when !string.IsNullOrEmpty(img.FileUri) || !string.IsNullOrEmpty(img.Url)
                           => new OneBotSegment
                               {
                                   Type = "image",
                                   Data = JObject.FromObject(new { file = img.FileUri is { Length: > 0 } ? img.FileUri : img.Url })
                               },
                       MentionAllSegment => new OneBotSegment
                           {
                               Type = "at",
                               Data = JObject.FromObject(new { qq = "all" })
                           },
                       MentionSegment m when (long)m.Target != 0 => new OneBotSegment
                           {
                               Type = "at",
                               Data = JObject.FromObject(
                                   new
                                       {
                                           qq = ((long)m.Target)
                                               .ToString()
                                       })
                           },
                       ReplySegment r when (long)r.TargetId != 0 => new OneBotSegment
                           {
                               Type = "reply",
                               Data = JObject.FromObject(new { id = ((long)r.TargetId).ToString() })
                           },
                       DiceSegment => new OneBotSegment
                           {
                               Type = "dice",
                               Data = new JObject()
                           },
                       RpsSegment => new OneBotSegment
                           {
                               Type = "rps",
                               Data = new JObject()
                           },
                       FaceSegment f => new OneBotSegment
                           {
                               Type = "face",
                               Data = f.IsLarge
                                   ? JObject.FromObject(new { id = f.FaceId, sub_type = 3 })
                                   : JObject.FromObject(new { id = f.FaceId })
                           },
                       AudioSegment a when !string.IsNullOrEmpty(a.FileUri) || !string.IsNullOrEmpty(a.Url)
                           => new OneBotSegment
                               {
                                   Type = "record",
                                   Data = JObject.FromObject(new { file = a.FileUri is { Length: > 0 } ? a.FileUri : a.Url })
                               },
                       VideoSegment v when !string.IsNullOrEmpty(v.FileUri) || !string.IsNullOrEmpty(v.Url)
                           => new OneBotSegment
                               {
                                   Type = "video",
                                   Data = JObject.FromObject(
                                       new
                                           {
                                               file  = v.FileUri is { Length: > 0 } ? v.FileUri : v.Url,
                                               thumb = string.IsNullOrEmpty(v.ThumbUri) ? null : v.ThumbUri
                                           },
                                       NullIgnoreSerializer)
                               },
                       // Forward segments are handled separately via ConvertForwardNodes
                       LightAppSegment la when !string.IsNullOrEmpty(la.JsonPayload) => new OneBotSegment
                           {
                               Type = "json",
                               Data = JObject.FromObject(new { data = la.JsonPayload })
                           },
                       XmlSegment xml when !string.IsNullOrEmpty(xml.XmlPayload) => new OneBotSegment
                           {
                               Type = "xml",
                               Data = JObject.FromObject(new { data = xml.XmlPayload })
                           },
                       _ => LogUnknownOutgoing(seg)
                   };
    }

#endregion

#region Logging Helpers

    private static Segment? LogUnknownIncoming(string? segmentType)
    {
        Logger.LogWarning("Unsupported incoming OB11 segment type: {SegmentType}", segmentType);
        return null;
    }

    private static OneBotSegment? LogUnknownOutgoing(Segment seg)
    {
        Logger.LogWarning("Unsupported outgoing Sora segment type for OB11: {SegmentType}", seg.GetType().Name);
        return null;
    }

#endregion
}