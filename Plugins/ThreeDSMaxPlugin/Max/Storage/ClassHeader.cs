using OneDo.ThreeDSMaxPlugin.Max.Decoders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.Storage
{
    public class ClassHeader
    {
        public int DllIndex { get; set; }
        public string[] ClassId { get; set; }
        public string SuperClassId { get; set; }

        public ClassHeader(int dllIndex, string[] classId, string superClassId)
        {
            DllIndex = dllIndex;
            ClassId = classId;
            SuperClassId = superClassId;
        }

        /// <summary>
        /// 解码成 <see cref="ClassHeader"/>
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static ClassHeader? Decode(byte[] val)
        {
            return new ClassHeaderDecoder().Decode(val) as ClassHeader;
        }
    }
}
