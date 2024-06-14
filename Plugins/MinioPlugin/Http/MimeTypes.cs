using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.CommandLine.Help.HelpBuilder;

namespace OneDo.MinioPlugin.Http
{
    internal class MimeTypes
    {
        private static Dictionary<string, string> _mimeTypes = new()
        {
            {"default", "application/octet-stream" },
            {"jpg", "image/jpeg" },
            {"gif", "image/gif" },
            {"jfif", "image/jpeg" },
            {"png", "image/png" },
            {"ico", "image/x-icon" },
            {"jpeg", "image/jpeg" },
            {"wbmp", "image/vnd.wap.wbmp" },
            {"fax", "image/fax" },
            {"net", "image/pnetvue" },
            {"jpe", "image/jpeg" },
            {"rp", "image/vnd.rn-realpix" }
        };

        /// <summary>
        /// 根据文件名获取 ContentType
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName).TrimStart('.').ToLower();
            return _mimeTypes.ContainsKey(ext) ? _mimeTypes[ext] : _mimeTypes["default"];
        }
    }
}
