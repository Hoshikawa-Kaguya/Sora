using ProtoBuf;

namespace Sora.Entities.Segment.DataModel;

/// <summary>
/// 空数据消息段
/// </summary>
[ProtoContract]
[ProtoInclude(1, typeof(AtSegment))]
[ProtoInclude(2, typeof(CardImageSegment))]
[ProtoInclude(3, typeof(CodeSegment))]
[ProtoInclude(4, typeof(CustomMusicSegment))]
[ProtoInclude(5, typeof(CustomReplySegment))]
[ProtoInclude(6, typeof(FaceSegment))]
[ProtoInclude(7, typeof(ForwardSegment))]
[ProtoInclude(8, typeof(ImageSegment))]
[ProtoInclude(9, typeof(MusicSegment))]
[ProtoInclude(10, typeof(PokeSegment))]
[ProtoInclude(11, typeof(RecordSegment))]
[ProtoInclude(12, typeof(RedbagSegment))]
[ProtoInclude(13, typeof(ReplySegment))]
[ProtoInclude(14, typeof(ShareSegment))]
[ProtoInclude(15, typeof(TextSegment))]
[ProtoInclude(16, typeof(TtsSegment))]
[ProtoInclude(17, typeof(VideoSegment))]
[ProtoInclude(18, typeof(UnknownSegment))]
public record BaseSegment;