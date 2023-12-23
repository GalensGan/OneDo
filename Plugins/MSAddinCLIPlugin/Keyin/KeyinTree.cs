using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    /// <summary>
    /// KeyinTree 类
    /// </summary>
    public class KeyinTree : XElement
    {
        public KeyinTree() : base(KeyinLabels.XNamespace + KeyinLabels.KeyinTree)
        {
            this.Add(new XAttribute(KeyinLabels.xmlns, KeyinLabels.XNamespace));
        }

        public KeyinTree(XName xName, params object?[] content) : base(xName, content)
        {
        }

        public RootKeyinTable? RootKeyinTable => this.Element(KeyinLabels.XNamespace + KeyinLabels.RootKeyinTable) as RootKeyinTable;

        public SubKeyinTables? SubKeyinTables => this.Element(KeyinLabels.XNamespace + KeyinLabels.SubKeyinTables) as SubKeyinTables;

        public KeyinHandlers? KeyinHandlers => this.Element(KeyinLabels.XNamespace + KeyinLabels.KeyinHandlers) as KeyinHandlers;
    }
}
