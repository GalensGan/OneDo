using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.Decoders
{
    public class NullDecoder : IDecoder
    {
        public object Decode(byte[] val)
        {
            return null;
        }
    }
}
