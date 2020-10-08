using Newtonsoft.Json;

namespace Sora.Model.SoraModel
{
    /// <summary>
    /// 群组类
    /// </summary>
    public class Group
    {
        /// <summary>
        /// 群号
        /// </summary>
        [JsonProperty(PropertyName = "group_id")]
        public long Id { get; set; }

        /// <summary>
        /// 群名称
        /// </summary>
        [JsonProperty(PropertyName = "group_name")]
        public string GroupName { get; set; }

        /// <summary>
        /// 成员数
        /// </summary>
        [JsonProperty(PropertyName = "member_count")]
        public int MemberCount { get; set; }

        /// <summary>
        /// 最大成员数（群容量）
        /// </summary>
        [JsonProperty(PropertyName = "max_member_count")]
        public int MaxMemberCount { get; set; }
    }
}
