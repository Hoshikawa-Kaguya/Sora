using Newtonsoft.Json;

namespace Sora.Server.Params.ApiParams
{
    /// <summary>
    /// 获取群成员列表参数
    /// </summary>
    internal struct GetGroupMemberListParams
    {
        /// <summary>
        /// 群号
        /// </summary>
        [JsonProperty(PropertyName = "group_id")]
        internal long Gid { get; set; }
    }
}
