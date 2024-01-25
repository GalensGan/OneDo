using OneDo.Plugin;
using OneDo.Utils;
using OpenMcdf;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Principal;
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

            var pathsOption = new Option<List<string>>("--path", "[可选] max 文件名或者目录名(默认为当前环境目录)");
            pathsOption.AddAlias("-p");
            archiveCommand.Add(pathsOption);
            var grepOption = new Option<string>("--grep", "[可选] max文件过滤方式(支持正则表达式)");
            archiveCommand.Add(grepOption);
            var yesOption = new Option<bool>("--yes", "[可选] 自动确认");
            yesOption.AddAlias("-y");
            archiveCommand.Add(yesOption);
            var outOption = new Option<string>("--out", "[可选] 输出目录，所有的文件都会归档到这个目录中");
            outOption.AddAlias("-o");
            archiveCommand.Add(outOption);
            var softLinkOption = new Option<bool>("--soft-link", "[可选] 使用软链定位到贴图文件(不占用额外空间)");
            softLinkOption.AddAlias("-s");
            archiveCommand.Add(softLinkOption);
            var reverseOption = new Option<bool>("--reverse", "[可选] 当使用软链时，将归档的文件移动到输出目录，然后创建一个软链指向当前位置");
            reverseOption.AddAlias("-r");
            archiveCommand.Add(reverseOption);

            // 添加操作
            archiveCommand.SetHandler((path, filter, yes, outDir, softLink, reverse) =>
            {
                // 检查 everything 是否运行
                if (!CheckEverythingRunning()) return;

                // 验证是否有管理员权限
                if (!CheckAuth(outDir, softLink, reverse)) return;

                // 限制 reverse
                // 仅当有 outDir 且 softLink 时才能使用 reverse
                if (string.IsNullOrEmpty(outDir) || !softLink)
                {
                    // 重置为 false
                    reverse = false;
                }

                if (!ConfirmOutDir(outDir, yes)) return;

                // 获取 max 文件
                List<MaxFile> dotMaxFiles = GetDotMaxFiles(path, filter, outDir);
                if (dotMaxFiles.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]未找到 .max 文件[/]");
                    return;
                }

                // 如果有 outDir, 要对文件使用 hash 去重
                dotMaxFiles = DistincFiles(dotMaxFiles, outDir);

                // 对 max 文件进行确认
                if (!ConfirmMaxFiles(dotMaxFiles, yes))
                {
                    AnsiConsole.MarkupLine($"[red]执行中断。文件数据确认失败[/]");
                    return;
                }

                AnsiConsole.Status()
                    .Start("max 文件归档中...", ctx =>
                    {
                        // 使用多线程同时处理 max 文件
                        // 读取 max 文件流，解析其中的贴图文件
                        //var tasks = dotMaxFiles.ConvertAll(x => Task.Run(() => ArchiveMaxFile(x, yes, outDir, softLink)));
                        //Task.WaitAll(tasks.ToArray());

                        foreach (var file in dotMaxFiles)
                            ArchiveMaxFile(file, yes, outDir, softLink, reverse);
                    });

                // 显示归档状态
                ShowArchiveStatus(dotMaxFiles);


            }, pathsOption, grepOption, yesOption, outOption, softLinkOption, reverseOption);
        }

        private static bool CheckEverythingRunning()
        {
            if (EverythingSDK.Everything_IsDBLoaded()) return true;
            string everythingHome = "https://www.voidtools.com/zh-cn/";
            AnsiConsole.MarkupLine($"[red]该插件需要先启动 Everything[/], 点击此处下载：[blue]{everythingHome}[/]");
            return false;
        }


        /// <summary>
        /// 验证权限
        /// </summary>
        /// <returns></returns>
        private static bool CheckAuth(string outDir, bool softLink, bool reverseLink)
        {
            if (string.IsNullOrEmpty(outDir)) return true;
            if (!softLink || !reverseLink) return true;

            // 验证是否有管理员权限
            if (!IsAdministrator())
            {
                AnsiConsole.MarkupLine($"[red]软链接需要管理员权限[/]");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 是否是管理员
        /// </summary>
        /// <returns></returns>
        private static bool IsAdministrator()
        {
            // 获取当前用户的 Windows 身份
            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            // 创建 WindowsPrincipal 对象
            WindowsPrincipal principal = new WindowsPrincipal(identity);

            // 检查是否是管理员
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool ConfirmOutDir(string outDir, bool yes)
        {
            // 判断输出目录是否存在，不存在则新建
            if (string.IsNullOrEmpty(outDir)) return true;

            // 说明输出目录不存在
            if (!yes && !Directory.Exists(outDir))
            {
                var confirmOutDir = AnsiConsole.Ask<string>($"输出目录：{outDir} 不存在,是否新建？(y/n)", "y");
                if (!confirmOutDir.ToLower().Contains("y"))
                {
                    AnsiConsole.MarkupLine($"[red]执行中断。输出目录不存在[/]");
                    return false;
                }
            }

            // 新建目录
            Directory.CreateDirectory(outDir);

            return true;
        }

        /// <summary>
        /// 获取 .Max 文件
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static List<MaxFile> GetDotMaxFiles(List<string> paths, string filter, string outDir)
        {
            // 为空时，使用当前环境目录
            if (paths == null)
                paths = new List<string>();
            if (paths.Count == 0)
                paths.Add(Environment.CurrentDirectory);

            // 解析其中的 max 文件
            List<string?> fileNames = new List<string>();
            foreach (string path in paths)
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    fileNames.Add(fileInfo.FullName);
                    continue;
                }

                if (Directory.Exists(path))
                {
                    // 说明是目录，读取目录下所有 .max 文件
                    var maxFiles = Directory.GetFiles(path, "*.max", SearchOption.AllDirectories);
                    var fullNames = maxFiles.ToList().ConvertAll(x => Path.GetFullPath(x));
                    fileNames.AddRange(fullNames);
                    continue;
                }
            }

            // 文件中可能有软链，将软链转换成真实路径
            fileNames = fileNames.ConvertAll(x =>
            {
                FileInfo pathInfo = new FileInfo(x);
                if (pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    // 说明是软链
                    string linkTarget = pathInfo.LinkTarget;
                    if (linkTarget == null) return null;
                    return Path.GetFullPath(linkTarget);
                }
                return x;
            }).FindAll(x => !string.IsNullOrEmpty(x));

            // 去掉 outDir 中的文件
            if (!string.IsNullOrEmpty(outDir))
            {
                var outFiles = Directory.GetFiles(outDir, "*.max", SearchOption.AllDirectories);
                var outFileFullNames = outFiles.ToList().ConvertAll(x => Path.GetFullPath(x));
                fileNames = fileNames.FindAll(x => !outFileFullNames.Contains(x));
            }

            // 去重
            fileNames = fileNames.Distinct().ToList();

            // 对文件执行匹配
            if (!string.IsNullOrEmpty(filter))
            {
                var regex = new Regex(filter, RegexOptions.IgnoreCase);
                fileNames = fileNames.FindAll(x => regex.IsMatch(x));
            }

            return fileNames.ConvertAll(x => new MaxFile()
            {
                MaxPath = x
            });
        }

        /// <summary>
        /// 按 hash 值去重
        /// </summary>
        /// <param name="maxFiles"></param>
        /// <param name="outDir"></param>
        /// <returns></returns>
        private static List<MaxFile> DistincFiles(List<MaxFile> maxFiles, string outDir)
        {
            if (string.IsNullOrEmpty(outDir)) return maxFiles;
            if (maxFiles.Count <= 1) return maxFiles;

            // 计算文件的 hash 值
            var tasks = maxFiles.ConvertAll(x => Task.Run(() =>
            {
                var hash = ComputeFileHash(x.MaxPath);
                x.Hash = hash;
            }));
            Task.WaitAll(tasks.ToArray());

            // 按 hash 值去重
            return maxFiles.DistinctBy(x => x.Hash).ToList();
        }

        private static string ComputeFileHash(string filePath)
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                HashAlgorithm hashAlgorithm = SHA256.Create();
                byte[] hashBytes = hashAlgorithm.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "");
            }
            catch (FileNotFoundException ex)
            {
                AnsiConsole.MarkupLine($"[red]File not found: {ex.Message}[/]");
                return null;
            }
            catch (IOException ex)
            {
                AnsiConsole.MarkupLine($"[red]Error reading the file: {ex.Message}[/]");
                return null;
            }
        }

        /// <summary>
        /// 确认 max file 文件数量
        /// </summary>
        /// <returns></returns>
        private static bool ConfirmMaxFiles(List<MaxFile> files, bool yes)
        {
            // 向用户展示数据
            JsonArray showResults = new JsonArray();
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file.MaxPath);
                var fileName = fileInfo.FullName;

                showResults.Add(new JsonObject()
                {
                    { "fileName",fileName},
                    { "fileSize",FormatFileSize(fileInfo.Length)},
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

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            while (bytes >= 1024 && order < sizes.Length - 1)
            {
                order++;
                bytes /= 1024;
            }

            return string.Format("{0:0.##} {1}", bytes, sizes[order]);
        }

        private static string GetOutDir(string outDir, string fileName)
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
        private static void ArchiveMaxFile(MaxFile maxFile, bool yes, string outDir, bool softLink, bool reverseLink)
        {
            string maxFileFullName = maxFile.MaxPath;

            var actualOutDir = GetOutDir(outDir, maxFileFullName);
            var relativeDisplayName = Path.GetRelativePath(actualOutDir, maxFileFullName);

            AnsiConsole.MarkupLine($"正在解析文件资源：{relativeDisplayName}");
            maxFile.FileAssets = GetMapFileNames(maxFileFullName);

            AnsiConsole.MarkupLine($"文件资源解析 [springgreen1]完成[/]");

            // 新建 maps 文件夹
            Directory.CreateDirectory(Path.Combine(actualOutDir, "maps"));

            // 复制 max 文件
            var targetMaxPath = Path.Combine(actualOutDir, Path.GetFileName(maxFileFullName));
            CopyOrSoftlinkFile(maxFileFullName, targetMaxPath, softLink, reverseLink);

            // 图片后缀
            List<string> imageExtensions = new List<string>()
            {
                ".jpg",
                ".png",
                ".jpeg"
            };

            // 判断是否有缩略图
            List<string> thubnailImages = imageExtensions.ConvertAll(x => Path.ChangeExtension(maxFileFullName, x));
            foreach (var thubnailImage in thubnailImages)
            {
                if (File.Exists(thubnailImage))
                {
                    var targetThumbnailJpg = Path.Combine(actualOutDir, Path.GetFileName(thubnailImage));
                    CopyOrSoftlinkFile(thubnailImage, targetThumbnailJpg, softLink, reverseLink);
                    maxFile.ThubnailPath = thubnailImage;
                }
            }

            // 复制 map 文件
            foreach (var mapFileName in maxFile.FileAssets)
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
                        maxFile.LostAssets.Add(mapFileName);
                        AnsiConsole.MarkupLine($"[red]{mapFileName} 材质已丢失[/]");
                        continue;
                    }

                    sourceMapFile = findedMap;
                }

                // 开始复制文件
                CopyOrSoftlinkFile(sourceMapFile, targetMapFile, softLink, reverseLink);
            }

            AnsiConsole.MarkupLine($"{relativeDisplayName} 归档 [springgreen1]完成[/]");
        }

        private static List<string> GetMapFileNames(string maxFileFullName)
        {
            if(!GetAssetsBytes(maxFileFullName,out var bytes))
            {
                return new List<string>();
            }

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

        private static bool GetAssetsBytes(string maxFileFullName,out byte[] bytes)
        {
            CompoundFile cf = new(maxFileFullName);
            //cf.RootStorage.VisitEntries(item =>
            //{
            //    var name = item.Name;
            //    Console.WriteLine($"entry:{name}-{item.Size}-{item.IsStream}-{item.IsStorage}-{item.CLSID}");
            //}, true);

            // 贴图在 FileAssetMetaData2 流中
            List<string> streamNames = new List<string>()
            {
                "FileAssetMetaData2",
                "FileAssetMetaData3"
            };
            foreach(var name in streamNames)
            {
                if (cf.RootStorage.TryGetStream(name, out var stream))
                {
                    bytes = new byte[stream.Size];
                    stream.Read(bytes, 0, (int)stream.Size);
                    cf.Close();
                    return true;
                }
            }

            bytes = null;
            AnsiConsole.MarkupLine($"[red]无法从文件 {maxFileFullName} 提取贴图，请联系作者(https://galensgan.gitee.io/)进行兼容[/]");
            return false;
        }

        /// <summary>
        /// 复制或者创建软链
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="softLink"></param>
        /// <returns></returns>
        private static bool CopyOrSoftlinkFile(string source, string target, bool softLink, bool reverseLink)
        {
            if (File.Exists(target))
            {
                return true;
            }

            if (softLink)
            {
                return CreateSymbolicLink(source, target, reverseLink);
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
        private static bool CreateSymbolicLink(string sourceFilePath, string symbolicLinkPath, bool reverseLink)
        {
            try
            {
                if (File.Exists(symbolicLinkPath)) return true;

                // 如果是反向链接，先将文件移动到输出目录
                if (reverseLink)
                {
                    File.Move(sourceFilePath, symbolicLinkPath);
                    (symbolicLinkPath, sourceFilePath) = (sourceFilePath, symbolicLinkPath);
                }

                // 第一个是软链路径，第二个是原始文件路径
                File.CreateSymbolicLink(symbolicLinkPath, sourceFilePath);
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                AnsiConsole.MarkupLine($"[red]创建软链接失败：{ex.Message}[/]");
                return false;
            }
            catch (IOException ex)
            {
                Console.WriteLine($"创建软链接失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 确认 max file 文件数量
        /// </summary>
        /// <returns></returns>
        private static void ShowArchiveStatus(List<MaxFile> files)
        {
            AnsiConsole.WriteLine();
            // 显示归档结果
            AnsiConsole.MarkupLine($"[springgreen1]归档成功！共计 {files.Count} 项：[/]");
            AnsiConsole.WriteLine();

            // 向用户展示数据
            JsonArray showResults = new JsonArray();
            foreach (var file in files)
            {
                showResults.Add(new JsonObject()
                {
                    { "fileName",file.MaxPath},
                    { "assetsCount",file.FileAssets.Count},
                    { "lostAssetsCount",string.Join(";",file.LostAssets)},
                    { "thubnail",string.IsNullOrEmpty(file.ThubnailPath)?"-":"有"}
                });
            }
            var listShow = new ListPluginConfs(showResults, new List<FieldMapper>()
            {
                new FieldMapper("fileName","文件名"),
                new FieldMapper("assetsCount","资源文件数量"),
                new FieldMapper("lostAssetsCount","已丢失"),
                new FieldMapper("thubnail","缩略图")
            });
            listShow.Show();
            AnsiConsole.WriteLine();
        }
    }
}
