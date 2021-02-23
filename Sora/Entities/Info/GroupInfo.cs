namespace Sora.Entities.Info
{
    /// <summary>
    /// 群信息
    /// </summary>
    public struct GroupInfo
    {
        #region 属性

        /// <summary>
        /// 群名称
        /// </summary>
        public string GroupName { get; internal init; }

        /// <summary>
        /// 成员数
        /// </summary>
        public int MemberCount { get; internal init; }

        /// <summary>
        /// 最大成员数（群容量）
        /// </summary>
        public int MaxMemberCount { get; internal init; }

        /// <summary>
        /// 群组ID
        /// </summary>
        public long GroupId { get; internal init; }

        #endregion
    }
}