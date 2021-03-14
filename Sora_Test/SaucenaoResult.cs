using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sora_Test
{
    public class Header
    {
        [JsonProperty(PropertyName = "similarity")]
        public string Similarity { get; set; }

        [JsonProperty(PropertyName = "thumbnail")]
        public string Thumbnail { get; set; }

        [JsonProperty(PropertyName = "index_id")]
        public int IndexId { get; set; }

        [JsonProperty(PropertyName = "index_name")]
        public string IndexName { get; set; }

        [JsonProperty(PropertyName = "dupes")] 
        public int Dupes { get; set; }
    }

    public class PixivData
    {
        [JsonProperty(PropertyName = "ext_urls")]
        public List<string> ExtUrls { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "pixiv_id")]
        public int PixivId { get; set; }

        [JsonProperty(PropertyName = "member_name")]
        public string MemberName { get; set; }

        [JsonProperty(PropertyName = "member_id")]
        public int MemberId { get; set; }
    }

    public class SaucenaoResult
    {
        [JsonProperty(PropertyName = "header")]
        public Header Header { get; set; }

        [JsonProperty(PropertyName = "data")] public PixivData PixivData { get; set; }
    }
}