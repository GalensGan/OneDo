using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    public class SubKeyinTables : XElement
    {
        public SubKeyinTables() : base(KeyinLabels.XNamespace + KeyinLabels.SubKeyinTables)
        {
        }

        public SubKeyinTables(XName xName, params object?[] content) : base(xName, content)
        {
        }
    }
}
