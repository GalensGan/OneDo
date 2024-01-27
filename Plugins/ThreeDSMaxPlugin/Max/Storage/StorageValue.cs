using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.Storage
{
    public class StorageValue: StorageContainer
    {
        public string Value { get; set; }
        public string HexSpaced { get; set; }
        public string Utf16 { get; set; }
        public string Ascii { get; set; }
        public string Parsed { get; set; }

        /// <summary>
        /// 将 Value 转换成 Bytes
        /// </summary>
        public byte[] ValueBytes
        {
            get
            {
                return Enumerable.Range(0, Value.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(Value.Substring(x, 2), 16))
                         .ToArray();
            }
        }
    }
}
