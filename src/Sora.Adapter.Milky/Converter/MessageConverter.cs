using Mapster;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sora.Adapter.Milky.Models;

namespace Sora.Adapter.Milky.Converter;

/// <summary>
///     Converts between Milky message segments and Sora segments.
/// </summary>
internal static class MessageConverter
{
    private static readonly Lazy<ILogger> LoggerLazy = new(() => SoraLogger.CreateLogger(typeof(MessageConverter).FullName!));
    private static          ILogger       Logger => LoggerLazy.Value;

    private static readonly JsonSerializer NullIgnoreSerializer =
        JsonSerializer.Create(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

#region Public Conversion API

    /// <summary>Converts Milky segment list to Sora MessageBody.</summary>
    /// <param name="segments">The list of Milky segments.</param>
    /// <returns>The converted Sora message body.</returns>
    public static MessageBody ToMessageBody(List<MilkySegment> segments) =>
        MessageBody.FromIncoming(segments.Select(ConvertIncoming).OfType<Segment>());

    /// <summary>Converts Sora MessageBody to Milky segment list.</summary>
    /// <param name="body">The Sora message body.</param>
    /// <returns>A list of Milky segments.</returns>
    public static List<MilkySegment> ToMilkySegments(MessageBody body) => body.Select(ConvertOutgoing).OfType<MilkySegment>().ToList();

#endregion

#region Incoming Segment Conversions

    /// <summary>Converts an incoming Milky segment to a Sora segment.</summary>
    /// <param name="seg">The incoming Milky segment.</param>
    /// <returns>The converted Sora segment, or null if the type is unsupported.</returns>
    private static Segment? ConvertIncoming(MilkySegment seg)
    {
        return seg.Type switch
                   {
                       "text" => new TextSegment { Text = seg.Data?.Value<string>("text") ?? string.Empty },
                       "mention" => new MentionSegment
                           {
                               Target = seg.Data?.Value<long>("user_id") ?? 0,
                               Name   = seg.Data?.Value<string>("name") ?? ""
                           },
                       "mention_all" => new MentionAllSegment(),
                       "face" => new FaceSegment
                           {
                               FaceId  = seg.Data?.Value<string>("face_id") ?? "0",
                               IsLarge = seg.Data?.Value<bool>("is_large") ?? false
                           },
                       "reply" => ConvertIncomingReply(seg),
                       "image" => new ImageSegment
                           {
                               ResourceId = seg.Data?.Value<string>("resource_id") ?? string.Empty,
                               Url        = seg.Data?.Value<string>("temp_url") ?? string.Empty,
                               Width      = seg.Data?.Value<int>("width") ?? 0,
                               Height     = seg.Data?.Value<int>("height") ?? 0,
                               SubType    = (seg.Data?.Value<string>("sub_type") ?? "").Adapt<ImageSubType>(),
                               Summary    = seg.Data?.Value<string>("summary") ?? string.Empty
                           },
                       "record" => new AudioSegment
                           {
                               ResourceId = seg.Data?.Value<string>("resource_id") ?? string.Empty,
                               Url        = seg.Data?.Value<string>("temp_url") ?? string.Empty,
                               Duration   = seg.Data?.Value<int>("duration") ?? 0
                           },
                       "video" => new VideoSegment
                           {
                               ResourceId = seg.Data?.Value<string>("resource_id") ?? string.Empty,
                               Url        = seg.Data?.Value<string>("temp_url") ?? string.Empty,
                               Duration   = seg.Data?.Value<int>("duration") ?? 0,
                               Width      = seg.Data?.Value<int>("width") ?? 0,
                               Height     = seg.Data?.Value<int>("height") ?? 0
                           },
                       "file" => new FileSegment
                           {
                               FileId   = seg.Data?.Value<string>("file_id") ?? string.Empty,
                               FileName = seg.Data?.Value<string>("file_name") ?? string.Empty,
                               FileSize = seg.Data?.Value<long>("file_size") ?? 0,
                               FileHash = seg.Data?.Value<string>("file_hash") ?? string.Empty
                           },
                       "forward" => new ForwardSegment
                           {
                               ForwardId = seg.Data?.Value<string>("forward_id") ?? string.Empty,
                               Title     = seg.Data?.Value<string>("title") ?? string.Empty,
                               Preview   = seg.Data?["preview"]?.ToObject<List<string>>() ?? [],
                               Summary   = seg.Data?.Value<string>("summary") ?? string.Empty
                           },
                       "market_face" => new MarketFaceSegment
                           {
                               EmojiPackageId = seg.Data?.Value<long>("emoji_package_id") ?? 0,
                               EmojiId        = seg.Data?.Value<string>("emoji_id") ?? string.Empty,
                               Key            = seg.Data?.Value<string>("key") ?? string.Empty,
                               Summary        = seg.Data?.Value<string>("summary") ?? string.Empty,
                               Url            = seg.Data?.Value<string>("url") ?? string.Empty
                           },
                       "light_app" => new LightAppSegment
                           {
                               AppName     = seg.Data?.Value<string>("app_name") ?? string.Empty,
                               JsonPayload = seg.Data?.Value<string>("json_payload") ?? string.Empty
                           },
                       "xml" => new XmlSegment
                           {
                               ServiceId  = seg.Data?.Value<int>("service_id") ?? 0,
                               XmlPayload = seg.Data?.Value<string>("xml_payload") ?? string.Empty
                           },
                       _ => LogUnknownIncoming(seg.Type)
                   };
    }

    /// <summary>Converts an incoming Milky reply segment to a Sora ReplySegment with quoted message content.</summary>
    /// <param name="seg">The incoming Milky reply segment.</param>
    /// <returns>The converted ReplySegment.</returns>
    private static ReplySegment ConvertIncomingReply(MilkySegment seg)
    {
        // Parse quoted message segments
        MessageBody? content       = null;
        JArray?      segmentsArray = seg.Data?.Value<JArray>("segments");
        if (segmentsArray is { Count: > 0 })
        {
            List<MilkySegment> innerSegments = segmentsArray
                                               .Select(token => token.ToObject<MilkySegment>())
                                               .OfType<MilkySegment>()
                                               .ToList();
            content = ToMessageBody(innerSegments);
        }

        return new ReplySegment
            {
                TargetId   = seg.Data?.Value<long>("message_seq") ?? 0,
                SenderId   = seg.Data?.Value<long>("sender_id") ?? 0,
                SenderName = seg.Data?.Value<string>("sender_name"),
                Time       = seg.Data?.Value<long>("time") ?? 0,
                Content    = content
            };
    }

#endregion

#region Outgoing Segment Conversions

    /// <summary>Converts an outgoing Sora segment to a Milky segment.</summary>
    /// <param name="seg">The outgoing Sora segment.</param>
    /// <returns>The converted Milky segment, or null if the type is unsupported.</returns>
    private static MilkySegment? ConvertOutgoing(Segment? seg) =>
        seg switch
            {
                TextSegment t when !string.IsNullOrEmpty(t.Text) => new MilkySegment
                    {
                        Type = "text",
                        Data = JObject.FromObject(new { text = t.Text })
                    },
                MentionSegment m when (long)m.Target != 0 => new MilkySegment
                    {
                        Type = "mention",
                        Data = JObject.FromObject(
                            new
                                {
                                    user_id = (long)m.Target
                                })
                    },
                MentionAllSegment => new MilkySegment
                    {
                        Type = "mention_all",
                        Data = new JObject()
                    },
                FaceSegment f => new MilkySegment
                    {
                        Type = "face",
                        Data = JObject.FromObject(new { face_id = f.FaceId, is_large = f.IsLarge })
                    },
                ReplySegment r when (long)r.TargetId != 0 => new MilkySegment
                    {
                        Type = "reply",
                        Data = JObject.FromObject(
                            new
                                {
                                    message_seq = (long)r.TargetId
                                })
                    },
                ImageSegment img when !string.IsNullOrEmpty(img.FileUri) || !string.IsNullOrEmpty(img.Url)
                    => new MilkySegment
                        {
                            Type = "image",
                            Data = JObject.FromObject(
                                new
                                    {
                                        uri      = img.FileUri is { Length: > 0 } ? img.FileUri : img.Url,
                                        sub_type = img.SubType.Adapt<string>(),
                                        summary  = string.IsNullOrEmpty(img.Summary) ? null : img.Summary
                                    },
                                NullIgnoreSerializer)
                        },
                AudioSegment a when !string.IsNullOrEmpty(a.FileUri) || !string.IsNullOrEmpty(a.Url)
                    => new MilkySegment
                        {
                            Type = "record",
                            Data = JObject.FromObject(new { uri = a.FileUri is { Length: > 0 } ? a.FileUri : a.Url })
                        },
                VideoSegment v when !string.IsNullOrEmpty(v.FileUri) || !string.IsNullOrEmpty(v.Url)
                    => new MilkySegment
                        {
                            Type = "video",
                            Data = JObject.FromObject(
                                new
                                    {
                                        uri       = v.FileUri is { Length: > 0 } ? v.FileUri : v.Url,
                                        thumb_uri = string.IsNullOrEmpty(v.ThumbUri) ? null : v.ThumbUri
                                    },
                                NullIgnoreSerializer)
                        },
                ForwardSegment fw => ConvertOutgoingForward(fw),
                LightAppSegment la when !string.IsNullOrEmpty(la.JsonPayload)
                    => new MilkySegment
                        {
                            Type = "light_app",
                            Data = JObject.FromObject(new { json_payload = la.JsonPayload })
                        },
                _ => LogUnknownOutgoing(seg)
            };

    /// <summary>Converts an outgoing forward segment to a Milky forward segment.</summary>
    /// <param name="fw">The outgoing forward segment.</param>
    /// <returns>The converted Milky forward segment, or null if the segment has no messages.</returns>
    private static MilkySegment? ConvertOutgoingForward(ForwardSegment fw)
    {
        // Must have messages to send
        if (fw.Messages.Count == 0) return null;

        object[] messagesArray =
            fw.Messages.Select(object (msg) => new
                  {
                      user_id     = (long)msg.UserId,
                      sender_name = msg.SenderName,
                      segments = msg.Segments
                                    .Select(ConvertOutgoing)
                                    .OfType<MilkySegment>()
                                    .ToList()
                  })
              .ToArray();

        JObject data = JObject.FromObject(
            new
                {
                    messages = messagesArray,
                    title    = string.IsNullOrEmpty(fw.Title) ? null : fw.Title,
                    preview  = fw.Preview.Count > 0 ? fw.Preview : null,
                    summary  = string.IsNullOrEmpty(fw.Summary) ? null : fw.Summary,
                    prompt   = string.IsNullOrEmpty(fw.Prompt) ? null : fw.Prompt
                },
            NullIgnoreSerializer);

        return new MilkySegment { Type = "forward", Data = data };
    }

#endregion

#region Logging Helpers

    private static Segment? LogUnknownIncoming(string? segmentType)
    {
        Logger.LogWarning("Unsupported incoming Milky segment type: {SegmentType}", segmentType);
        return null;
    }

    private static MilkySegment? LogUnknownOutgoing(Segment? seg)
    {
        Logger.LogWarning("Unsupported outgoing Sora segment type for Milky: {SegmentType}", seg?.GetType().Name ?? "(null)");
        return null;
    }

#endregion
}