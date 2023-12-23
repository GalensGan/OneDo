using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    public class KeyinHandlers : XElement
    {
        public KeyinHandlers() : base(KeyinLabels.XNamespace + KeyinLabels.KeyinHandlers)
        {
        }

        public KeyinHandlers(XName xName, params object?[] content) : base(xName, content)
        {
        }
    }
}
