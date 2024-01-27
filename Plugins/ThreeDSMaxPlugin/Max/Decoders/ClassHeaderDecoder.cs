using OneDo.ThreeDSMaxPlugin.Max.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.Decoders
{
    public class ClassHeaderDecoder : IDecoder
    {
        public object Decode(byte[] val)
        {
            if (val.Length != 16)
            {
                throw new ArgumentException("Length of a class header string must be 16");
            }

            int dllIndex = BitConverter.ToInt32(val, 0);
            string[] classId = new string[3];
            for (int i = 0; i < 3; i++)
            {
                classId[i] = BitConverter.ToInt32(val, 4 + i * 4).ToString("X");
            }
            string superClassId = BitConverter.ToInt32(val, 12).ToString("X");
            return new ClassHeader(dllIndex, classId, superClassId);
        }
    }
}
