using OneDo.MSAddinCLIPlugin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.MSAddinCLIPlugin.Builders
{
    /// <summary>
    /// 指挥者类
    /// 从外部添加建造者
    /// </summary>
    internal class Director : List<BuilderBase>
    {
        /// <summary>
        /// 开始执行命令
        /// </summary>
        public void Start()
        {
            // 获取项目文件
            var projectFilePaths = Helper.FindProjectFilePaths();
            var context = new BuilderContext(projectFilePaths.FirstOrDefault());

            foreach (var builder in this)
            {
                if (!builder.Build(context)) return;
            }
        }

        #region 帮助方法

        #endregion
    }
}
