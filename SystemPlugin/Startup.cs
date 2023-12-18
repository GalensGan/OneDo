using OneDo.Plugin;
using OneDo.SystemPlugin;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OneDo.SystemPlugin
{
    /// <summary>
    /// 安装与卸载插件
    /// </summary>
    public class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            // 安装
            var installCommand = new Command("install", "安装OneDo,将该程序添加到用户Path变量中");
            rootCommand.Add(installCommand);
            installCommand.SetHandler(() =>
            {
                // 给用户 path 变量中加上当前目录
                string userPath = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.User);

                var baseDir = AppDomain.CurrentDomain.BaseDirectory;

                if (userPath.Contains(baseDir))
                {
                    AnsiConsole.MarkupLine("[yellow]已经安装![/]");
                }
                else
                {
                    AnsiConsole.Status()
                     .Spinner(Spinner.Known.Dots5)
                     .AutoRefresh(true)
                     .Start("开始更新环境变量...", ctx =>
                     {
                         userPath += $";{baseDir}";
                         Environment.SetEnvironmentVariable("path", userPath, EnvironmentVariableTarget.User);
                     });
                    AnsiConsole.MarkupLine("[springgreen1]安装成功[/]");
                }
                AnsiConsole.MarkupLine($"运行下列语句卸载: oneDo uninstall");
            });

            // 卸载
            var uninstallCommand = new Command("uninstall", "卸载OneDo,移除环境变量，删除配置文件");
            rootCommand.Add(uninstallCommand);
            uninstallCommand.SetHandler(() =>
            {
                // 删除用户 path 中添加的变量
                string userPath = Environment.GetEnvironmentVariable("path", EnvironmentVariableTarget.User);
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (userPath.Contains(baseDir))
                {
                    AnsiConsole.MarkupLine("正在清除环境变量...");

                    AnsiConsole.Status()
                     .Spinner(Spinner.Known.Dots5)
                     .AutoRefresh(true)
                    .Start("正在清除环境变量...", ctx =>
                    {
                        userPath = userPath.Replace(";" + baseDir, "");
                        Environment.SetEnvironmentVariable("path", userPath, EnvironmentVariableTarget.User);
                    });

                    AnsiConsole.MarkupLine("[springgreen1]环境变量清除成功![/]");
                }

                // 删除配置文件
                // 询问是否删除配置文件
                var askResult = AnsiConsole.Ask<string>("是否删除个人配置 ([green]Yes/No[/])","No");
                if (!string.IsNullOrEmpty(askResult) && askResult.ToLower().Contains("y"))
                {
                    var configPath = PluginSetting.GetUserConfigPath();
                    var configDir = Path.GetDirectoryName(configPath);
                    if (Directory.Exists(configDir)) Directory.Delete(configDir, true);
                    AnsiConsole.MarkupLine("[springgreen1]个人配置已清除[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]保留个人配置[/]");
                }

                // 判断当前命令行位置
                AnsiConsole.MarkupLine("[springgreen1]卸载成功！运行下列语句彻底删除：[/]");
                AnsiConsole.WriteLine($"cd .. & rmdir /s/q \"{baseDir.Trim('\\')}\"");
            });

            // 打开配置
            var configCommand = new Command("conf", "打开 OneDo 相关的目录");
            rootCommand.Add(configCommand);

            // 打开安装目录
            var appDirCommand = new Command("app", "打开 OneDo 安装目录");
            configCommand.Add(appDirCommand);
            appDirCommand.SetHandler(() =>
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                Process.Start("explorer.exe", baseDir);
            });

            // 打开用户配置文件
            var userConfigCommand = new Command("user", "打开用户配置文件");
            configCommand.Add(userConfigCommand);
            userConfigCommand.SetHandler(() =>
            {
                var configPath = PluginSetting.GetUserConfigPath();
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                };
                Process.Start(psi);
            });            
        }
    }
}
