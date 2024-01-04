using SixLabors.ImageSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OneDo.WatermarkPlugin
{
    internal class WatermarkOption
    {
        /// <summary>
        /// 是否静执行
        /// </summary>
        public bool Silent { get; set; }

        /// <summary>
        /// 位置参数
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// 是否填充
        /// 有 Position 后，该参数将忽略
        /// </summary>
        public bool Fill { get; set; }

        /// <summary>
        /// 加水印的文件路径
        /// </summary>
        public string TargetPath { get; set; }

        /// <summary>
        /// 加完水印后的图片名后缀
        /// </summary>
        public string Suffix { get; set; }

        /// <summary>
        /// 是否递归遍历所有文件
        /// </summary>
        public bool Recursive { get; set; }

        /// <summary>
        /// 正则过滤语句
        /// </summary>
        public string Grep { get; set; }

        /// <summary>
        /// 不透明度，范围为 0-1，1 代表完全不透明
        /// </summary>
        public float Opacity { get; set; }

        /// <summary>
        /// 导出目录，默认保存到当前目录中
        /// </summary>
        public string OutDir { get; set; }

        /// <summary>
        /// 水印旋转角
        /// </summary>
        public float Angle { get; set; }

        /// <summary>
        /// 文字水印
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 图片水印
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// 支持的图片后缀
        /// </summary>
        public List<string> SupportImageExtensions = new List<string>()
        {
            ".jpg",
            ".png",
            ".bmp",
            ".jpeg"
        };

        /// <summary>
        /// 进行参数校验
        /// </summary>
        /// <returns></returns>
        public bool Validate()
        {
            // 添加默认值
            if (string.IsNullOrEmpty(TargetPath))
            {
                // 使用当前目录
                TargetPath = Environment.CurrentDirectory;
            }
            if (string.IsNullOrEmpty(Suffix))
            {
                Suffix = "_watermarked";
            }

            if (!ValidatePosition()) return false;

            // 图片和文字必须有一个要输入
            if (string.IsNullOrEmpty(Image) && string.IsNullOrEmpty(Text))
            {
                AnsiConsole.MarkupLine("[red]请至少指定一个 --text 或 --image 参数，其中 --text 若是数字，需要使用引号包裹[/]");
                return false;
            }

            // 对 Fill 进行提示
            if (Fill && !string.IsNullOrEmpty(Position) && !Silent)
            {
                var positionConfirm = AnsiConsole.Ask("已经指定 --position, 将忽略 --fill 参数，是否继续？([springgreen1]Y/N[/])", "Y");
                if (!positionConfirm.ToLower().Contains("y")) return false;
                AnsiConsole.WriteLine();
            }

            // 验证图片水印
            if (!string.IsNullOrEmpty(Image))
            {
                // 验证文件是否存在
                if (!File.Exists(Image))
                {
                    AnsiConsole.MarkupLine($"[red]{Image} 文件不存在[/]");
                    return false;
                }

                // 验证是否是图片
                var imageExtension = Path.GetExtension(Image).ToLower();
                if (!SupportImageExtensions.Contains(imageExtension))
                {
                    AnsiConsole.MarkupLine($"{imageExtension} 文件类型不支持作为水印，支持的文件格式有：{string.Join(",", SupportImageExtensions)}");
                    return false;
                }
            }

            // 若 opacity 为 0, 则使用默认值
            if (Opacity <= 0)
                Opacity = 0.75f;
            else if (Opacity > 1) Opacity = 1;

            // 判断输出目录是否存在
            if (!string.IsNullOrEmpty(OutDir))
            {
                if (!Directory.Exists(OutDir))
                {
                    AnsiConsole.MarkupLine($"[red]输出目录 {OutDir} 不存在[/]");
                    if (!Silent)
                    {
                        // 提示新建输出目录
                        var createDir = AnsiConsole.Ask("是否新建输出目录？([springgreen1]Y/N[/])", "Y");
                        if (createDir.ToLower().Contains("y"))
                        {
                            Directory.CreateDirectory(OutDir);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine($"执行退出，没有找到目录 {OutDir}");
                            return false;
                        }
                    }
                }
            }

            // 验证添加水印的目录是否存在
            if (!File.Exists(TargetPath) && !Directory.Exists(TargetPath))
            {
                AnsiConsole.MarkupLine($"[red]未找到添加水印的文件或目录 {TargetPath}[/]");
                return false;
            }

            // 若指定了文件，同时也指定了 --grep，则提示 --grep 将失效
            if (File.Exists(TargetPath))
            {
                // 判断是否是图片
                var targetExtension = Path.GetExtension(TargetPath).ToLower();
                if (!SupportImageExtensions.Contains(targetExtension))
                {
                    AnsiConsole.MarkupLine($"{targetExtension} 文件类型不支持添加水印，支持的文件格式有：{string.Join(",", SupportImageExtensions)}");
                    return false;
                }

                if (!string.IsNullOrEmpty(Grep) && !Silent)
                {
                    // 输出提示
                    AnsiConsole.MarkupLine("[yellow]同时指定了 --path 和 --grep 参数，--grep 将不会生效");
                    AnsiConsole.WriteLine();
                }
            }

            return true;
        }

        private List<string> _specifiedPositions = new()
        {
            "left-top",
            "lef-center",
            "left-bottom",
            "center-top",
            "center-center",
            "center",
            "center-bottom",
            "right-top",
            "right-center",
            "right-bottom"
        };

        /// <summary>
        /// 验证位置
        /// </summary>
        /// <returns></returns>
        private bool ValidatePosition()
        {
            // 将字符串的值转换成特定值
            if (string.IsNullOrEmpty(Position)) return true;

            // 判断是否为指定格式
            if (_specifiedPositions.Contains(Position.ToLower()))
            {
                // 特定位置
                return true;
            }

            // 判断格式是否正确： x,y
            var positionFormatRegex = new Regex(@"(\d+)[,，-](\d+)");
            var match = positionFormatRegex.Match(Position);
            if (match.Success)
            {
                return true;
            }

            AnsiConsole.MarkupLine($"[red]--position 格式错误，示例: x,y 其中 x、y 为正整数,中间使用 ，或 - 号分隔[/]");
            return false;
        }

        /// <summary>
        /// 获取指定位置
        /// </summary>
        /// <param name="imageWidth"></param>
        /// <param name="imageHeight"></param>
        /// <returns></returns>
        public Point GetPosition(int imageWidth, int imageHeight)
        {
            if (string.IsNullOrEmpty(Position)) return new Point(0, 0);

            // 判断是否为指定格式
            if (_specifiedPositions.Contains(Position.ToLower()))
            {
                // 说明是特定位置
                Position = Position.ToLower();
                if (Position == "center") Position = "center-center";
                var positionArray = Position.Split("-");
                int positionX = 0;
                int positionY = 0;
                switch (positionArray[0])
                {
                    case "left":
                        positionX = 10;
                        break;
                    case "center":
                        positionX = imageWidth / 2;
                        break;
                    case "right":
                        positionX = imageWidth-10;
                        break;
                }
                switch (positionArray[1])
                {
                    case "top":
                        positionY = 10;
                        break;
                    case "center":
                        positionY = imageHeight / 2;
                        break;
                    case "bottom":
                        positionY = imageHeight-10;
                        break;
                }
                return new Point(positionX, positionY);
            }

            var positionFormatRegex = new Regex(@"(\d+)[,，-](\d+)");
            var match = positionFormatRegex.Match(Position);
            var x = int.Parse(match.Groups[1].Value);
            var y = int.Parse((match.Groups[2].Value));
            return new Point(x, y);
        }
    }
}
