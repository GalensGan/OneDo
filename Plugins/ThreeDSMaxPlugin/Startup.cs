using OneDo.Plugin;
using OneDo.Utils;
using OpenMcdf;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace OneDo.ThreeDSMaxPlugin
{
    public class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var maxCommand = new Command("max", "提供 3D Studio Max 相关便捷工具");
            rootCommand.Add(maxCommand);

            // 归档命令
            var archiveCommand = new Command("archive", "使用 everything 从本机中查找所有的贴图并进行归档");
            maxCommand.Add(archiveCommand);

            var pathsOption = new Option<List<string>>("--path", "max 文件名或者目录名");
            pathsOption.AddAlias("-p");
            archiveCommand.Add(pathsOption);
            var grepOption = new Option<string>("--grep", "max文件过滤方式(支持正则表达式)");
            archiveCommand.Add(grepOption);
            var yesOption = new Option<bool>("--yes", "自动确认");
            yesOption.AddAlias("-y");
            archiveCommand.Add(yesOption);
            var outOption = new Option<string>("--out", "输出目录");
            outOption.AddAlias("-o");
            archiveCommand.Add(outOption);
            var softLinkOption = new Option<bool>("--soft-link", "使用软链定位到贴图文件(不占用空间)");
            softLinkOption.AddAlias("-s");
            archiveCommand.Add(softLinkOption);

            // 添加操作
            archiveCommand.SetHandler((path, filter, yes, outDir, softLink) =>
            {
                // 判断输出目录是否存在，不存在则新建
                if (!string.IsNullOrEmpty(outDir))
                {
                    // 说明输出目录不存在
                    if (!yes && !Directory.Exists(outDir))
                    {
                        var confirmOutDir = AnsiConsole.Ask<string>($"输出目录：{outDir} 不存在,是否新建？(y/n)", "y");
                        if (!confirmOutDir.ToLower().Contains("y"))
                        {
                            AnsiConsole.MarkupLine($"[red]执行中断。输出目录不存在[/]");
                            return;
                        }
                    }

                    // 新建目录
                    Directory.CreateDirectory(outDir);
                }

                // 获取 max 文件
                List<string> dotMaxFiles = GetDotMaxFiles(path, filter);

                // 对 max 文件进行确认
                if (!ConfirmMaxFiles(dotMaxFiles, yes))
                {
                    AnsiConsole.MarkupLine($"[red]执行中断。文件数据确认失败[/]");
                    return;
                }

                // 开始添加水印
                // Synchronous
                AnsiConsole.Status()
                    .Start("max 文件归档中...", ctx =>
                    {
                        // 使用多线程同时处理 max 文件
                        // 读取 max 文件流，解析其中的贴图文件
                        //var tasks = dotMaxFiles.ConvertAll(x => Task.Run(() => ArchiveMaxFile(x, yes, outDir, softLink)));
                        //Task.WaitAll(tasks.ToArray());

                        foreach (var file in dotMaxFiles)
                            ArchiveMaxFile(file, yes, outDir, softLink);
                    });

                AnsiConsole.MarkupLine($"[springgreen1]归档成功！共计 {dotMaxFiles.Count} 项[/]");

            }, pathsOption, grepOption, yesOption, outOption, softLinkOption);
        }

        /// <summary>
        /// 获取 .Max 文件
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private List<string> GetDotMaxFiles(List<string> paths, string filter)
        {
            // 为空时，使用当前环境目录
            if (paths == null)
                paths = new List<string>();
            if (paths.Count == 0)
                paths.Add(Environment.CurrentDirectory);

            // 解析其中的 max 文件
            List<string> fileNames = new List<string>();
            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    fileNames.Add(path);
                    continue;
                }

                if (Directory.Exists(path))
                {
                    // 说明是目录，读取目录下所有 .max 文件
                    var maxFiles = Directory.GetFiles(path, "*.max", SearchOption.AllDirectories);
                    fileNames.AddRange(maxFiles);
                    continue;
                }
            }

            // 对文件执行匹配
            if (!string.IsNullOrEmpty(filter))
            {
                var regex = new Regex(filter, RegexOptions.IgnoreCase);
                fileNames = fileNames.FindAll(x => regex.IsMatch(x));
            }

            return fileNames;
        }

        /// <summary>
        /// 确认 max file 文件数量
        /// </summary>
        /// <returns></returns>
        private bool ConfirmMaxFiles(List<string> files, bool yes)
        {
            // 向用户展示数据
            JsonArray showResults = new JsonArray();
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var fileName = fileInfo.FullName;

                showResults.Add(new JsonObject()
                {
                    { "fileName",fileName},
                    { "fileSize",FormatFileSize(file.Length)},
                    { "lastModifyDate",fileInfo.LastWriteTime.ToString("F")}
                });
            }
            var listShow = new ListPluginConfs(showResults, new List<FieldMapper>()
            {
                new FieldMapper("fileName","文件名"),
                new FieldMapper("fileSize","文件大小"),
                new FieldMapper("lastModifyDate","最后修改日期")
            });
            AnsiConsole.MarkupLine("共找到以下 .max 文件:");
            AnsiConsole.WriteLine();
            listShow.Show();

            if (!yes)
            {
                // 进行提示
                var confirmFilesCount = AnsiConsole.Ask($"共找到 {files.Count} 个文件，是否对它们进行归档?(y/n)", "y");
                if (!confirmFilesCount.ToLower().Contains("y")) return false;
                AnsiConsole.WriteLine();
            }

            return true;
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }

            return String.Format("{0:0.##} {1}", bytes, sizes[order]);
        }

        private string GetOutDir(string outDir, string fileName)
        {
            if (string.IsNullOrEmpty(outDir)) return Path.GetDirectoryName(fileName);
            return outDir;
        }

        /// <summary>
        /// 新线程归档
        /// </summary>
        /// <param name="maxFileFullName"></param>
        /// <param name="yes"></param>
        /// <param name="outDir"></param>
        /// <param name="softLink"></param>
        private void ArchiveMaxFile(string maxFileFullName, bool yes, string outDir, bool softLink)
        {
            var actualOutDir = GetOutDir(outDir, maxFileFullName);
            var relativeDisplayName = Path.GetRelativePath(actualOutDir, maxFileFullName);

            AnsiConsole.MarkupLine($"解析文件资源：{relativeDisplayName}");

            List<string> mapFiles = GetMapFileNames(maxFileFullName);

            AnsiConsole.MarkupLine($"解析文件资源 [springgreen1]完成[/]");

            // 新建 maps 文件夹
            Directory.CreateDirectory(Path.Combine(actualOutDir, "maps"));

            // 复制 max 文件
            var targetMaxPath = Path.Combine(actualOutDir, Path.GetFileName(maxFileFullName));
            CopyOrSoftlinkFile(maxFileFullName, targetMaxPath, softLink);

            // 复制 jpg 图片
            var thumbnailJpg = Regex.Replace(maxFileFullName, @"\.max$", @".jpg");
            if (File.Exists(thumbnailJpg))
            {
                var targetThumbnailJpg = Path.Combine(actualOutDir, Path.GetFileName(thumbnailJpg));
                CopyOrSoftlinkFile(thumbnailJpg, targetThumbnailJpg, softLink);
            }

            // 复制 map 文件
            foreach (var mapFileName in mapFiles)
            {
                // 生成目标路径
                var targetMapFile = Path.Combine(actualOutDir, "maps", mapFileName);
                var sourceMapFile = Path.Combine(Path.GetDirectoryName(maxFileFullName), "maps", mapFileName);
                // 如果 source 不存在，则使用 everything 进行查找
                if (!File.Exists(sourceMapFile))
                {
                    var findedMap = EverythingSDK.SearchOne($"*{mapFileName}");
                    if (findedMap == null)
                    {
                        AnsiConsole.MarkupLine($"[red]{mapFileName} 材质已丢失[/]");
                        continue;
                    }

                    sourceMapFile = findedMap;
                }

                // 开始复制文件
                CopyOrSoftlinkFile(sourceMapFile, targetMapFile, softLink);
            }

            AnsiConsole.MarkupLine($"{relativeDisplayName} [springgreen1]完成[/]");
        }

        private List<string> GetMapFileNames(string maxFileFullName)
        {
            CompoundFile cf = new(maxFileFullName);
            //cf.RootStorage.VisitEntries(item =>
            //{
            //    var name = item.Name;
            //    Console.WriteLine($"entry:{name}-{item.Size}-{item.IsStream}-{item.IsStorage}-{item.CLSID}");
            //}, true);
            // 贴图在 FileAssetMetaData2 流中
            var sceneStrem = cf.RootStorage.GetStream("FileAssetMetaData2");
            var bytes = new byte[sceneStrem.Size];
            sceneStrem.Read(bytes, 0, (int)sceneStrem.Size);
            cf.Close();

            // 提取文件名称
            // 6000 后面的为文件名
            List<int> assetFileNameStartIndexes = new List<int>();
            for (int i = 0; i < bytes.Length - 3; i++)
            {
                if (bytes[i] == 6 && bytes[i + 1] == 0 && bytes[i + 2] == 0 && bytes[i + 3] == 0)
                {
                    i += 4;
                    assetFileNameStartIndexes.Add(i);
                    continue;
                }
            }
            // +16 是为了方便统一处理
            assetFileNameStartIndexes.Add(bytes.Length - 1 + 21);

            // AnsiConsole.WriteLine(string.Join(" ", assetFileNameStartIndexes));

            // 提取文件
            List<string> fileNames = new List<string>();
            for (int i = 0; i < assetFileNameStartIndexes.Count - 1; i++)
            {
                var startIndex = assetFileNameStartIndexes[i];
                var nextIndex = assetFileNameStartIndexes[i + 1];
                var tempBytes = bytes.Skip(startIndex).Take(nextIndex - startIndex - 22).ToArray();
                // 将字节转换成字符串
                var fileName = Encoding.Unicode.GetString(tempBytes);
                // AnsiConsole.WriteLine(fileName);
                // 只有包含 Bitmap 的才有效
                if (fileName.StartsWith("Bitmap"))
                {
                    fileNames.Add(fileName.Substring(10));
                }
            }

            return fileNames;
        }

        /// <summary>
        /// 复制或者创建软链
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="softLink"></param>
        /// <returns></returns>
        private bool CopyOrSoftlinkFile(string source, string target, bool softLink)
        {
            if (File.Exists(target))
            {
                return true;
            }

            if (softLink)
            {
                CreateSymbolicLink(source, target);
                return true;
            }

            // 复制到目标位置
            File.Copy(source, target, false);

            return true;
        }

        /// <summary>
        /// 创建软链
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="symbolicLinkPath"></param>
        private void CreateSymbolicLink(string sourceFilePath, string symbolicLinkPath)
        {
            try
            {
                // 使用 CreateSymbolicLink 方法创建软链接
                if (!File.Exists(symbolicLinkPath))
                {
                    // 参数 "file" 表示创建文件软链接
                    // 参数 "targetPath" 是软链接的目标文件路径
                    File.CreateSymbolicLink(symbolicLinkPath, sourceFilePath);
                    AnsiConsole.MarkupLine("软链接创建成功！");
                }
                else
                {
                    AnsiConsole.MarkupLine("软链接已经存在。");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                AnsiConsole.MarkupLine($"[red]创建软链接失败：{ex.Message}[/]");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"创建软链接失败：{ex.Message}");
            }
        }
    }
}
