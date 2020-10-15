using System.Text.Json.Serialization;

namespace Sora.Plugin
{
    /// <summary>
    /// 插件信息
    /// </summary>
    public class PluginInfo
    {
        /// <summary>
        /// 插件名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 插件入口类
        /// </summary>
        [JsonPropertyName("assembly")]
        public string Assembly { get; set; } = string.Empty;
        /// <summary>
        /// 插件是否启用
        /// </summary>
        [JsonPropertyName("enable")]
        public bool Enable { get; set; } = false;
    }
}
