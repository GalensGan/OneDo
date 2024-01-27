using OneDo.Plugin;
using OneDo.Utils;
using Spectre.Console;
using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace OneDo.ShellPlugin
{
    public class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var shellCommand = new Command("shell", "执行脚本或者命令");
            shellCommand.AddAlias("cli");
            rootCommand.Add(shellCommand);

            var namesArg = new Argument<List<string>>("names", "指定要执行的脚本名称，多个使用空格分隔");
            shellCommand.Add(namesArg);

            var backgroundOption = new Option<bool>("--background", "后台异步执行");
            backgroundOption.AddAlias("-b");
            shellCommand.AddOption(backgroundOption);
            shellCommand.SetHandler((shellNames, background) =>
            {
                if (shellNames.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]需要指待运行的名称[/]");
                    return;
                }

                // 从配置中解析命令
                if (!JsonHelper.GetJsonArray<ShellModel>(config, "shells", out var shellConfigs)) return;

                if (shellConfigs.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]shells 配置为空[/]");
                    return;
                }

                // 查找名称
                shellNames = shellNames.ConvertAll(x => x.ToLower());
                var targetShells = shellConfigs.FindAll(x => shellNames.Contains(x.Name.ToLower()));
                if (targetShells.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]未找到 {string.Join(",", shellNames)} 配置[/]");
                    return;
                }

                // 开始执行
                foreach (var shell in targetShells)
                {
                    StartShell(shell, background);
                }

                //AnsiConsole.MarkupLine($"[springgreen1]执行成功! 共 {targetShells.Count} 项[/]");
            }, namesArg, backgroundOption);


            #region 命令列表
            var listCommand = new Command("list", "查看所有可用配置");
            listCommand.AddAlias("ls");
            shellCommand.Add(listCommand);
            listCommand.SetHandler(() =>
            {
                var list = new ListPluginConfs(config, "shells", new List<FieldMapper>()
                {
                    new FieldMapper("name","名称"),
                    new FieldMapper("description","描述")
                });
                list.Show();
            });
            #endregion           
        }

        private void StartShell(ShellModel shellModel, bool background)
        {
            Process p = new Process();
            // 如果有附加参数，则要添加            
            var baseDir = shellModel.WorkingDirectory;
            if (string.IsNullOrEmpty(baseDir))
            {
                // 默认使用根目录下的 Shells 目录
                baseDir = Path.Combine(Environment.CurrentDirectory, "shells");
            }
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                WorkingDirectory = baseDir,
                FileName = shellModel.FileName,
                Arguments = shellModel.Arguments,
                UseShellExecute = false,
                RedirectStandardInput = true,// 接受来自调用程序的输入信息
                RedirectStandardOutput = true,// 由调用程序获取输出信息
                RedirectStandardError = true,// 重定向标准错误输出
                CreateNoWindow = true,// 不显示程序窗口
                StandardOutputEncoding = Encoding.Default,
                StandardErrorEncoding = Encoding.Default
            };
            p.StartInfo = startInfo;
            p.OutputDataReceived += P_OutputDataReceived;
            p.ErrorDataReceived += P_ErrorDataReceived;
            p.Start();//启动程序
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.StandardInput.AutoFlush = true;

            if (!background)
            {
                //获取cmd窗口的输出信息
                // string output = p.StandardOutput.ReadToEnd();
                //等待程序执行完退出进程
                p.WaitForExit();
                p.Close();
            }
        }

        private void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            // 转码
            byte[] bytes = Encoding.Default.GetBytes(e.Data);
            bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, bytes);
            string formatString = Encoding.UTF8.GetString(bytes);
            Console.WriteLine(formatString);
        }

        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            // 转码
            byte[] bytes = Encoding.Default.GetBytes(e.Data);
            bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, bytes);
            string formatString = Encoding.UTF8.GetString(bytes);
            Console.WriteLine(formatString);
        }
    }
}