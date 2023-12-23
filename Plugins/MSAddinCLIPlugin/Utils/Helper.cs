using OneDo.MSAddinCLIPlugin.Keyin;
using OneDo.MSAddinCLIPlugin.Models;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Utils
{
    internal class Helper
    {
        /// <summary>
        /// 查找当前环境目录下的项目文件
        /// </summary>
        /// <returns></returns>
        public static List<string> FindProjectFilePaths()
        {
            var currentDir = Environment.CurrentDirectory;
            var projectFiles = Directory.GetFiles(currentDir, "*.csproj", SearchOption.AllDirectories);
            return projectFiles.ToList();
        }

        /// <summary>
        /// 获取里面的命令表
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public static bool FindCommandTables(out List<XDocument> docuemtns, out List<string> paths)
        {
            docuemtns = new List<XDocument>();
            paths = new List<string>();

            var currentDir = Environment.CurrentDirectory;
            var commandTableXMLs = Directory.GetFiles(currentDir, "*.xml", SearchOption.AllDirectories);
            if (commandTableXMLs.Length == 0)
            {
                return false;
            }
            var files = commandTableXMLs.ToList();
            // 判断是否是命令表
            foreach (var file in files)
            {
                var doc = XDocument.Load(file);
                var root = doc.Root;
                if (root.Name.LocalName == KeyinLabels.KeyinTree)
                {
                    docuemtns.Add(doc);
                    paths.Add(file);
                }
            }
            return docuemtns.Count > 0;
        }

        /// <summary>
        /// 获取配置文件
        /// </summary>
        /// <returns></returns>
        public static string GetThisConfigPath()
        {
            var location = typeof(Helper).Assembly.Location;
            var files = Directory.GetFiles(Path.GetDirectoryName(location), ".addinPlugin.json");
            return files.FirstOrDefault();
        }

        /// <summary>
        /// 获取配置对象
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static bool GetConfigObject(out Config config)
        {
            config = null;
            var configPath = GetThisConfigPath();
            if (string.IsNullOrEmpty(configPath))
            {
                AnsiConsole.MarkupLine("[red]未能找到配置文件[/]");
                return false;
            }

            config = JsonSerializer.Deserialize<Config>(File.OpenRead(configPath), new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (config == null)
            {
                AnsiConsole.MarkupLine("[red]配置文件不是有效的 json 格式或格式错误[/]");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 复制模板并修改命名空间
        /// </summary>
        /// <param name="csproj"></param>
        /// <param name="projectFile"></param>
        /// <param name="templateName"></param>
        /// <returns></returns>
        public static bool CopyTemplateAndRenameNamespace(XDocument csproj, string projectFile, string templateName, string startupName)
        {
            // 复制当前程序集目录下的 AppAddin.cs 文件作为模板
            var assemblyDirr = Path.GetDirectoryName(typeof(Helper).Assembly.Location);
            var appAddinFile = Directory.GetFiles(assemblyDirr, templateName, SearchOption.AllDirectories).FirstOrDefault();
            if (appAddinFile == null)
            {
                AnsiConsole.MarkupLine($"[red]未能找到 {templateName} 模板文件[/]");
                return false;
            }

            // 读取文件内容
            var csContent = File.ReadAllText(appAddinFile);
            // 修改命令空间
            var root = csproj.Root;
            var ns = root.DescendantsWithNSP("RootNamespace").FirstOrDefault();
            string rootNamespace = ns?.Value ?? string.Empty;
            if (string.IsNullOrEmpty(rootNamespace))
            {
                // 使用文件名作为命名空间
                rootNamespace = Path.GetFileNameWithoutExtension(projectFile);
            }
            else
            {
                // 将 $(MSBuildProjectName.Replace(" ", "_")) 替换成项目名
                var variable1 = "$(MSBuildProjectName.Replace(\" \", \"_\"))";
                var variable2 = "$(MSBuildProjectName)";
                var projectName = Path.GetFileNameWithoutExtension(projectFile);
                rootNamespace = rootNamespace.Replace(variable1, projectName).Replace(variable2, projectName);
            }

            // 替换命名空间
            csContent = csContent.Replace("namespace AddinNamespace", $"namespace {rootNamespace}.{startupName}");
            // 替换 TaskId
            csContent = csContent.Replace("__MDLTaskId__", Path.GetFileNameWithoutExtension(projectFile));

            // 保存到 Startup 目录中
            var startupDir = Path.Combine(Path.GetDirectoryName(projectFile), startupName);
            Directory.CreateDirectory(startupDir);
            var addinFilePath = Path.Combine(startupDir, templateName);
            File.WriteAllText(addinFilePath, csContent);

            // 若使用的是旧版本的 csproj 文件，需要将 AppAddin.cs 添加到项目中
            var sdkAtrr = csproj.Root.Attribute("Sdk");
            if(sdkAtrr == null)
            {
                // 添加到项目中
                var compileElement = csproj.Root.DescendantsWithNSP("Compile").FirstOrDefault();
                var itemGroup = compileElement?.Parent;
                if (itemGroup == null)
                {
                    // 添加一个 itemGroup
                    itemGroup = new XElement("ItemGroup".ToXNameWithNSP(root));
                    root.Add(itemGroup);
                }

                var compile = new XElement("Compile".ToXNameWithNSP(root),new XAttribute("Include", $"{startupName}\\{templateName}"));                
                itemGroup.Add(compile);               
            }

            AnsiConsole.MarkupLine($"[springgreen1]已添加模板 {startupName}\\{templateName}[/]");

            return true;
        }
    }
}
