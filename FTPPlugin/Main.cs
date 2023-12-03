using FluentFTP.Helpers;
using FluentFTP;
using FTPPlugin;
using OneDo.Plugin;
using Spectre.Console;
using System;
using System.CommandLine;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using OneDo.Utils;

namespace OneDo.FTPPlugin
{
    public class Main : IPlugin
    {
        public bool RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var ftpCommand = new Command("ftp", "FTP文件传输");
            rootCommand.Add(ftpCommand);
            var ftpOption = new Argument<string>("ftp-name", "需要执行上传的ftp名称");
            ftpCommand.Add(ftpOption);
            ftpCommand.SetHandler(name =>
            {
                // 读取参数
                if (config["ftps"] == null)
                {
                    AnsiConsole.MarkupLine("[red]配置文件中未找到 ftps 配置[/]");
                    return;
                }

                var array = config["ftps"].AsArray();
                if (array == null)
                {
                    AnsiConsole.MarkupLine("[red]ftps应是数组");
                    return;
                }

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                if (!JsonHelper.GetJsonArray<FTPModel>(config, "ftps", out var ftpArgs))
                {
                    return;
                }
                var ftpRuns = ftpArgs.FindAll(x => x.Name.ToLower() == name.ToLower());
                if (ftpRuns.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]未找到名称为 {name} 的 ftp 配置[/]");
                    return;
                }

                // 验证参数
                for (var index = 0; index < ftpRuns.Count; index++)
                {
                    if (!ftpRuns[index].Validate(out var message))
                    {
                        AnsiConsole.MarkupLine($"第 {index + 1} 个 {ftpRuns[index].Name} 发生错误: [red]{message}[/]");
                        return;
                    }
                }

                var startDate = DateTime.Now;

                AnsiConsole.Progress()
                .AutoRefresh(true) // Turn off auto refresh
                .AutoClear(false)   // Do not remove the task list when done
                .HideCompleted(false)   // Hide tasks as they are completed
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn()
                    {
                        Alignment=Justify.Left
                    },    // Task description
                    new ProgressBarColumn(),// Progress bar
                    new PercentageColumn(),         // Percentage
                    new RemainingTimeColumn(),      // Remaining time
                    new SpinnerColumn()
                    {
                        Spinner = Spinner.Known.Dots5
                    },// Spinner
                })
                .Start(ctx =>
                {
                    ProgressTask totalProgressTask = ctx.AddTask($"[yellow]总进度: [/]", new ProgressTaskSettings()
                    {
                        AutoStart = true,
                        MaxValue = ftpRuns.Count * 100
                    });

                    // 添加单个进度
                    for (var index = 0; index < ftpRuns.Count; index++)
                    {
                        var ftpRun = ftpRuns[index];
                        if (ftpRun.Method.ToLower() == "put")
                        {
                            FtpUpload(ctx, totalProgressTask, index, ftpRuns[index], startDate);
                        }
                    }
                });

                // 计算花费的时间，单位秒
                var timespan = DateTime.Now - startDate;
                AnsiConsole.MarkupLine($"[springgreen1]上传成功! 耗时 {timespan.Seconds} 秒[/]");
            }, ftpOption);

            // 展示可用的 ftp 配置
            var listCommand = new Command("list", "展示可用的 ftp 配置");
            listCommand.AddAlias("ls");
            ftpCommand.Add(listCommand);
            listCommand.SetHandler(() =>
            {
                var list = new ListPluginConfs(config, "ftps", new Dictionary<string, string>()
                {
                    { "name","名称"},
                    { "host","主机"},
                    { "method","方法"},
                    { "description","描述"}
                });
                list.Show();
            });

            return true;
        }

        /// <summary>
        /// 通过 ftp 上传文件
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="totalProgress"></param>
        /// <param name="index"></param>
        /// <param name="ftpPutOptions"></param>
        private void FtpUpload(ProgressContext ctx, ProgressTask totalProgress, int index, FTPModel ftpPutOptions, DateTime totalStart)
        {
            // 创建进度条
            var description = $"{index + 1}.{ftpPutOptions.Name}: 开始上传...";
            var subProgress = ctx.AddTask(description);
            // create an FTP client
            using FtpClient client = new FtpClient(ftpPutOptions.Host)
            {
                // specify the login credentials, unless you want to use the "anonymous" user account
                Credentials = new NetworkCredential(ftpPutOptions.Username, ftpPutOptions.Password),
                Port = ftpPutOptions.Port,
                Encoding = Encoding.UTF8,
            };
            // begin connecting to the server
            client.Connect();

            // 开启 utf8 编码
            FtpReply ftpReply = client.Execute("OPTS UTF8 ON");
            if (!ftpReply.Code.Equals("200") && !ftpReply.Code.Equals("202"))
                client.Encoding = Encoding.GetEncoding("ISO-8859-1");

            var ftpProgress = (FtpProgress progress) =>
            {
                subProgress.Value(progress.Progress);
                subProgress.Description($"{Path.GetFileName(progress.LocalPath)} : ");

                // 更新总进度
                double accumulatePercent = progress.FileIndex * 1.0 / (progress.FileCount);
                double currentPercent = accumulatePercent + progress.Progress / 100 / progress.FileCount;
                totalProgress.Value((index + currentPercent) * 100);
                totalProgress.Description($"总进度 [blue]{(DateTime.Now - totalStart).ToString(@"hh\:mm\:ss")}[/] :");
            };

            // 判断是否是文件
            if (File.Exists(ftpPutOptions.LocalPath))
            {
                // 上传文件
                client.UploadFile(ftpPutOptions.LocalPath, ftpPutOptions.RemotePath, FtpRemoteExists.Overwrite, progress: ftpProgress);
            }
            else
            {
                client.UploadDirectory(ftpPutOptions.LocalPath, ftpPutOptions.RemotePath, FtpFolderSyncMode.Update, FtpRemoteExists.Overwrite, progress: ftpProgress);
            }
        }
    }
}