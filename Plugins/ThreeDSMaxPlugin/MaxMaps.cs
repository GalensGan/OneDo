using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin
{
    internal class MaxMaps
    {
        /// <summary>
        /// max 文件名
        /// </summary>
        public string MaxFileFullName { get; set; }

        /// <summary>
        /// 文件资源：贴图等等
        /// </summary>
        public List<FileInfo> FileAssets { get; set; }
    }
}
