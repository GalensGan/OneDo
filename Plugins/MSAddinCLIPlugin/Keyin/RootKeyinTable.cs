using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    public class RootKeyinTable : XElement
    {
        public RootKeyinTable() : base(KeyinLabels.XNamespace + KeyinLabels.RootKeyinTable)
        {
        }

        public RootKeyinTable(XName xName, params object?[] content) : base(xName, content)
        {
        }

        /// <summary>
        /// 获取 keyword,若不存在，返回空
        /// </summary>
        /// <param name="commandWord"></param>
        /// <returns></returns>
        public Keyword? GetKeyword(string commandWord)
        {
            return this.Elements().FirstOrDefault(x => x.Attribute(KeyinLabels.CommandWord)?.Value.ToLower() == commandWord.ToLower()) as Keyword;
        }
    }
}
