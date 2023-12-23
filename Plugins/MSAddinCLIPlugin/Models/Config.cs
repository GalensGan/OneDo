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
        /// 启动资源所在的目录
        /// 仅对新建时有效
        /// </summary>
        [Obsolete("目前该属性未兼容，若启用需要修改 KeyinFunctionsBuilder 和 AddinBuilder 类")]
        public string StarupFolderName { get; set; }

        /// <summary>
        /// Benltey 产品
        /// </summary>
        public List<BentleyProductModel> Products { get; set; } = new List<BentleyProductModel>();

        /// <summary>
        /// 框架版本
        /// </summary>
        public string Framework { get; set; } = "net462";

        /// <summary>
        /// 引用配置
        /// </summary>
        public List<ReferenceModel> References { get; set; } = new List<ReferenceModel>();
    }
}
