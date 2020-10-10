using Newtonsoft.Json;

namespace Sora.Model.CQCodes.CQCodeModel
{
    /// <summary>
    /// 接收红包
    /// 仅支持Go
    /// </summary>
    internal struct Redbag
    {
        /// <summary>
        /// 祝福语/口令
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        internal string Title { get; set; }
    }
}
