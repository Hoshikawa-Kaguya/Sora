using Sora.Enumeration.ApiEnum;
using Sora.Model.CQCodes.CQCodeModel;

namespace Sora.Model.OnebotApi
{
    /// <summary>
    /// API返回集
    /// 用于存放API的返回值
    /// </summary>
    internal class ApiResponseCollection
    {
        /// <summary>
        /// API调用状态
        /// </summary>
        internal string Status { get; set; } = "failed";

        /// <summary>
        /// 返回值
        /// </summary>
        internal int RetCode { get; set; } = -1;

        /// <summary>
        /// cqhttp客户端类型
        /// </summary>
        internal ClientType Client { get; set; } = ClientType.Other;

        /// <summary>
        /// cqhttp客户端版本
        /// </summary>
        internal string ClientVer { get; set; } = "Unknown";

        /// <summary>
        /// 用户ID（qq）
        /// </summary>
        internal long Uid { get; set; } = -1;

        /// <summary>
        /// 用户昵称
        /// </summary>
        internal string Nick { get; set; } = null;

        /// <summary>
        /// 消息id
        /// </summary>
        internal int MessageId { get; set; } = -1;

        /// <summary>
        /// 消息列表
        /// </summary>
        internal NodeArray NodeMessages { get; set; } = new NodeArray();
    }
}
