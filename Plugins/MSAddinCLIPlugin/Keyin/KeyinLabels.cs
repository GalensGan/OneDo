using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    public class KeyinLabels
    {
        public static XNamespace XNamespace = (XNamespace)"http://www.bentley.com/schemas/1.0/MicroStation/AddIn/KeyinTree.xsd";
        public const string KeyinTree = "KeyinTree";
        public const string SubKeyinTables = "SubKeyinTables";
        public const string SubKeyinTable = "SubKeyinTable";
        public const string RootKeyinTable = "RootKeyinTable";
        public const string ID = "ID";
        public const string xmlns = "xmlns";
        public const string Keyword="Keyword";
        public const string Option = "Option";
        public const string CommandWord = "CommandWord";
        public const string SubtableRef = "SubtableRef";
        public const string KeyinHandlers = "KeyinHandlers";
        public const string KeyinHandler = "KeyinHandler";

        public const string Required = "Required";
        public const string Default = "Default";
        public const string TryParse = "TryParse";
        public const string Hidden = "Hidden";
        public const string Keyin = "Keyin";
        public const string Function = "Function";
    }
}
