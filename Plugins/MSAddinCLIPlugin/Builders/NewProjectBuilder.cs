using OneDo.MSAddinCLIPlugin.Models;
using OneDo.MSAddinCLIPlugin.Utils;
using OneDO.MSAddinCLIPlugin;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Builders
{
    internal class NewProjectBuilder : BuilderBase
    {
        private string _projectType;
        private string _name;
        public NewProjectBuilder(string projectType, string name)
        {
            _projectType = projectType;
            _name = name;
        }

        public override bool Build(BuilderContext context)
        {
            if (!ValidateRuntimeEnv()) return false;

            // 判断类型是否正确
            List<string> acceptProjectTypes = new List<string>()
                {
                    "wpf","winform","classlib"
                };
            if (!acceptProjectTypes.Contains(_projectType))
            {
                AnsiConsole.MarkupLine($"[red]项目类型限定为: {string.Join(",", acceptProjectTypes)}[/]");
                return false;
            }

            // 读取配置文件
            var currentDir = Path.GetDirectoryName(this.GetType().Assembly.Location);
            var configPath = Path.Combine(currentDir, ".addinPlugin.json");
            var config = JsonSerializer.Deserialize<Config>(File.OpenRead(configPath), new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            // 使用 dotnet 执行创建解决方案或者项目
            if (!CreateSolutionOrProject(_name, _projectType, config.Framework, out string projectFilePath)) return false;

            // 修改 csproj 文件
            var csprojXDocument = XDocument.Load(projectFilePath);

            // 1.将生成类型改成 libary
            if (!ChangeOutputTypeToLibary(csprojXDocument)) return false;

            // 将平台改成 x64
            if (!ChangePlatformToX64(csprojXDocument)) return false;
            csprojXDocument.Save(projectFilePath);

            // 修改环境当前目录位置
            var projectDir = Path.GetDirectoryName(projectFilePath);
            Environment.CurrentDirectory = projectDir;

            // 更新上下文
            context.SetCSProjectDocument(csprojXDocument, projectFilePath);
            return true;
        }

        /// <summary>
        /// 验证运行所需要的环境：比如配置文件，模板文件等
        /// </summary>
        /// <returns></returns>
        private bool ValidateRuntimeEnv()
        {
            // 验证是否有 dotnet 环境
            if (!HasDotnetEnv())
            {
                AnsiConsole.MarkupLine("[red]未能找到 dotnet 环境[/]");
                return false;
            }

            // 获取当前 dll 所在的目录
            var currentDir = Path.GetDirectoryName(this.GetType().Assembly.Location);
            // 获取配置文件
            var configPath = Path.Combine(currentDir, ".addinPlugin.json");
            if (!File.Exists(configPath))
            {
                AnsiConsole.MarkupLine("[red]未能在插件的同级目录中找到 .addinPlugin.json 配置文件[/]");
                return false;
            }

            // 判断模板是否存在
            var templateDir = Path.Combine(currentDir, "Templates");
            var templateFiles = new List<string>()
            {
                "AppAddin.cs",
                "KeyinFunctions.cs"
            }.ConvertAll(x =>
            {
                return Path.Combine(templateDir, x);
            });
            foreach (var templateFile in templateFiles)
            {
                if (!File.Exists(templateFile))
                {
                    AnsiConsole.MarkupLine($"[red]模板文件 {Path.GetFileName(templateFile)} 丢失[/]");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 是否有 dotnet 环境
        /// </summary>
        /// <returns></returns>
        public static bool HasDotnetEnv()
        {
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (Version.TryParse(output, out var version)) return true;
            return false;
        }

        /// <summary>
        /// 创建解决方案或者项目
        /// 若目标目录中，没有找到解决方案，则先新建解决方案
        /// 若有解决方案，则新增项目
        /// </summary>
        /// <param name="name"></param>
        /// <param name="_projectType"></param>
        /// <param name="framework"></param>
        /// <returns></returns>
        private bool CreateSolutionOrProject(string name, string projectType, string framework, out string projectFilePath)
        {
            projectFilePath = string.Empty;

            // 获取当前执行目录
            var currentWorkingDir = Environment.CurrentDirectory;
            // 判断当前执行目录下，是否有相同的名称
            var slnPath = Path.Combine(currentWorkingDir, name);
            if (!Directory.Exists(slnPath))
            {
                // 目录不存在时，创建
                Directory.CreateDirectory(slnPath);
            }

            // 判断当前目录是否存在 .sln 文件，若存在，表示当前目录是一个解决方案，name 为项目名称
            var slnFiles = Directory.GetFiles(slnPath, "*.sln");
            if (slnFiles.Length > 1)
            {
                AnsiConsole.MarkupLine($"[red]当前目录下存在多个解决方案，无法生成项目[/]");
                return false;
            }

            bool shouldCreateSolution = slnFiles.Length == 0;
            var procss = new RedirectedProcess();
            if (shouldCreateSolution)
            {
                // 先创建解决方案
                procss.Start(slnPath, "dotnet.exe", $"new sln -n {name}");
            }
            else
            {
                slnPath = Path.GetDirectoryName(slnFiles[0]);
            }

            // 新建项目
            procss.Start(slnPath, "dotnet.exe", $"new {projectType} -n {name}  --target-framework-override {framework}");
            // 将项目添加到解决方案中
            procss.Start(slnPath, "dotnet.exe", $"sln add {name}/{name}.csproj");
            procss.Close();

            // 返回项目路径
            projectFilePath = Path.Combine(slnPath, name, $"{name}.csproj");
            return true;
        }

        /// <summary>
        /// 修改 csproj 文件中的输出类型为 libary
        /// </summary>
        /// <param name="csprojRootDocument"></param>
        /// <returns></returns>
        private bool ChangeOutputTypeToLibary(XDocument csprojRootDocument)
        {
            // 找到 Project/PropertyGroup/OutputType，修改成 Libary
            var outputTypes = csprojRootDocument.Root.DescendantsWithNSP("OutputType");
            foreach (var outputType in outputTypes)
            {
                outputType.Value = "Library";
            }
            return true;
        }

        /// <summary>
        /// 切换成 x64 平台
        /// </summary>
        /// <param name="csprojRootDocument"></param>
        /// <returns></returns>
        private bool ChangePlatformToX64(XDocument csprojRootDocument)
        {
            // 找到 Project/PropertyGroup/PlatformTarget，修改成 x64
            var platformTargets = csprojRootDocument.Root.DescendantsWithNSP("PlatformTarget");
            foreach (var platformTarget in platformTargets)
            {
                platformTarget.Value = "x64";
            }

            return true;
        }
    }
}
