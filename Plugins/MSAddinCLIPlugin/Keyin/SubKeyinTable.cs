using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    public class SubKeyinTable : XElement
    {
        public SubKeyinTable(string id) : base(KeyinLabels.XNamespace + KeyinLabels.SubKeyinTable, new XAttribute(KeyinLabels.ID, id))
        {
        }

        public SubKeyinTable(XName xName, params object?[] content) : base(xName, content)
        {
        }

        public string? ID
        {
            get
            {
                return this.Attribute(KeyinLabels.ID)?.Value;
            }
            set
            {
                this.SetAttributeValue(KeyinLabels.ID, value);
            }
        }
    }
}
