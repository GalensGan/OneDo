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
    internal class KeyinFunctionsBuilder : BuilderBase
    {
        public override bool Build(BuilderContext context)
        {
            if (context.CSProjectDocument == null) return false;

            return CopyKeyinFunctions(context.CSProjectDocument, context.CSProjectPath);
        }

        /// <summary>
        /// 复制 keyinFunctions.cs 文件
        /// </summary>
        /// <param name="csproj"></param>
        /// <param name="projectFile"></param>
        /// <returns></returns>
        private bool CopyKeyinFunctions(XDocument csproj, string projectFile)
        {
            // 判断目标目录是否存在，如果已经存在，则不复制
            var projectDir = Path.GetDirectoryName(projectFile);
            var keyinFunctionFileName = "KeyinFunctions.cs";
            var keyinFunctionsPath = Path.Combine(projectDir, "Startup", keyinFunctionFileName);
            if (File.Exists(keyinFunctionsPath))
            {
                AnsiConsole.MarkupLine($"[green]项目中已存在 Startup/{keyinFunctionFileName}[/]");
                return true;
            }

            // 复制 KeyinFunctions.cs 文件
            return Helper.CopyTemplateAndRenameNamespace(csproj, projectFile, keyinFunctionFileName, "Startup");
        }
    }
}
