using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sora.Entities.Base;
using Sora.Entities.Info;

namespace Sora.Entities;

//TODO 完善相关方法
/// <summary>
/// 频道实例
/// </summary>
public class Guild : BaseModel
{
    #region 属性

    /// <summary>
    /// 频道ID
    /// </summary>
    public ulong GuildId { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="serviceId">服务ID</param>
    /// <param name="connectionId">服务器连接标识</param>
    /// <param name="gid">频道ID</param>
    internal Guild(Guid serviceId, Guid connectionId, ulong gid) : base(serviceId, connectionId)
    {
        GuildId = gid;
    }

    #endregion

    #region 管理方法

    /// <summary>
    /// 通过访客获取频道元数据
    /// </summary>
    public async ValueTask<(ApiStatus apiStatus, GuildMetaInfo guildMetaInfo)>
        GetGuildMetaByGuest()
    {
        return await SoraApi.GetGuildMetaByGuest(GuildId);
    }

    /// <summary>
    /// 获取子频道列表
    /// </summary>
    public async ValueTask<(ApiStatus apiStatus, List<ChannelInfo> channelList)>
        GetGuildChannelList()
    {
        return await SoraApi.GetGuildChannelList(GuildId);
    }

    #endregion

    #region 转换方法

    /// <summary>
    /// 定义将 <see cref="User"/> 对象转换为 <see cref="ulong"/>
    /// </summary>
    /// <param name="value">转换的 <see cref="User"/> 对象</param>
    public static implicit operator ulong(Guild value)
    {
        return value.GuildId;
    }

    /// <summary>
    /// 定义将 <see cref="User"/> 对象转换为 <see cref="string"/>
    /// </summary>
    /// <param name="value">转换的 <see cref="User"/> 对象</param>
    public static implicit operator string(Guild value)
    {
        return value.ToString();
    }

    #endregion
}