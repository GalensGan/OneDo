using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.MinioPlugin.Http
{
    /// <summary>
    /// 进度报告
    /// </summary>
    public class ProgressReport
    {
        /// <summary>
        /// 进度 0-100
        /// </summary>
        public int Percentage { get; set; }

        /// <summary>
        /// 被传输的总字节数
        /// </summary>
        public long TotalBytesTransferred { get; set; }
    }
}
