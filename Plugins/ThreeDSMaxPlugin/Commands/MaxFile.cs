using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Commands
{
    internal class MaxFile
    {
        /// <summary>
        /// max 文件路径
        /// </summary>
        public string MaxPath { get; set; }

        /// <summary>
        /// 缩略图路径
        /// </summary>
        public string ThubnailPath { get; set; }

        /// <summary>
        /// 文件资源：贴图
        /// 全路径
        /// </summary>
        public List<string> FileAssets { get; set; } = new List<string>();

        /// <summary>
        /// 丢失的资源
        /// </summary>
        public List<string> LostAssets { get; set; } = new List<string>();

        /// <summary>
        /// hash 值
        /// </summary>
        public string Hash { get; set; }
    }
}
