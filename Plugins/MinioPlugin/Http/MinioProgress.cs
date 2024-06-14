using Spectre.Console;

namespace OneDo.MinioPlugin.Http
{
    internal class MinioProgress : IProgress<Minio.DataModel.ProgressReport>
    {
        private ProgressTask _progressTask;
        public MinioProgress(ProgressTask progressTask)
        {
            _progressTask = progressTask;
        }

        public void Report(Minio.DataModel.ProgressReport value)
        {
            _progressTask.Value = value.Percentage;
        }
    }
}
