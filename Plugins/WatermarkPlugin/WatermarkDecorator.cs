using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.WatermarkPlugin
{
    /// <summary>
    /// 水印装饰器
    /// </summary>
    internal abstract class WatermarkDecorator:Watermarker
    {
        public WatermarkDecorator(Watermarker watermarker):base(watermarker)
        {
            
        }
    }
}
