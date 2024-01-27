using OneDo.ThreeDSMaxPlugin.Max.Decoders;
using OneDo.ThreeDSMaxPlugin.Max.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max
{
    public class KnownType
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 存储类型
        /// </summary>
        public StorageType StorageType { get; set; }

        /// <summary>
        /// 解码器
        /// </summary>
        public IDecoder Decoder { get; set; }
    }
}
