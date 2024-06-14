using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.MinioPlugin.Http
{
    internal class HttpProgress : IProgress<ProgressReport>
    {
        private ProgressTask _progressTask;
        public HttpProgress(ProgressTask progressTask)
        {
            _progressTask = progressTask;
        }

        public void Report(ProgressReport value)
        {
            _progressTask.Value = value.Percentage;
        }
    }
}
