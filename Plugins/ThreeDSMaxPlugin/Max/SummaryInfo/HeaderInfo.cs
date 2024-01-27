using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.SummaryInfo
{
    public class HeaderInfo
    {
        /// <summary>
        /// key 值
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// 项数量
        /// </summary>
        public List<string> Items { get; set; } = new List<string>();
    }
}
