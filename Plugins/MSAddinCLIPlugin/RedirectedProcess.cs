using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDO.MSAddinCLIPlugin
{
    /// <summary>
    /// 重定向的 Process
    /// </summary>
    internal class RedirectedProcess : Process
    {
        public RedirectedProcess()
        {
            var startInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                RedirectStandardInput = true,// 接受来自调用程序的输入信息
                RedirectStandardOutput = true,// 由调用程序获取输出信息
                RedirectStandardError = true,// 重定向标准错误输出
                CreateNoWindow = true,// 不显示程序窗口
                StandardOutputEncoding = Encoding.Default,
                StandardErrorEncoding = Encoding.Default
            };

            this.StartInfo = startInfo;
            this.OutputDataReceived += P_OutputDataReceived;
            this.ErrorDataReceived += P_ErrorDataReceived;            
        }

        private bool _started = false;
        /// <summary>
        /// 启动进程
        /// </summary>
        /// <param name="workingDir"></param>
        /// <param name="fileName"></param>
        /// <param name="arguments"></param>
        /// <param name="background"></param>
        public void Start(string workingDir, string fileName, string arguments, bool background = false)
        {
            this.StartInfo.WorkingDirectory = workingDir;
            this.StartInfo.FileName = fileName;
            this.StartInfo.Arguments = arguments;
            this.Start();//启动程序

            if (!_started)
            {
                this.BeginOutputReadLine();
                this.BeginErrorReadLine();
                this.StandardInput.AutoFlush = true;
                _started = true;
            }

            if (!background)
            {
                this.WaitForExit();
            }
        }

        private void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            // 转码
            byte[] bytes = Encoding.Default.GetBytes(e.Data);
            bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, bytes);
            string formatString = Encoding.UTF8.GetString(bytes);
            Console.WriteLine(formatString);
        }

        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data)) return;
            // 转码
            byte[] bytes = Encoding.Default.GetBytes(e.Data);
            bytes = Encoding.Convert(Encoding.Default, Encoding.UTF8, bytes);
            string formatString = Encoding.UTF8.GetString(bytes);
            Console.WriteLine(formatString);
        }
    }
}
