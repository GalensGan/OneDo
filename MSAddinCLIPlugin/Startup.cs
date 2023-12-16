using OneDo.MSAddinCLIPlugin.Models;
using OneDo.Plugin;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneDO.MSAddinCLIPlugin
{
    /// <summary>
    /// Bentley Addin 项目快速创建命令配置
    /// </summary>
    public class Startup : IPlugin
    {
        public bool RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var msAddinCommand = new Command("addin", "快速新建 Bentley Addin 二次开发项目");
            rootCommand.Add(msAddinCommand);

            var newCommand = new Command("new", "新建 addin 解决方案或项目");
            msAddinCommand.Add(newCommand);

            var projectTypeArg = new Argument<string>("ProjectType", "指定项目的类型，可选值: wpf, winform, dll");
            projectTypeArg.AddValidator(result =>
            {
                var value = result.GetValueOrDefault();
                if (value == null)
                {
                    result.ErrorMessage = "请指定项目类型";
                    return;
                }

                var projectType = value.ToString().ToLower();
                List<string> acceptProjectTypes = new List<string>()
                {
                    "wpf","winform","dll"
                };
                if (!acceptProjectTypes.Contains(projectType))
                {
                    result.ErrorMessage = $"项目类型限定为: {string.Join(",", acceptProjectTypes)}";
                    return;
                }
            });
            newCommand.AddArgument(projectTypeArg);

            var nameOption = new Option<string>("--name", "解决方案或项目名称")
            {
                IsRequired = true,
            };
            nameOption.AddAlias("-n");
            newCommand.AddOption(nameOption);

            var productOption = new Option<string>("--product", "Addin 对应的软件名称，默认为 Microstaton")
            {
                IsRequired = true
            };
            productOption.AddAlias("-p");
            productOption.SetDefaultValue("microstation");
            newCommand.Add(productOption);

            newCommand.SetHandler((projectType, name, productName) =>
            {
                if (!ValidateEnv()) return;

                // 判断类型是否正确
                List<string> acceptProjectTypes = new List<string>()
                {
                    "wpf","winform","dll"
                };
                if (!acceptProjectTypes.Contains(projectType))
                {
                    AnsiConsole.MarkupLine($"[red]项目类型限定为: {string.Join(",", acceptProjectTypes)}[/]");
                    return;
                }

                // 读取配置文件
                var currentDir = Path.GetDirectoryName(this.GetType().Assembly.Location);
                var configPath = Path.Combine(currentDir, ".addinPlugin.json");
                var config = JsonSerializer.Deserialize<Config>(File.OpenRead(configPath), new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var product = config.Products.FirstOrDefault(x => x.ProductNames.Contains(productName.ToLower()));
                if (product == null)
                {
                    AnsiConsole.MarkupLine($"[red]未能找到对应的产品: {productName}[/]");
                    return;
                }

                // 使用 dotnet 执行创建解决方案或者项目

                // 将生成类型改成 libary

                // 修改 csproj 文件，添加对应的引用

                // 格式化 keyin

                // 格式化 addin

                // 格式化命令表
            }, projectTypeArg, nameOption, productOption);
            return true;
        }

        /// <summary>
        /// 验证运行所需要的环境：比如配置文件，模板文件等
        /// </summary>
        /// <returns></returns>
        private bool ValidateEnv()
        {
            // 获取当前 dll 所在的目录
            var currentDir = Path.GetDirectoryName(this.GetType().Assembly.Location);
            // 获取配置文件
            var configPath = Path.Combine(currentDir, ".addinPlugin.json");
            if (!File.Exists(configPath))
            {
                AnsiConsole.MarkupLine("[/red]未能在插件的同级目录中找到 .addinPlugin.json 配置文件[/]");
                return false;
            }

            // 判断模板是否存在
            var templateDir = Path.Combine(currentDir, "Templates");
            var templateFiles = new List<string>()
            {
                "AppAddin.cs",
                "commands.xml",
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

        private bool CreateSolutionOrProject(string name)
        {
            // 判断当前执行目录下，是否有相同的名称
        }
    }
}