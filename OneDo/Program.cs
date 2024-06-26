﻿// See https://aka.ms/new-console-template for more information
using OneDo.Plugin;
using Spectre.Console;
using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

// 调试代码
//args ="conf user".split(" ");
//args = "ftp put -n test".Split(" ");
//args = "ftp list".Split(" ");
//args = "plugin enable -n FTP".Split(" ");
//args = "shell -n gitlog".Split(" ");
//args = "minio pdf -p C:\\Users\\galens\\Downloads\\DgnEC_CRUD.pdf".Split(' ');
//args = "minio img --clipboard-image".Split(" ");
//args = "shell build".Split(" ");
//args = "wol test --ip 127.0.0.1 --port 9 --mac d85083810F03".Split(" ");
//args = "addin new wpf -n testSln".Split(" ");
//args = "addin init cmd".Split(" ");
//args = "addin ref add -n ec".Split(" ");

// 获取程序执行目录
var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
var pluginPath = Path.Combine(currentDirectory, "plugins");

// 判断目录是否存在，不存在，则无法启动程序
if (!Directory.Exists(pluginPath))
{
    AnsiConsole.MarkupLine("[red]{0} 目录不存在[/]", pluginPath);
    return;
}

// 读取 Plugin 目录下的插件 DLL,以 Plugin.dll 结尾
var allPluginDllFullNames = Directory.GetFiles(pluginPath, "*Plugin.dll", SearchOption.AllDirectories);
var dllNames = allPluginDllFullNames.Select(x => Path.GetFileNameWithoutExtension(x));

// 读取用户配置文件 json 格式
// Environment.SpecialFolder.ApplicationData 对应 linux 中的 ~/.config
var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "onedo", "config.json");
// 如果不存在，则新建一个配置
if (!File.Exists(configPath))
{
    Directory.CreateDirectory(Path.GetDirectoryName(configPath));
    var fs = File.Create(configPath);
    var sw = new StreamWriter(fs);
    sw.WriteLine("{}");
    sw.Close();
    fs.Close();
}
// 读取配置
var sr = new StreamReader(configPath);
var configString = sr.ReadToEnd();
sr.Close();
JsonNode config = null;
try
{
    config = JsonNode.Parse(configString);
}
catch (Exception)
{
    AnsiConsole.MarkupLine($"[red]个人配置格式错误, 请查看文件是否满足 json 格式要求: [/][green]{configPath}[/]");
    return;
}

// 获取禁用的插件（默认都加载）
if (config["disabledPlugins"] != null)
{
    var disabledPluginNames = config["disabledPlugins"].AsArray().Select(x => x.GetValue<string>());
    dllNames = dllNames.Except(disabledPluginNames);
}

var rootCommand = new RootCommand("欢迎使用 OneDo，它将帮助您将复杂的操作简化成一条命令，提升效率");

List<string> allDllNames = null;
AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args)
{
    if (allDllNames == null)
    {
        allDllNames = Directory.GetFiles(currentDirectory, "*.dll", SearchOption.AllDirectories).ToList();
    }

    var dllName = args.Name.Split(',')[0];
    var dllFullName = allDllNames.Where(x => x.EndsWith(dllName + ".dll")).FirstOrDefault();

    return dllFullName == null ? null : Assembly.LoadFile(dllFullName);
}

// 加载插件
foreach (var dllName in dllNames)
{
    var dllFullPath = allPluginDllFullNames.Where(x => x.EndsWith(dllName + ".dll")).FirstOrDefault();
    var dll = Assembly.LoadFrom(dllFullPath);
    var pluginTypes = dll.GetTypes().Where(x => typeof(IPlugin).IsAssignableFrom(x));

    foreach (var pluginType in pluginTypes)
    {
        if (Activator.CreateInstance(pluginType) is not IPlugin plugin)
        {
            AnsiConsole.WriteLine($"[red]插件 {dllName} 未实现 IPlugin 接口[/]");
            continue;
        }
        plugin.RegisterCommand(rootCommand, config);
    }
}

rootCommand.InvokeAsync(args);
