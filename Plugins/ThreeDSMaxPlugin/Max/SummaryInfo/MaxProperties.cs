using OpenMcdf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.SummaryInfo
{
    public class MaxProperties : List<HeaderInfo>
    {       
        public MaxProperties(string maxFileName)
        {
           AddRange(ExtractFileProps(maxFileName));
        }

        #region 解析算法
        private List<HeaderInfo> ExtractFileProps(string maxFileName)
        {
            byte[] marker = [0x1e, 0x00, 0x00, 0x00];
            var compoundFile = new CompoundFile(maxFileName);

            var stream = compoundFile.RootStorage.GetStream("\u0005DocumentSummaryInformation");
            byte[] bytes = stream.GetData();
            compoundFile.Close();

            int idx = IndexOf(bytes, marker);
            byte[] subBytes = new byte[bytes.Length - idx];
            Array.Copy(bytes, idx, subBytes, 0, subBytes.Length);
            var memory = new MemoryStream(subBytes);

            // 获取头信息
            var headers = GetHeaders(memory, marker);
            // 解析属性
            headers = ReadProps(memory, headers);
            return headers;
        }

        private int IndexOf(byte[] array, byte[] pattern)
        {
            for (int i = 0; i < array.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (array[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    return i;
                }
            }
            return -1;
        }

        private int ReadInt(MemoryStream bio)
        {
            byte[] bytes = new byte[4];
            bio.Read(bytes, 0, 4);
            return BitConverter.ToInt32(bytes, 0);
        }

        private string ReadStr(MemoryStream bio)
        {
            int strLen = ReadInt(bio);
            byte[] bytes = new byte[strLen];
            bio.Read(bytes, 0, strLen);
            int nullIndex = Array.IndexOf(bytes, (byte)0);
            return Encoding.UTF8.GetString(bytes, 0, nullIndex);
        }

        private void Unread(MemoryStream bio, byte[] chunk)
        {
            bio.Position -= chunk.Length;
        }

        private List<HeaderInfo> GetHeaders(MemoryStream bio, byte[] marker)
        {
            byte[] delim = new byte[] { 0x03, 0x00, 0x00, 0x00 };
            var headers = new List<HeaderInfo>();
            while (true)
            {
                byte[] buf = new byte[4];
                bio.Read(buf, 0, 4);
                if (!buf.SequenceEqual(marker))
                {
                    Unread(bio, buf);
                    break;
                }
                string head = ReadStr(bio);
                bio.Read(buf, 0, 4);
                if (!buf.SequenceEqual(delim))
                {
                    throw new Exception("Delimiter does not match");
                }
                int count = ReadInt(bio);
                headers.Add(new HeaderInfo()
                {
                    Key = head,
                    Count = count
                });               
            }
            return headers;
        }

        private List<HeaderInfo> ReadProps(MemoryStream bio, List<HeaderInfo> headers)
        {
            byte[] propStart = new byte[] { 0x1e, 0x10, 0x00, 0x00 };
            byte[] buf = new byte[4];
            bio.Read(buf, 0, 4);
            if (!buf.SequenceEqual(propStart))
            {
                throw new Exception("Property start does not match");
            }
            int propCount = ReadInt(bio);
            foreach (var head in headers)
            {
                var items = new List<string>();
                head.Items = items;

                int count = head.Count;
                int i = 0;
                while (i < count)
                {
                    string item = ReadStr(bio);
                    items.Add(item);
                    i += 1;
                }
                propCount -= i;
            }
            if (propCount != 0)
            {
                throw new Exception("The actual number of properties does not match the one declared");
            }
            return headers;
        }


        #endregion

        #region 外部调用
        /// <summary>
        /// 获取外部依赖文件
        /// </summary>
        /// <returns></returns>
        public HeaderInfo? GetExternalDependencies()
        {
            return this.Find(x => x.Key == "External Dependencies");
        }
        #endregion
    }
}
