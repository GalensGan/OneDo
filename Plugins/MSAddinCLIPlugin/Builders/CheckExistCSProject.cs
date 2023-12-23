using OneDo.MSAddinCLIPlugin.Utils;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.MSAddinCLIPlugin.Builders
{
    /// <summary>
    /// 环境验证器
    /// </summary>
    internal class CheckExistCSProject : BuilderBase
    {
        private bool _allowNotExist;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="allowNotExist"></param>
        public CheckExistCSProject(bool allowNotExist = false)
        {
            _allowNotExist = allowNotExist;
        }

        public override bool Build(BuilderContext context)
        {
            // 获取项目 csproj 文件
            var projectFiles = Helper.FindProjectFilePaths();
            if (projectFiles.Count > 1)
            {
                AnsiConsole.MarkupLine("[red]目录中存在多个项目，请切换到项目目录[/]");
                return false;
            }
            if (!_allowNotExist && projectFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]目录中不存在项目，请切换到项目目录[/]");
                return false;
            }

            return true;
        }
    }
}
