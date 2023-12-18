using OneDo.MSAddinCLIPlugin.Models;
using OneDo.Plugin;
using OneDo.Utils;
using Spectre.Console;
using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;

namespace OneDO.MSAddinCLIPlugin
{
    /// <summary>
    /// Bentley Addin 项目快速创建命令配置
    /// </summary>
    public class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var msAddinCommand = new Command("addin", "快速新建 Bentley Addin 二次开发项目");
            rootCommand.Add(msAddinCommand);

            #region 新建项目
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
                    "wpf","winform","classlib"
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
                    "wpf","winform","classlib"
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
                if (!CreateSolutionOrProject(name, projectType, config.Framework, out string projectFilePath)) return;

                // 修改 csproj 文件
                XmlDocument csprojXmlDocument = new XmlDocument();
                csprojXmlDocument.Load(File.OpenRead(projectFilePath));

                // 1.将生成类型改成 libary
                if (!ChangeOutputTypeToLibary(csprojXmlDocument)) return;

                // 将平台改成 x64
                if (!ChangePlatformToX64(csprojXmlDocument)) return;

                // 添加 Dll 引用

                // 格式化 keyin

                // 格式化 addin

                // 格式化命令表
            }, projectTypeArg, nameOption, productOption);
            #endregion

            #region 将项目初始化成 addin 类型
            var initCommand = new Command("init", "将当前项目初始化成 addin 项目");
            msAddinCommand.Add(initCommand);
            initCommand.SetHandler(() =>
            {

            });
            #endregion

            #region 添加引用
            var referenceCommand = new Command("reference", "addin引用操作");
            referenceCommand.AddAlias("ref");
            msAddinCommand.Add(referenceCommand);

            // 添加引用
            var addReferenceCommand = new Command("add", "添加引用");
            referenceCommand.Add(addReferenceCommand);
            var namesArg = new Argument<string>("name", "需要保存的引用名称");
            addReferenceCommand.Add(namesArg);
            // 具体的值
            var referencePathsOption = new Option<List<string>>("--path", "引用的路径，可以是文件夹或者文件，多个值用逗号分隔")
            {
                IsRequired = true,
                Arity = ArgumentArity.OneOrMore
            };
            referencePathsOption.AddAlias("-p");
            addReferenceCommand.Add(referencePathsOption);
            addReferenceCommand.SetHandler((names, paths) =>
            {
                // 判断是否存在配置文件
                var configPath = GetThisConfigPath();
                if (!File.Exists(configPath))
                {
                    AnsiConsole.MarkupLine("[red]未找到配置文件: .addinPlugin.json[/]");
                    return;
                }

                // 读取配置文件为 json，并向 json 中添加 references 节点
                var rootNode = JsonNode.Parse(File.OpenRead(configPath));
                if (rootNode == null)
                {
                    AnsiConsole.MarkupLine("[red]配置文件不是有效的 json 格式[/]");
                    return;
                }
                var referencesNode = rootNode["references"] as JsonArray;
                if(referencesNode == null)
                {
                    referencesNode = new JsonArray();
                    rootNode.AsObject().Add("references", referencesNode);
                }

                // 开始添加引用
                foreach(var refPath in paths)
                {
                    if (referencesNode.Contains(refPath)) continue;
                    referencesNode.Add(refPath);
                }

                // 保存配置文件
                File.WriteAllText(configPath, rootNode.ToJsonString());
            }, namesArg, referencePathsOption);

            // 列出可用引用
            var listReferenceCommand = new Command("list", "列出可用的引用配置");
            listReferenceCommand.AddAlias("ls");
            referenceCommand.Add(listReferenceCommand);
            listReferenceCommand.SetHandler(() =>
            {
                // 读取配置文件
                var configPath = GetThisConfigPath();
                if (!File.Exists(configPath))
                {
                    AnsiConsole.MarkupLine("[red]未找到配置文件: .addinPlugin.json[/]");
                    return;
                }

                // 读取配置文件为 json
                var rootNode = JsonNode.Parse(File.OpenRead(configPath));
                var list = new ListPluginConfs(rootNode, "references", new List<FieldMapper>()
                {
                    new FieldMapper("name","名称"),
                    new FieldMapper("description","描述"),
                    new FieldMapper("references", "引用数量")
                    {
                        Formatter = (value) =>
                        {
                            var references = value as JsonArray;
                            if(references ==null)return "0";
                            return references.Count.ToString();
                        }
                    }
                });
                list.Show();
            });

            // 查看某个详细引用
            var detailReferenceCommand = new Command("detail", "查看某个引用的详细信息");
            referenceCommand.Add(detailReferenceCommand);
            var nameArg = new Argument<string>("name", "引用名称");
            detailReferenceCommand.SetHandler(name =>
            {
                // 查找 name 对应的引用
                var configPath = GetThisConfigPath();
                var config = JsonSerializer.Deserialize<Config>(File.OpenRead(configPath), new JsonSerializerOptions()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                AnsiConsole.WriteLine();

                var referenceModel = config.References.Find(x => x.Name.ToLower() == name.ToLower());
                if (referenceModel == null)
                {
                    AnsiConsole.MarkupLine($"[red]未找到引用: {name}[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"{name} references:");
                AnsiConsole.WriteLine();
                foreach (var reference in referenceModel.References)
                {
                    AnsiConsole.MarkupLine($"[green]{reference}[/]");
                }
                AnsiConsole.WriteLine();
            }, nameArg);
            #endregion
        }

        /// <summary>
        /// 是否有 dotnet 环境
        /// </summary>
        /// <returns></returns>
        private bool IsDotnetEnv()
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
        /// 验证运行所需要的环境：比如配置文件，模板文件等
        /// </summary>
        /// <returns></returns>
        private bool ValidateEnv()
        {
            // 验证是否有 dotnet 环境
            if (!IsDotnetEnv())
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

        /// <summary>
        /// 获取配置文件路径
        /// </summary>
        /// <returns></returns>
        private string _configPath = string.Empty;
        private string GetThisConfigPath()
        {
            if (string.IsNullOrEmpty(_configPath))
            {
                var location = this.GetType().Assembly.Location;
                var files = Directory.GetFiles(location, ".addinPlugin.json");
                return files.FirstOrDefault();
            }

            return _configPath;
        }

        /// <summary>
        /// 创建解决方案或者项目
        /// 若目标目录中，没有找到解决方案，则先新建解决方案
        /// 若有解决方案，则新增项目
        /// </summary>
        /// <param name="name"></param>
        /// <param name="projectType"></param>
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
        private bool ChangeOutputTypeToLibary(XmlDocument csprojRootDocument)
        {
            // 找到 Project/PropertyGroup/OutputType，修改成 Libary
            var outputTypeNodes = csprojRootDocument.SelectNodes("/Project/PropertyGroup/OutputType");
            foreach (var outputTypeNode in outputTypeNodes)
            {
                if (outputTypeNode is not XmlElement outputTypeElement) continue;
                outputTypeElement.InnerText = "Library";
            }
            return true;
        }

        /// <summary>
        /// 切换成 x64 平台
        /// </summary>
        /// <param name="csprojRootDocument"></param>
        /// <returns></returns>
        private bool ChangePlatformToX64(XmlDocument csprojRootDocument)
        {
            // 找到 Project/PropertyGroup/PlatformTarget，修改成 x64
            var platformTargetNodes = csprojRootDocument.SelectNodes("/Project/PropertyGroup/PlatformTarget");
            foreach (var platformTargetNode in platformTargetNodes)
            {
                if (platformTargetNode is not XmlElement platformTargetElement) continue;
                platformTargetElement.InnerText = "x64";
            }

            return true;
        }
    }
}