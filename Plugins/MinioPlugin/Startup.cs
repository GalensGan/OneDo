﻿using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using MinioPlugin;
using OneDo.MinioPlugin.Http;
using OneDo.Plugin;
using OneDo.Utils;
using Spectre.Console;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace OneDo.MinioPlugin
{
    /// <summary>
    /// minio 插件注册
    /// </summary>
    public class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var minioCommand = new Command("minio", "向 minio 中上传文件");
            rootCommand.Add(minioCommand);

            var nameArg = new Argument<string>("config-name", "minio 配置名称");
            minioCommand.Add(nameArg);

            var pathsOption = new Option<List<string>>("--paths", "指定上传文件的路径(本地或网络)");
            pathsOption.AddAlias("-p");
            pathsOption.AllowMultipleArgumentsPerToken = true;
            minioCommand.Add(pathsOption);

            var clipboardOption = new Option<bool>("--clipboard-image", "从剪切板中上传图片")
            {
                IsHidden = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            };
            minioCommand.Add(clipboardOption);

            minioCommand.SetHandler(async (name, paths, isFromCb) =>
            {
                if (string.IsNullOrEmpty(name))
                {
                    AnsiConsole.MarkupLine("[red]请指定 minio 配置项名称[/]");
                    return;
                }

                if(paths.ToList().All(x=>string.IsNullOrEmpty(x)) && !isFromCb)               
                {
                    AnsiConsole.MarkupLine("[red]请指定上传文件位置[/]");
                    return;
                }

                // 获取所有的配置
                if (!JsonHelper.GetJsonArray<MinioModel>(config, "minios", out var models)) return;

                // 找到名称
                var minioModel = models.Find(x => x.Name.ToLower() == name.ToLower());
                if (minioModel == null)
                {
                    AnsiConsole.MarkupLine($"[red]未找到 minio 配置 {name}[/]");
                    return;
                }

                // 利用 httpclient 上传                
                var httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(60)
                };

                // 获取 minIO client
                var minioClient = new MinioClient()
                .WithEndpoint(minioModel.Endpoint)
                .WithSSL(minioModel.UseSSL)
                .WithSessionToken(minioModel.SessionToken)
                .WithRegion(minioModel.Region)
                .WithCredentials(minioModel.AccessKey, minioModel.SecretKey)
                .WithTimeout(60000)
                .WithHttpClient(httpClient)
                .Build();

                List<string> uploadResultUrls = new List<string>();
                // 显示进度条
                await AnsiConsole.Progress()
                .AutoRefresh(true) // Turn off auto refresh
                .AutoClear(false)   // Do not remove the task list when done
                .HideCompleted(false)   // Hide tasks as they are completed
                .Columns(
                [
                     new TaskDescriptionColumn()
                     {
                         Alignment = Justify.Left
                     },    // Task description
                    new ProgressBarColumn(),// Progress bar
                    new PercentageColumn(),         // Percentage
                    new RemainingTimeColumn(),      // Remaining time
                    new SpinnerColumn()
                    {
                        Spinner = Spinner.Known.Dots5
                    },// Spinner
                ])
                .Start(async ctx =>
                {
                    List<string> needUploadFiles = DownloadFileToTempDir(httpClient, ctx, paths, isFromCb);

                    // 上传文件
                    List<string> uploadResults = UploadFileFromPath(ctx, minioClient, needUploadFiles, minioModel);

                    // 删除临时目录
                    var tempMinioDir = Path.Combine(Path.GetTempPath(), "OneDo\\MinioPlugin");
                    if (Directory.Exists(tempMinioDir))
                    {
                        Directory.Delete(tempMinioDir, true);
                    }

                    uploadResultUrls.AddRange(uploadResults);
                });

                if (uploadResultUrls.Count == 0)
                {
                    AnsiConsole.MarkupLine("[red]上传失败[/]");
                    return;
                }

                // 输出上传结果
                AnsiConsole.MarkupLine($"[springgreen1]上传成功! 共 {uploadResultUrls.Count} 项[/]");
                uploadResultUrls.ForEach(x => AnsiConsole.MarkupLine($"[green]{x}[/]"));
            }, nameArg, pathsOption, clipboardOption);

            #region 添加 list
            var listCommand = new Command("list", "查看所有可用的 MinIO 配置");
            listCommand.AddAlias("ls");
            minioCommand.Add(listCommand);
            listCommand.SetHandler(() =>
            {
                var list = new ListPluginConfs(config, "minios", new List<FieldMapper>()
                {
                    new("name","名称"),
                    new FieldMapper("endPoint", "连接端点"),
                    new FieldMapper("bucketName", "桶名称"),
                    new FieldMapper("description", "描述")
                });
                list.Show();
            });
            #endregion
        }

        /// <summary>
        /// 如果是剪切板、url 文件，则将文件保存到临时目录中
        /// </summary>
        /// <returns></returns>
        private List<string> DownloadFileToTempDir(HttpClient httpClient, ProgressContext ctx, List<string> paths, bool clipboard)
        {
            // 获取当前系统类型
            List<string> fileNames = [];

            if (clipboard && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // 剪切板中的文件保存到临时目录中
                var subTempPath = $"OneDo\\MinioPlugin\\screenshot_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.png";
                // 调用脚本保存图片到临时目录
                var currentDir = Assembly.GetExecutingAssembly().Location;
                var shellFileName = Directory.GetFiles(Path.GetDirectoryName(currentDir), "saveImageFromClipboard.ps1", SearchOption.AllDirectories).FirstOrDefault();

                Process.Start("powershell.exe", $"{shellFileName} {subTempPath}").WaitForExit();

                var imageFullPath = Path.Combine(Path.GetTempPath(), subTempPath);
                if (File.Exists(imageFullPath))
                {
                    fileNames.Add(imageFullPath);
                }
            }

            // 如果文件是来自网络，则下载到临时目录中
            foreach (var path in paths)
            {
                if (!string.IsNullOrEmpty(path) && path.ToLower().StartsWith("http"))
                {
                    // 获取文件名
                    var subTempPath = $"OneDo\\MinioPlugin\\{Path.GetFileName(path)}";
                    var imageFullPath = Path.Combine(Path.GetTempPath(), subTempPath);
                    var dirName = Path.GetDirectoryName(imageFullPath);
                    // 如果目录不存在，则新建
                    if (!Directory.Exists(dirName))
                    {
                        Directory.CreateDirectory(dirName);
                    }

                    // 提示正在下载文件
                    var downloadFileTask = ctx.AddTask($"下载 {Path.GetFileName(path)} ");
                    var response = httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead).Result;
                    if (response.IsSuccessStatusCode)
                    {
                        using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
                        {
                            var httpProgress = new HttpProgress(downloadFileTask);
                            using var fileStream = File.Create(imageFullPath);
                            var progressStream = new ProgressStream(fileStream)
                                .WithProgress(httpProgress);
                            stream.CopyTo(progressStream);
                        }
                        fileNames.Add(imageFullPath);
                    }

                    continue;
                }

                // 如果是本地文件，判断是否存在
                if (!string.IsNullOrEmpty(path) && !path.ToLower().StartsWith("http"))
                {
                    if (File.Exists(path))
                    {
                        fileNames.Add(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        // 如果是文件夹，则添加所有文件
                        var allFileNames = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        fileNames.AddRange(allFileNames);
                    }
                }
            }

            return fileNames;
        }

        /// <summary>
        /// 从路径上传图片
        /// </summary>
        /// <param name="filePath"></param>
        private List<string> UploadFileFromPath(ProgressContext ctx, IMinioClient client, List<string> filePaths, MinioModel minioModel)
        {
            List<string> resultUrls = [];
            var  httpClient = client.Config.HttpClient;
            for (var fileIndex = 0; fileIndex < filePaths.Count; fileIndex++)
            {
                var fileInfo = new FileInfo(filePaths[fileIndex]);
                // 判断文件是否存在
                if (!fileInfo.Exists)
                {
                    AnsiConsole.MarkupLine($"[red]文件 {fileInfo.FullName} 不存在[/]");
                    continue;
                }

                // 判断桶是否存在
                BucketExistsArgs bucketExistsArgs = new BucketExistsArgs().WithBucket(minioModel.BucketName);
                if (!client.BucketExistsAsync(bucketExistsArgs).GetAwaiter().GetResult())
                {
                    // 判断是否要新建
                    if (minioModel.CreateWhenBucketNotExist)
                    {
                        // 创建桶
                        MakeBucketArgs makeBucketArgs = new MakeBucketArgs().WithBucket(minioModel.BucketName);
                        client.MakeBucketAsync(makeBucketArgs).GetAwaiter().GetResult();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[red]{minioModel.BucketName} 桶不存在，可以设置 createWhenBucketNotExist 为 true 进行新建[/]");
                        continue;
                    }
                }

                var objectFullName = Path.Combine(minioModel.ObjectDir, fileInfo.Name).Replace("\\", "/");

                // 开始上传文件
                var progressTask = ctx.AddTask($"上传 {Path.GetFileName(filePaths[fileIndex])} ");
                //// 注册进度回调
                //void action(object? sender, HttpProgressEventArgs e)
                //{
                //    progressTask.Value = e.ProgressPercentage;
                //}
                //httpClient.HttpSendProgress = action;
                // 获取预上传的url
                //var presignedPutArg = new PresignedPutObjectArgs()                    
                //    .WithBucket(minioModel.BucketName)
                //    .WithObject(objectFullName)
                //    .WithExpiry(24 * 60 * 60);
                //var uploadUrl = client.PresignedPutObjectAsync(presignedPutArg).GetAwaiter().GetResult();
                //var fs = fileInfo.OpenRead();
                //var sc = new StreamContent(fs);                
                //HttpResponseMessage resMessage = httpClient.PutAsync(uploadUrl, sc).GetAwaiter().GetResult();
                //fs.Close();

                var minioProgress = new MinioProgress(progressTask);
                var fileStrem = fileInfo.OpenRead();
                var putObjectArg = new PutObjectArgs()
                    .WithBucket(minioModel.BucketName)
                    .WithObject(objectFullName)
                    .WithContentType(MimeTypes.GetMimeType(fileInfo.FullName))
                    .WithStreamData(fileStrem)
                    .WithProgress(minioProgress)
                    .WithObjectSize(fileInfo.Length);

                try
                {
                    client.PutObjectAsync(putObjectArg).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    AnsiConsole.MarkupLine($"[red]{fileInfo.FullName} 上传失败 : {e.Message}[/]");
                    continue;
                }
                finally
                {
                    fileStrem.Close();
                }

                // 添加到成果中
                resultUrls.Add($"/{minioModel.BucketName}/{objectFullName}");
            }

            // 写入命令行
            // 要是进度条外面写才能显示            
            var protocol = minioModel.UseSSL ? "https" : "http";
            // 返回结果
            return resultUrls.ConvertAll(x =>
            {
                if (!x.StartsWith("http")) return $"{protocol}://{minioModel.Endpoint}{x}";
                return x;
            });
        }


    }
}