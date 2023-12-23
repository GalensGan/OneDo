using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    public class Keyword : XElement
    {
        public Keyword(XName commandWord) : base(KeyinLabels.XNamespace + KeyinLabels.Keyword)
        {
            this.SetAttributeValue(KeyinLabels.CommandWord, commandWord);
        }

        public Keyword(XName xName, params object?[] content) : base(xName, content)
        {
        }

        /// <summary>
        /// 获取或设置下一级命令表的 ID
        /// </summary>
        public string? SubtableRef
        {
            get
            {
                return this.Attribute(KeyinLabels.SubtableRef)?.Value;
            }
            set
            {
                this.SetAttributeValue(KeyinLabels.SubtableRef, value);
            }
        }

        /// <summary>
        /// 下一级命令表
        /// </summary>
        public SubKeyinTable? SubKeyinTable
        {
            get
            {
                var subtableRef = this.SubtableRef;
                if (string.IsNullOrEmpty(subtableRef)) return null;
                // 获取属性 ID 值为 subtableRef 的 SubKeyinTable
                var document = this.Document as KeyinDocument;
                var subKeyinTable = document?.KeyinTree?.SubKeyinTables?.Elements().FirstOrDefault(x => x.Attribute(KeyinLabels.ID)?.Value == subtableRef);
                return subKeyinTable as SubKeyinTable;
            }
        }

        /// <summary>
        /// 命令单词
        /// </summary>
        public string? CommandWord
        {
            get
            {
                return this.Attribute("CommandWord")?.Value;
            }
            set
            {
                this.SetAttributeValue("CommandWord", value);
            }
        }

        /// <summary>
        /// 命令类别
        /// </summary>
        public CommandClass CommandClass
        {
            get
            {
                var att = this.Attribute("CommandClass");
                if (att == null) return CommandClass.Inherit;
                return (CommandClass)Enum.Parse(typeof(CommandClass), att.Value);
            }
            set
            {
                this.SetAttributeValue("CommandClass", value);
            }
        }

        /// <summary>
        /// 获取或设置 keyword 的选项
        /// 若没有选项，则为空
        /// </summary>
        public Option? Option
        {
            get
            {
                return this.Element("Option") as Option;
            }
            set
            {
                var option = this.Element("Option");
                if (option == null)
                {
                    option = new Option();
                    this.Add(option);
                }
                option.SetAttributeValue("Required", value.Required);
                option.SetAttributeValue("Default", value.Default);
                option.SetAttributeValue("TryParse", value.TryParse);
                option.SetAttributeValue("Hidden", value.Hidden);
            }
        }
    }
}
