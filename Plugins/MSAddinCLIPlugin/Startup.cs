using OneDo.MSAddinCLIPlugin.Builders;
using OneDo.MSAddinCLIPlugin.Keyin;
using OneDo.MSAddinCLIPlugin.Models;
using OneDo.MSAddinCLIPlugin.Utils;
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
using System.Xml.Linq;
using System.Xml.Serialization;

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

            var productOption = new Option<string>("--product", "给 Addin 添加引用时，使用的产品名称，比如：ms,ord,obd")
            {
                IsRequired = false
            };
            productOption.AddAlias("-p");
            newCommand.Add(productOption);

            newCommand.SetHandler((projectType, name, productName) =>
            {
                var director = new Director()
                {
                    new NewProjectBuilder(projectType,name),
                    new CheckExistCSProject(),
                    new KeyinCommandsBuilder(),
                    new AddinBuilder(),
                    new KeyinFunctionsBuilder(),
                    new RefAddBuilder("common",productName),
                    new CSProjectSaver()
                };
                director.Start();
            }, projectTypeArg, nameOption, productOption);
            #endregion

            #region 将项目初始化成 addin 类型
            var initCommand = new Command("init", "将当前项目初始化成 addin 项目");
            msAddinCommand.Add(initCommand);
            initCommand.SetHandler(() =>
            {
                var director = new Director()
                {
                    new CheckExistCSProject(),
                    new KeyinCommandsBuilder(),
                    new AddinBuilder(),
                    new KeyinFunctionsBuilder(),
                    new RefAddBuilder("common",string.Empty,true),
                    new CSProjectSaver()
                };
                director.Start();
            });
            #endregion

            #region 命令表相关
            var commandTableCommand = new Command("command", "对当前项目的命令表进行操作");
            commandTableCommand.AddAlias("cmd");
            var defaultKeyinOption = new Option<string>("--keyin", "默认的 keyin 命令");
            commandTableCommand.Add(defaultKeyinOption);
            var defaultFunctionOption = new Option<string>("--function", "默认的 keyin 命令对应的函数");
            commandTableCommand.Add(defaultFunctionOption);
            initCommand.Add(commandTableCommand);
            commandTableCommand.SetHandler((defaultKeyin, defaultFunctionOption) =>
            {
                var director = new Director()
                {
                    new CheckExistCSProject(true),
                    new KeyinCommandsBuilder(defaultKeyin, defaultFunctionOption),
                    new CSProjectSaver()
                };
                director.Start();
            }, defaultKeyinOption, defaultFunctionOption);
            #endregion

            #region 引用相关
            var refCommand = new Command("ref", "对当前项目的引用进行操作");
            msAddinCommand.Add(refCommand);

            // 添加引用
            var addRefCommand = new Command("add", "添加引用");
            refCommand.Add(addRefCommand);
            var refNameOption = new Option<string>("name", "引用名称");
            refNameOption.AddAlias("-n");
            addRefCommand.Add(refNameOption);
            var productNameOption = new Option<string>("--product", "引用对应的产品名称");
            productNameOption.AddAlias("-p");
            addRefCommand.Add(productNameOption);
            addRefCommand.SetHandler((refName, productName) =>
            {
                var director = new Director()
                {
                    new CheckExistCSProject(),
                    new RefAddBuilder(refName, productName),
                    new CSProjectSaver()
                };
                director.Start();
            }, refNameOption, productNameOption);

            // 移除引用
            var removeRefCommand = new Command("remove", "移除引用");
            removeRefCommand.AddAlias("rm");
            removeRefCommand.Add(refNameOption);
            var grepOption = new Option<string>("--grep", "目标引用名称的模糊匹配");
            removeRefCommand.Add(grepOption);

            refCommand.Add(removeRefCommand);
            removeRefCommand.SetHandler((refName, grep) =>
            {
                var director = new Director()
                {
                    new CheckExistCSProject(),
                    new RefRemoveBuilder(refName,grep),
                    new CSProjectSaver()
                };
                director.Start();
            }, refNameOption, grepOption);
            #endregion

            #region 配置相关
            //var configCommand = new Command("refConfig", "addin引用操作");
            //msAddinCommand.Add(configCommand);

            // 添加引用配置
            var addReferenceConfig = new Command("update", "向配置中新增引用");
            refCommand.Add(addReferenceConfig);
            var refUpdateNameOption = new Option<string>("--name", "需要保存的引用名称");
            refUpdateNameOption.AddAlias("-n");
            addReferenceConfig.Add(refUpdateNameOption);
            // 具体的值
            var referencePathsOption = new Option<List<string>>("--path", "引用的路径，可以是文件夹或者文件，多个值用逗号分隔")
            {
                IsRequired = true,
                Arity = ArgumentArity.OneOrMore
            };
            referencePathsOption.AddAlias("-p");
            addReferenceConfig.Add(referencePathsOption);
            addReferenceConfig.SetHandler((names, paths) =>
            {
                // 判断是否存在配置文件
                var configPath = Helper.GetThisConfigPath();
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
                if (referencesNode == null)
                {
                    referencesNode = new JsonArray();
                    rootNode.AsObject().Add("references", referencesNode);
                }

                // 开始添加引用
                foreach (var refPath in paths)
                {
                    if (referencesNode.Contains(refPath)) continue;
                    referencesNode.Add(refPath);
                }

                // 保存配置文件
                File.WriteAllText(configPath, rootNode.ToJsonString());
            }, refUpdateNameOption, referencePathsOption);

            // 列出可用引用
            var listReferenceCommand = new Command("list", "列出可用的引用配置信息");
            listReferenceCommand.AddAlias("ls");
            refCommand.Add(listReferenceCommand);
            listReferenceCommand.SetHandler(() =>
            {
                // 读取配置文件
                var configPath = Helper.GetThisConfigPath();
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
            var detailReferenceCommand = new Command("detail", "查看某个引用配置的详细信息");
            var detailNameOption = new Option<string>("--name", "引用名称");
            detailNameOption.AddAlias("-n");
            detailReferenceCommand.Add(detailNameOption);
            refCommand.Add(detailReferenceCommand);
            detailReferenceCommand.SetHandler(name =>
            {
                // 查找 name 对应的引用
                if (!Helper.GetConfigObject(out var config)) return;
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
                    AnsiConsole.MarkupLine($"[springgreen1]{reference}[/]");
                }
                AnsiConsole.WriteLine();
            }, detailNameOption);
            #endregion
        }
    }
}