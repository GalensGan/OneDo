using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneDo.WakeOnLanPlugin
{
    /// <summary>
    /// 局域网唤醒数据类
    /// </summary>
    internal class WolModel
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// IP地址
        /// </summary>
        [JsonPropertyName("ip")]
        public string IP { get; set; }
        /// <summary>
        /// MAC 地址
        /// </summary>
        [JsonPropertyName("mac")]
        public string MAC { get; set; }
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 9;
    }
}
