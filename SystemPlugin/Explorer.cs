using OneDo.Plugin;
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
using System.Xml.Linq;

namespace OneDo.SystemPlugin
{
    /// <summary>
    /// 打开当前目录或者文件
    /// </summary>
    public class Explorer : IPlugin
    {
        public bool RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var openCommand = new Command("open", "打开当前目录或者文件所在的目录");
            rootCommand.AddCommand(openCommand);

            var pathOption = new Option<string>("--path", "要打开的文件或者目录路径");
            pathOption.AddAlias("-p");
            pathOption.IsRequired = false;
            openCommand.AddOption(pathOption);
            openCommand.SetHandler(path =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    path = Environment.CurrentDirectory;                    
                }
                                
                
                // 判断是否是文件
                if (!File.Exists(path) && !Directory.Exists(path))
                {
                    AnsiConsole.WriteLine("[red]文件或者路径不存在[/]");
                    return;
                }

                if (File.Exists(path))
                {
                    path = "/select," + path;
                }

                // 打开
                Process process = new()
                {
                    StartInfo = new ProcessStartInfo()
                    {
                        FileName = "explorer.exe",
                        Arguments = path,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                    }
                };
                process.Start();
            }, pathOption);

            return true;
        }
    }
}
