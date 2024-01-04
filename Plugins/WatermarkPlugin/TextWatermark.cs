using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.WatermarkPlugin
{
    /// <summary>
    /// 文字水印
    /// </summary>
    internal class TextWatermark : WatermarkDecorator
    {
        const float WatermarkFontSize = 18f;

        public TextWatermark(Watermarker watermarker) : base(watermarker)
        {
        }

        public override bool Save(string savePath)
        {
            // 添加文字
            if (string.IsNullOrEmpty(Options.Text)) return base.Save(savePath);

            var fontFamilies = SystemFonts.GetByCulture(CultureInfo.CurrentCulture);
            var fontFamily = fontFamilies.FirstOrDefault(x => x.Name.Contains("宋"));
            var font = fontFamily.CreateFont(WatermarkFontSize);

            var fontSize = TextMeasurer.MeasureBounds(Options.Text, new TextOptions(font));

            // 当指定位置时，按指定位置
            List<Point> positions = GetWatermarkPositions(Image.Width - (int)fontSize.Width, Image.Height - (int)fontSize.Height, Options);

            // 添加文字水印
            foreach (var position in positions)
            {
                // 参考：https://github.com/SixLabors/ImageSharp.Drawing/discussions/124#discussioncomment-939873
                Image.Mutate(image =>
                {
                    var positionF = new PointF(position.X, position.Y);
                    image.SetDrawingTransform(Matrix3x2Extensions.CreateRotationDegrees(-Options.Angle, positionF))
                    .DrawText(Options.Text, font, new Rgba32(100, 0, 1, Options.Opacity), positionF);
                });
            }

            return base.Save(savePath);
        }

        /// <summary>
        /// 获取满布时的点
        /// </summary>
        /// <param name="imageWidth"></param>
        /// <param name="imageHeight"></param>
        /// <param name="space"></param>
        /// <returns></returns>
        public static List<Point> GetWatermarkPositions(int imageWidth, int imageHeight, WatermarkOption options)
        {
            List<Point> positions = new();
            if (!string.IsNullOrEmpty(options.Position))
            {
                positions.Add(options.GetPosition(imageWidth,imageHeight));
            }
            else if (options.Fill)
            {
                int space = 270;
                // 获取所有的位置
                if (imageWidth <= 2 * space && imageHeight <= 2 * space)
                {
                    // 仅在中间放一个
                    positions.Add(new Point(imageWidth / 2, imageHeight / 2));
                }
                else
                {
                    var xCount = imageWidth / 2 / space;
                    var yCount = imageHeight / 2 / space;

                    var startX = (int)(imageWidth / 2 - xCount * space);
                    var startY = (int)(imageHeight / 2 - yCount * space);
                    for (var xIndex = 0; xIndex <= xCount * 2; xIndex++)
                    {
                        for (var yIndex = 0; yIndex <= yCount * 2; yIndex++)
                        {
                            positions.Add(new Point(startX + xIndex * space, startY + yIndex * space));
                        }
                    }
                }
            }
            else // 添加默认值
            {
                positions.Add(new Point(10, 10));
            }

            return positions;
        }
    }
}
