using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Utils
{
    internal static class XDocumentExtension
    {
        /// <summary>
        /// 使用带有命名空间的名称查询后代
        /// </summary>
        /// <param name="element"></param>
        /// <param name="localName"></param>
        /// <returns></returns>
        public static IEnumerable<XElement> DescendantsWithNSP(this XElement element, string localName)
        {
            var fullName = "{" + element.Name.NamespaceName + "}" + localName;
            return element.Descendants(fullName);
        }

        /// <summary>
        /// 转换成带有命名空间的 XName
        /// </summary>
        /// <param name="localName"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static XName ToXNameWithNSP(this string localName, XElement template)
        {
            return template.Name.Namespace + localName;
        }
    }
}
