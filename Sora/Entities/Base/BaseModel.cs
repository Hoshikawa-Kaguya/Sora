using System;
using Sora.Net.Records;

namespace Sora.Entities.Base;

/// <summary>
/// 数据模型基类
/// </summary>
public abstract class BaseModel
{
#region 属性

    /// <summary>
    /// API执行实例
    /// </summary>
    // ReSharper disable once MemberCanBeProtected.Global
    public SoraApi SoraApi { get; }

#endregion

#region 构造函数

    internal BaseModel(Guid connectionId)
    {
        SoraApi = ConnectionRecord.GetApi(connectionId);
    }

#endregion
}