using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    public class KeyinHandler : XElement
    {
        public KeyinHandler(string keyin, string function) : base(KeyinLabels.XNamespace + KeyinLabels.KeyinHandler,
            new XAttribute(KeyinLabels.Keyin, keyin),
            new XAttribute(KeyinLabels.Function, function))
        {
        }

        public KeyinHandler(XName xName, params object?[] content) : base(xName, content)
        {
        }
    }
}
