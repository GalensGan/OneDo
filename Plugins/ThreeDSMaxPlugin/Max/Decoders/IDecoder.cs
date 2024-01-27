using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.Decoders
{
    public interface IDecoder
    {
        object Decode(byte[] val);
    }
}
