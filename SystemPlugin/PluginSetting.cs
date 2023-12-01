using OneDo.Plugin;
using Spectre.Console;
using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace OneDo.SystemPlugin
{
    public class PluginSetting : IPlugin
    {
        public bool RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            // 注册子命令
            var settingCommand = new Command("plugin", "设置插件：启用或禁用插件");
            rootCommand.Add(settingCommand);

            #region 启用插件
            var enableCommand = new Command("enable", "启用插件");
            var pluginNameOption = new Option<string>("--name", "插件名称") { IsRequired = true };
            pluginNameOption.AddAlias("-n");
            enableCommand.Add(pluginNameOption);
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
                    disabledPluginsNode.AsArray().Remove(pluginName);

                    // 重新保存到文件中
                    OverrideConfigFile(config);

                    AnsiConsole.MarkupLine($"[springgreen1]插件: {pluginName} 启用成功[/]");
                }

                AnsiConsole.MarkupLine($"[yellow]插件: {pluginName} 已启用[/]");
            }, pluginNameOption);
            settingCommand.Add(enableCommand);
            #endregion

            #region 禁用插件
            var disableCommand = new Command("disable", "禁用插件");
            disableCommand.AddAlias("d");
            disableCommand.Add(pluginNameOption);
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
                arrayNodes.Remove(pluginName);
                arrayNodes.Add(pluginName);

                // 重新保存到文件中
                OverrideConfigFile(config);

            }, pluginNameOption);
            #endregion

            #region 查看已经安装的插件
            var listInstalledPlugins = new Command("list", "展示已安装插件");
            listInstalledPlugins.AddAlias("ls");
            settingCommand.Add(listInstalledPlugins);
            listInstalledPlugins.SetHandler(() =>
            {
                AnsiConsole.MarkupLine("Installed plugins:");
                AnsiConsole.WriteLine();

                var pluginNames = GetPluginFiles().ConvertAll(x => Path.GetFileNameWithoutExtension(x));

                // 获取禁用的插件列表
                var disabledPluginsNode = config["disabledPlugins"];
                if (disabledPluginsNode != null)
                {
                    var disabledPlugins = disabledPluginsNode.AsArray().Select(x => x.GetValue<string>());
                    pluginNames = pluginNames.Except(disabledPlugins).ToList();
                }

                var grid = new Grid();
                // Add columns 
                grid.AddColumn();
                grid.AddColumn();
                grid.AddColumn();
                // Add header row 
                grid.AddRow(new Text[]{
                    new Text("名称", new Style(Color.SpringGreen1)).LeftJustified(),
                    new Text("版本", new Style(Color.SpringGreen1)).LeftJustified(),
                    new Text("作者", new Style(Color.SpringGreen1)).LeftJustified()
                });
                grid.AddRow(new Text[]{
                    new Text("----", new Style(Color.SpringGreen1)).LeftJustified(),
                    new Text("----", new Style(Color.SpringGreen1)).LeftJustified(),
                    new Text("----", new Style(Color.SpringGreen1)).LeftJustified()
                });

                // 获取程序所有的程序集
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(x => pluginNames.Contains(x.GetName().Name));
                foreach (var assembly in assemblies)
                {
                    // 获取程序的版本与作者信息
                    var assemblyNameInfo = assembly.GetName();                  
                    var author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company;
                    grid.AddRow(assemblyNameInfo.Name, assemblyNameInfo.Version.ToString(), author);
                }

                AnsiConsole.Write(grid);
                AnsiConsole.WriteLine();
            });
            #endregion

            return true;
        }

        private string GetPluginFullName(string pluginName)
        {
            if (pluginName.EndsWith("plugin")) pluginName += "plugin";
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
            var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneDo", "config.json");
            using var fs = File.Create(configPath);
            using var sw = new StreamWriter(fs);
            sw.Write(jsonString);
        }

        private bool ExistPlugin(string pluginName)
        {
            var files = GetPluginFiles();
            if (files.Any(x => Path.GetFileNameWithoutExtension(x) == pluginName))
            {
                return true;
            }

            return false;
        }

        private List<string> GetPluginFiles()
        {
            string executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var files = Directory.GetFiles(executingPath, $"*Plugin.dll", SearchOption.AllDirectories);
            return files.ToList();
        }
    }
}