﻿using SixLabors.Fonts;
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
    internal class ImageWatermark : WatermarkDecorator
    {
        public ImageWatermark(Watermarker watermarker) : base(watermarker)
        {
        }

        public override bool Save(string savePath)
        {
            // 添加图片水印
            if (string.IsNullOrEmpty(Options.Image)) return base.Save(savePath);

            // 水印图片
            var watermarkImage = Image.Load<Rgba32>(Options.Image);
            watermarkImage.Mutate(x =>
            {
                x.BackgroundColor(new Rgba32(0, 0, 0, 0))
                .Rotate(-Options.Angle);
            });
            // 若水印太大，需要对水印进等比缩小
            double limitRate = 0.2;
            double widthRate = watermarkImage.Width / (limitRate * Image.Width);
            double heightRate = watermarkImage.Height / (limitRate * Image.Height);
            if (widthRate > 1 || heightRate > 1)
            {
                double scale = Math.Min(1 / widthRate, 1 / heightRate);
                watermarkImage.Mutate(x => x.Resize((int)(watermarkImage.Width * scale), (int)(watermarkImage.Height * scale)));
            }

            // 获取水印位置
            List<Point> positions = TextWatermark.GetWatermarkPositions(Image.Width - watermarkImage.Width, Image.Height - watermarkImage.Height, Options);

            foreach (var position in positions)
            {
                // 判断图片是否超出范围
                if (position.X + watermarkImage.Width > Image.Width || position.Y + watermarkImage.Height > Image.Height) continue;

                Image.Mutate(image =>
                {
                    image.DrawImage(watermarkImage, position, Options.Opacity);
                });
            }

            return base.Save(savePath);
        }
    }
}
