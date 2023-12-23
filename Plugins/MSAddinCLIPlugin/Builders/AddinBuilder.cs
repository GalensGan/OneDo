using OneDo.MSAddinCLIPlugin.Utils;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Builders
{
    internal class AddinBuilder : BuilderBase
    {
        public override bool Build(BuilderContext context)
        {
            if (context.CSProjectDocument == null) return false;
            return CopyAddinTemplate(context.CSProjectDocument, context.CSProjectPath);
        }

        /// <summary>
        /// 复制 addin 模板
        /// </summary>
        /// <param name="csproj"></param>
        /// <returns></returns>
        private bool CopyAddinTemplate(XDocument csproj, string projectFile)
        {
            // 获取  addin 文件
            var projectDir = Path.GetDirectoryName(projectFile);
            var csFiles = Directory.GetFiles(projectDir, "*.cs", SearchOption.AllDirectories);
            var addinFile = csFiles.FirstOrDefault(x =>
            {
                // 按行匹配
                return IsInheritedFromAddin(x);
            });

            if (addinFile != null)
            {
                return true;
            }

            return Helper.CopyTemplateAndRenameNamespace(csproj, projectFile, "AppAddin.cs", "Startup");
        }

        /// <summary>
        /// 是否继承  Addin
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool IsInheritedFromAddin(string filePath)
        {
            var allText = File.ReadAllText(filePath);
            return allText.Contains(" : Addin");
        }
    }
}
