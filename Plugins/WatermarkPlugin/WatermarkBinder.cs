using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Binding;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.WatermarkPlugin
{
    internal class WatermarkBinder : BinderBase<WatermarkOption>
    {
        private Option<bool> _silentOption;
        private Option<string> _positionOption;
        private Option<bool> _fillOption;

        private Option<string> _pathOption;
        private Option<string> _suffixOption;
        private Option<bool> _recursiveOption;

        private Option<string> _grepOption;
        private Option<float> _opacityOption;
        private Option<string> _outDirOption;

        private Option<float> _angleOption;
        private Option<string> _textOption;
        private Option<string> _imageOption;

        public WatermarkBinder(Option<bool> silentOption, Option<string> positionOption, Option<bool> fillOption,
            Option<string> pathOption, Option<string> suffixOption, Option<bool> recursiveOption,
            Option<string> grepOption, Option<float> opacityOption, Option<string> outDirOption,
            Option<float> angleOption, Option<string> textOption, Option<string> imageOption)
        {
            _silentOption = silentOption;
            _positionOption = positionOption;
            _fillOption = fillOption;
            _pathOption = pathOption;
            _suffixOption = suffixOption;
            _recursiveOption = recursiveOption;
            _grepOption = grepOption;
            _opacityOption = opacityOption;
            _outDirOption = outDirOption;
            _angleOption = angleOption;
            _textOption = textOption;
            _imageOption = imageOption;
        }

        protected override WatermarkOption GetBoundValue(BindingContext bindingContext)
        {
            return new WatermarkOption()
            {
                Silent = bindingContext.ParseResult.GetValueForOption(_silentOption),
                Position = bindingContext.ParseResult.GetValueForOption(_positionOption),
                Fill = bindingContext.ParseResult.GetValueForOption(_fillOption),
                TargetPath = bindingContext.ParseResult.GetValueForOption(_pathOption),
                Suffix = bindingContext.ParseResult.GetValueForOption(_suffixOption),
                Recursive = bindingContext.ParseResult.GetValueForOption(_recursiveOption),
                Grep = bindingContext.ParseResult.GetValueForOption(_grepOption),
                Opacity = bindingContext.ParseResult.GetValueForOption(_opacityOption),
                OutDir = bindingContext.ParseResult.GetValueForOption(_outDirOption),
                Angle = bindingContext.ParseResult.GetValueForOption(_angleOption),
                Text = bindingContext.ParseResult.GetValueForOption(_textOption),
                Image = bindingContext.ParseResult.GetValueForOption(_imageOption)                
            };
        }
    }
}
