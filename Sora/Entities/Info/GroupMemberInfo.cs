using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration.EventParamsType;

namespace Sora.Entities.Info
{
    /// <summary>
    /// 群成员信息
    /// </summary>
    public struct GroupMemberInfo
    {
        #region 属性

        /// <summary>
        /// 群号
        /// </summary>
        [JsonProperty(PropertyName = "group_id")]
        public long GroupId { get; internal init; }

        /// <summary>
        /// 成员UID
        /// </summary>
        [JsonProperty(PropertyName = "user_id")]
        public long UserId { get; internal init; }

        /// <summary>
        /// 昵称
        /// </summary>
        [JsonProperty(PropertyName = "nickname")]
        public string Nick { get; internal init; }

        /// <summary>
        /// 群名片／备注
        /// </summary>
        [JsonProperty(PropertyName = "card")]
        public string Card { get; internal init; }

        /// <summary>
        /// 性别
        /// </summary>
        [JsonProperty(PropertyName = "sex")]
        public string Sex { get; internal init; }

        /// <summary>
        /// 年龄
        /// </summary>
        [JsonProperty(PropertyName = "age")]
        public int Age { get; internal init; }

        /// <summary>
        /// 地区
        /// </summary>
        [JsonProperty(PropertyName = "area")]
        public string Area { get; internal init; }

        /// <summary>
        /// 加群时间戳
        /// </summary>
        [JsonProperty(PropertyName = "join_time")]
        public int JoinTime { get; internal init; }

        /// <summary>
        /// 最后发言时间戳
        /// </summary>
        [JsonProperty(PropertyName = "last_sent_time")]
        public int LastSentTime { get; internal init; }

        /// <summary>
        /// 成员等级
        /// </summary>
        [JsonProperty(PropertyName = "level")]
        public string Level { get; internal init; }

        /// <summary>
        /// 角色(权限等级)
        /// </summary>
        [JsonConverter(typeof(EnumDescriptionConverter))]
        [JsonProperty(PropertyName = "role")]
        public MemberRoleType Role { get; internal init; }

        /// <summary>
        /// 是否不良记录成员
        /// </summary>
        [JsonProperty(PropertyName = "unfriendly")]
        public bool Unfriendly { get; internal init; }

        /// <summary>
        /// 专属头衔
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Title { get; internal init; }

        /// <summary>
        /// 专属头衔过期时间戳
        /// </summary>
        [JsonProperty(PropertyName = "title_expire_time")]
        public long TitleExpireTime { get; internal init; }

        /// <summary>
        /// 是否允许修改群名片
        /// </summary>
        [JsonProperty(PropertyName = "card_changeable")]
        public bool CardChangeable { get; internal init; }

        #endregion
    }
}