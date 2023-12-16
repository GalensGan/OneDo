using OneDO.MSAddinCLIPlugin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.MSAddinCLIPlugin.Models
{
    internal class Config
    {
        /// <summary>
        /// Benltey 产品
        /// </summary>
        public List<BentleyProductModel> Products { get; set; } = new List<BentleyProductModel>();

        /// <summary>
        /// 引用配置
        /// </summary>
        public List<ReferenceModel> References { get; set; } = new List<ReferenceModel>();
    }
}
