using Newtonsoft.Json;

namespace Sora.Entities.Info;

/// <summary>
/// 型号信息
/// </summary>
public struct Model
{
    /// <summary>
    /// 型号
    /// </summary>
    [JsonProperty(PropertyName = "model_show")]
    public string ModelString { get; internal set; }

    /// <summary>
    /// 需要会员
    /// </summary>
    [JsonProperty(PropertyName = "need_pay")]
    public bool NeedVip { get; internal set; }
}