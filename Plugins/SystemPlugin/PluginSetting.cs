﻿using OneDo.Plugin;
using OneDo.Utils;
using Spectre.Console;
using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneDo.SystemPlugin
{
    public class PluginSetting : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            // 注册子命令
            var settingCommand = new Command("plugin", "设置插件：启用或禁用插件");
            rootCommand.Add(settingCommand);

            #region 启用插件
            var enableCommand = new Command("enable", "启用插件");
            var pluginNameArgs = new Argument<string>("pluginName", "插件名称");
            enableCommand.Add(pluginNameArgs);
            enableCommand.SetHandler(pluginName =>
            {
                // 插件名称不区分大小写
                pluginName = GetPluginFullName(pluginName);

                // 判断插件文件是否存在
                if (!ExistPlugin(pluginName))
                {
                    AnsiConsole.MarkupLine($"[red]未找到插件:{pluginName}[/]");
                    return;
                }


                // 从配置中读取 disabledPlugins
                var disabledPluginsNode = config["disabledPlugins"];
                if (disabledPluginsNode != null)
                {
                    // 找到了之后，移除指定名称
                    var disabledPlugins = disabledPluginsNode.AsArray();
                    var enableNodes = disabledPlugins.Where(x => pluginName.Equals(x.GetValue<string>(),StringComparison.CurrentCultureIgnoreCase)).ToList();
                    foreach (var node in enableNodes) disabledPlugins.Remove(node);

                    // 重新保存到文件中
                    OverrideConfigFile(config);

                    AnsiConsole.MarkupLine($"[springgreen1]插件 {pluginName} 启用成功！[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[springgreen1]插件 {pluginName} 已启用[/]");
            }, pluginNameArgs);
            settingCommand.Add(enableCommand);
            #endregion

            #region 禁用插件
            var disableCommand = new Command("disable", "禁用插件")
            {
                pluginNameArgs
            };
            settingCommand.Add(disableCommand);
            disableCommand.SetHandler(pluginName =>
            {
                // 插件名称不区分大小写
                pluginName = GetPluginFullName(pluginName);

                // 不允许卸载当前插件
                if (pluginName.ToLower() == "systemplugin")
                {
                    AnsiConsole.MarkupLine($"[red]不允许禁用 {pluginName}[/]");
                    return;
                }

                // 判断插件文件是否存在
                if (!ExistPlugin(pluginName))
                {
                    AnsiConsole.MarkupLine($"[red]未找到插件:{pluginName}[/]");
                    return;
                }

                var disabledPluginsNode = config["disabledPlugins"];
                if (disabledPluginsNode == null)
                {
                    // 添加节点
                    config["disabledPlugins"] = new JsonArray();
                    disabledPluginsNode = config["disabledPlugins"];
                }

                var arrayNodes = disabledPluginsNode.AsArray();
                var usefullNodes = arrayNodes.Select(x => x.GetValue<string>() != pluginName);
                arrayNodes.Clear();
                foreach (var node in usefullNodes) arrayNodes.Add(node);

                arrayNodes.Add(pluginName);

                // 重新保存到文件中
                OverrideConfigFile(config);
                AnsiConsole.MarkupLine($"[springgreen1]插件 {pluginName} 禁用成功！[/]");

            }, pluginNameArgs);
            #endregion

            #region 查看已经安装的插件
            var listInstalledPlugins = new Command("list", "展示已安装插件");
            listInstalledPlugins.AddAlias("ls");
            settingCommand.Add(listInstalledPlugins);
            listInstalledPlugins.SetHandler(() =>
            {
                AnsiConsole.MarkupLine("Installed plugins:");
                AnsiConsole.WriteLine();

                var allPluginFiles = GetPluginFiles();
                var pluginNames = allPluginFiles.ConvertAll(x => Path.GetFileNameWithoutExtension(x));

                // 获取禁用的插件列表
                var disabledPluginsNode = config["disabledPlugins"];
                if (disabledPluginsNode != null)
                {
                    var disabledPlugins = disabledPluginsNode.AsArray().Select(x => x.GetValue<string>());
                    pluginNames = pluginNames.Except(disabledPlugins).ToList();
                }
                // 将名称升序排列
                allPluginFiles.Sort();

                // 获取程序所有的程序集
                var assemblies = allPluginFiles.ConvertAll(x =>
                {
                    // 从单个文件加载程序集
                    return Assembly.LoadFile(x);
                });
                JsonArray jsonDatas = new JsonArray();
                foreach (var assembly in assemblies)
                {
                    // 获取程序的版本与作者信息
                    var assemblyNameInfo = assembly.GetName();
                    var author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
                    jsonDatas.Add(new JsonObject()
                    {
                        { "name", assemblyNameInfo.Name },
                        { "version" , assemblyNameInfo.Version.ToString()},
                        { "author",author},
                        {"status", pluginNames.Contains(assemblyNameInfo.Name) ? "启用" : "禁用"}
                    });
                }

                var list = new ListPluginConfs(jsonDatas, new List<FieldMapper>()
                {
                    new("name","名称"),
                    new("version","版本号"),
                    new("author","作者"),
                    new("status", "状态")
                    {
                        StyleFormatter = value =>
                        {
                            if(value=="启用")return new Style(Color.SpringGreen1);
                            return new Style(Color.Red);
                        }
                    }
                });
                list.Show();

                AnsiConsole.WriteLine();
            });
            #endregion
        }

        /// <summary>
        /// 获取用户配置文件路径
        /// </summary>
        /// <returns></returns>
        internal static string GetUserConfigPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneDo", "config.json");
        }

        private string GetPluginFullName(string pluginName)
        {
            if (!pluginName.EndsWith("Plugin")) pluginName += "Plugin";
            return pluginName;
        }

        private void OverrideConfigFile(JsonNode configNode)
        {
            // 将 jsonNode 转换为 json 字符串
            var jsonString = JsonSerializer.Serialize(configNode, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            // 重新保存到文件中
            var configPath = GetUserConfigPath();
            using var fs = File.Create(configPath);
            using var sw = new StreamWriter(fs);
            sw.Write(jsonString);
        }

        private bool ExistPlugin(string pluginName)
        {
            var files = GetPluginFiles();
            var names = files.ConvertAll(x => Path.GetFileNameWithoutExtension(x));
            if (files.Any(x => Path.GetFileNameWithoutExtension(x).Equals(pluginName, StringComparison.CurrentCultureIgnoreCase)))
            {
                return true;
            }

            return false;
        }

        private List<string> GetPluginFiles()
        {
            string executingPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var files = Directory.GetFiles(executingPath, $"*Plugin.dll", SearchOption.AllDirectories);
            return files.ToList();
        }
    }
}