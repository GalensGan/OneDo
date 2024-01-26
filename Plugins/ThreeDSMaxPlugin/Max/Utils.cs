using OpenMcdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max
{
    public class Utils
    {
        public static int ReadInt(BinaryReader reader)
        {
            return reader.ReadInt32();
        }

        /// <summary>
        /// 读取字符串
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static string ReadStr(BinaryReader reader)
        {
            int strLen = ReadInt(reader);
            byte[] strBytes = reader.ReadBytes(strLen);
            int nullPos = Array.IndexOf(strBytes,(byte)0);
            if (nullPos >= 0)
            {
                strBytes = strBytes[..nullPos];
            }
            return Encoding.Unicode.GetString(strBytes);
        }

        public static Dictionary<string, object> GetHeaders(BinaryReader reader, byte[] marker)
        {
            var headers = new Dictionary<string, object>();
            while (true)
            {
                byte[] buf = reader.ReadBytes(4);
                if (!buf.SequenceEqual(marker))
                {
                    reader.BaseStream.Seek(-4, SeekOrigin.Current);
                    break;
                }
                string head = ReadStr(reader);
                reader.BaseStream.Seek(4, SeekOrigin.Current);
                int count = ReadInt(reader);
                headers[head] = new { count = count };
            }
            return headers;
        }

        public static void ExtractFileProps(string maxFileName)
        {
            byte[] marker = new byte[] { 0x1e, 0x00, 0x00, 0x00 };
            using (var oleFile = new CompoundFile(maxFileName))
            {
                var stream = oleFile.RootStorage.GetStream("\x05DocumentSummaryInformation");
                byte[] bytes = stream.GetData();
                int idx = Array.IndexOf(bytes, marker);
                bytes = bytes[idx..];
                using (var reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    var headers = GetHeaders(reader, marker);
                    // TODO: Read properties
                }
            }
        }
    }
}
