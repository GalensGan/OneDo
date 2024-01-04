using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.WatermarkPlugin
{
    internal class Watermarker
    {
        public Image Image { get; private set; }
        public WatermarkOption Options { get; private set; }

        public Watermarker(string imagePath,WatermarkOption options)
        {
            Image = Image.Load(imagePath);
            Options = options;
        }

        private Watermarker _origin;
        /// <summary>
        /// 子类继承使用
        /// </summary>
        protected Watermarker(Watermarker watermarker)
        {
            _origin = watermarker;
            Image = watermarker.Image;
            Options = watermarker.Options;
        }

        /// <summary>
        /// 保存图片
        /// 请最后再调用该逻辑
        /// </summary>
        /// <param name="savePath"></param>
        public virtual bool Save(string savePath)
        {
            if(_origin ==null) Image.Save(savePath);
            else _origin.Save(savePath);
            return true;
        }
    }
}
