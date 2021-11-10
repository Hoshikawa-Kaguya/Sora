using System;
using Newtonsoft.Json;
using Sora.Converter;
using Sora.Enumeration.ApiType;

namespace Sora.OnebotModel.ApiParams;

/// <summary>
/// API请求类
/// </summary>
internal sealed class ApiRequest
{
    /// <summary>
    /// API请求类型
    /// </summary>
    [JsonProperty(PropertyName = "action")]
    [JsonConverter(typeof(EnumDescriptionConverter))]
    internal ApiRequestType ApiRequestType { get; init; }

    /// <summary>
    /// 请求标识符
    /// 会自动生成初始值不需要设置
    /// </summary>
    [JsonProperty(PropertyName = "echo")]
    internal Guid Echo { get; } = Guid.NewGuid();

    /// <summary>
    /// API参数对象
    /// 不需要参数时不需要设置
    /// </summary>
    [JsonProperty(PropertyName = "params")]
    internal dynamic ApiParams { get; init; } = new { };
}