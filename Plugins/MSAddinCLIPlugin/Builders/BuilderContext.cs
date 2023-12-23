using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Builders
{
    internal class BuilderContext
    {
        public BuilderContext(string? csProjectPath)
        {
            if (string.IsNullOrEmpty(csProjectPath)) return;
            var document = XDocument.Load(csProjectPath);
            SetCSProjectDocument(document, csProjectPath);
        }

        /// <summary>
        /// 更新值
        /// </summary>
        /// <param name="csProjectDocument"></param>
        /// <param name="csProjectPath"></param>
        public void SetCSProjectDocument(XDocument? csProjectDocument, string csProjectPath)
        {
            CSProjectPath = csProjectPath.Trim();
            CSProjectDocument = csProjectDocument;
        }

        /// <summary>
        /// 项目配置 xml 对象
        /// </summary>
        public XDocument? CSProjectDocument { get; private set; }

        /// <summary>
        /// 项目配置文件全名
        /// </summary>
        public string? CSProjectPath { get; private set; }

        /// <summary>
        /// 项目配置文件所在目录
        /// </summary>
        public string? CSProjectDir => Path.GetDirectoryName(CSProjectPath);
    }
}
