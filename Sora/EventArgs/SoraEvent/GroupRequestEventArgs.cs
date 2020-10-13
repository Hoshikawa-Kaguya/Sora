using System;
using System.Threading.Tasks;
using Sora.Enumeration.ApiEnum;
using Sora.EventArgs.OnebotEvent.RequestEvent;
using Sora.Module;

namespace Sora.EventArgs.SoraEvent
{
    public sealed class GroupRequestEventArgs : BaseSoraEventArgs
    {
        #region 属性
        /// <summary>
        /// 请求发送者实例
        /// </summary>
        public User Sender { get; private set; }

        /// <summary>
        /// 请求来源群组实例
        /// </summary>
        public Group SourceGroup { get; private set; }

        /// <summary>
        /// 验证信息
        /// </summary>
        public string Comment { get; private set; }

        /// <summary>
        /// 当前请求的flag标识
        /// </summary>
        public string RequsetFlag { get; private set; }

        /// <summary>
        /// 请求子类型
        /// </summary>
        public GroupRequestType SubType { get; private set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="connectionGuid">服务器链接标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="groupRequest">服务器请求事件参数</param>
        internal GroupRequestEventArgs(Guid connectionGuid, string eventName, ApiGroupRequestEventArgs groupRequest) :
            base(connectionGuid, eventName, groupRequest.SelfID, groupRequest.Time)
        {
            this.Sender      = new User(connectionGuid, groupRequest.UserId);
            this.SourceGroup = new Group(connectionGuid, groupRequest.GroupId);
            this.Comment     = groupRequest.Comment;
            this.RequsetFlag = groupRequest.Flag;
            this.SubType     = groupRequest.GroupRequestType;
        }
        #endregion

        #region 公有方法
        /// <summary>
        /// 同意当前申请
        /// </summary>
        public async ValueTask Accept()
        {
            await base.SoraApi.SetGroupAddRequest(this.RequsetFlag, this.SubType, true);
        }

        /// <summary>
        /// 拒绝当前申请
        /// </summary>
        /// <param name="reason">原因</param>
        public async ValueTask Reject(string reason = null)
        {
            await base.SoraApi.SetGroupAddRequest(this.RequsetFlag, this.SubType, false, reason);
        }
        #endregion
    }
}
