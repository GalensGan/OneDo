using OneDo.Plugin;
using OneDo.Utils;
using SixLabors.ImageSharp;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace OneDo.WatermarkPlugin
{
    /// <summary>
    /// <see cref="https://github.com/dotnet/command-line-api/issues/1537"/>
    /// </summary>
    public class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var watermarkCommand = new Command("watermark", "为图片添加水印");
            rootCommand.Add(watermarkCommand);

            // 通用设置
            var silentOption = new Option<bool>("--silent", "静默模式");
            var positionOption = new Option<string>("--position", "水印位置");
            var fillOption = new Option<bool>("--fill", "满布水印");
            var fillSpaceOption = new Option<int>("--fill-space", "满布间距");
            fillSpaceOption.SetDefaultValue(270);
            var pathOption = new Option<string>("--path", "需加水印图片路径或目录(默认当前目录)");
            var suffixOption = new Option<string>("--suffix", "水印图片输出后缀");
            suffixOption.SetDefaultValue("_watermarked");
            var recursiveOption = new Option<bool>("--recursive", "递归所有子目录");
            recursiveOption.AddAlias("-r");
            var grepOption = new Option<string>("--grep", "按正则匹配文件名");
            var opacityOption = new Option<float>("--opacity", "不透明度(0-1.0，0表示完全透明，1 表示不透明)");
            opacityOption.SetDefaultValue(1f);
            var outDirOption = new Option<string>("--out", "加水印后的图片保存目录");
            var angleOption = new Option<float>("--angle", "水印旋转角度");
            angleOption.SetDefaultValue(0);

            var watermarkTextOption = new Option<string>("--text", "文字水印");
            var watermarkImageOption = new Option<string>("--image", "图片水印");

            List<Option> optionsList = new()
            {
                silentOption,positionOption,fillOption,fillSpaceOption,
                pathOption,suffixOption,recursiveOption,
                grepOption,opacityOption,outDirOption,angleOption,
                watermarkTextOption,watermarkImageOption
            };
            optionsList.ForEach(x => watermarkCommand.Add(x));

            var optionsBinder = new WatermarkBinder(silentOption, positionOption, fillOption, fillSpaceOption,
                pathOption, suffixOption, recursiveOption, grepOption,
                opacityOption, outDirOption, angleOption, watermarkTextOption, watermarkImageOption);
            watermarkCommand.SetHandler(options =>
            {
                if (!options.Validate()) return;

                // 查找图片
                var images = GetTargetImages(options);

                // 进行确认
                if (!ConfirmWatermark(images, options)) return;

                // 开始添加水印
                // Synchronous
                AnsiConsole.Status()
                    .Start("开始添加水印...", ctx =>
                    {
                        ctx.Spinner(Spinner.Known.Dots5);
                        ctx.SpinnerStyle(Style.Parse("springgreen1"));
                        foreach (var image in images)
                        {
                            var relativePath = Path.GetRelativePath(options.TargetPath, image);
                            AnsiConsole.MarkupLine($"{relativePath} ...");
                            ctx.Status($"正在为 {Path.GetFileName(image)} 添加水印");

                            // 执行逻辑
                            var watermarker = new Watermarker(image, options);
                            var textWatermarker = new TextWatermark(watermarker);
                            var imageWatermarker = new ImageWatermark(textWatermarker);
                            imageWatermarker.Save(GetSavePath(image,options));

                            AnsiConsole.MarkupLine($"{relativePath} [springgreen1]完成[/]");
                        }
                    });
                
                AnsiConsole.MarkupLine($"[springgreen1]全部完成，共计 {images.Count} 项[/]");
                AnsiConsole.WriteLine();

            }, optionsBinder);
        }

        /// <summary>
        /// 获取目标图片
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private List<string> GetTargetImages(WatermarkOption options)
        {
            List<string> results = new List<string>();
            if (File.Exists(options.TargetPath))
            {
                results.Add(options.TargetPath);
                return results;
            }

            // 说明是目录
            var files = Directory.GetFiles(options.TargetPath, "*.*", options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            results.AddRange(files);

            // 筛选出目标图片
            results = results.FindAll(x =>
            {
                var extension = Path.GetExtension(x).ToLower();
                if (!options.SupportImageExtensions.Contains(extension)) return false;

                // 判断是否有相同后缀
                var name = Path.GetFileNameWithoutExtension(x);
                return !name.EndsWith(options.Suffix);
            });

            // 使用正则过滤
            if (!string.IsNullOrEmpty(options.Grep))
            {
                var regex = new Regex(options.Grep,RegexOptions.IgnoreCase);
                results = results.FindAll(x =>
                {
                    var relativePath = Path.GetRelativePath(options.TargetPath, x);
                    return regex.IsMatch(relativePath);
                });
            }

            return results;
        }

        private bool ConfirmWatermark(List<string> files, WatermarkOption options)
        {
            if (files.Count == 0)
            {
                AnsiConsole.MarkupLine($"[red]未找到任何可添加水印的图片[/]");
                return false;
            }

            // 向用户展示数据
            JsonArray showResults = new JsonArray();
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                var fileName = fileInfo.FullName;
                if (options.TargetPath != fileName)
                {
                    fileName = Path.GetRelativePath(options.TargetPath, fileInfo.FullName);
                }

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
            AnsiConsole.MarkupLine("共找到以下文件:");
            AnsiConsole.WriteLine();
            listShow.Show();

            if (!options.Silent)
            {
                // 进行提示
                var confirmFilesCount = AnsiConsole.Ask($"共找到 {files.Count} 个文件，是否对它们添加水印?(Y/N)", "Y");
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

        /// <summary>
        /// 获取保存路径
        /// </summary>
        /// <param name="originPath"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private string GetSavePath(string originPath, WatermarkOption options)
        {
            string dirPath;
            if (!string.IsNullOrEmpty(options.OutDir))
            {
                dirPath = options.OutDir;
            }
            else
            {
                dirPath = Path.GetDirectoryName(originPath);
            }

            // 获取文件名
            var fileName = Path.GetFileNameWithoutExtension(originPath);
            var extension = Path.GetExtension(originPath);
            return Path.Combine(dirPath, fileName + options.Suffix + extension);
        }
    }
}