using System;

namespace Sora.Module.SoraModel
{
    /// <summary>
    /// 群组类
    /// </summary>
    public sealed class GroupInfo
    {
        #region 属性
        /// <summary>
        /// 服务器链接GUID
        /// 用于构建群组实例
        /// </summary>
        internal Guid ConnectionGuid { get; set; }

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
        /// 群组ID
        /// </summary>
        public long GroupId { get; internal set; }
        #endregion

        #region 公有方法
        /// <summary>
        /// 获取群组实例
        /// </summary>
        public Group GetGroup()
        {
            return new Group(this.ConnectionGuid, GroupId);
        }
        #endregion
    }
}
