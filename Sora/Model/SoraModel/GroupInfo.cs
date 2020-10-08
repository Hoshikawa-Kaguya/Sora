namespace Sora.Model.SoraModel
{
    /// <summary>
    /// 群组类
    /// </summary>
    public class GroupInfo
    {
        /// <summary>
        /// 群名称
        /// </summary>
        public string GroupName { get; internal set; }

        /// <summary>
        /// 成员数
        /// </summary>
        public int MemberCount { get; internal set; }

        /// <summary>
        /// 最大成员数（群容量）
        /// </summary>
        public int MaxMemberCount { get; internal set; }
        /// <summary>
        /// 群组实例
        /// </summary>
        public Group Group { get; internal set; }
    }
}
