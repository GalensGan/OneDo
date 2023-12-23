using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    public class Option : XElement
    {
        public Option() : base(KeyinLabels.XNamespace + KeyinLabels.Option)
        {
        }

        public Option(XName xName, params object?[] content) : base(xName, content)
        {
        }

        private bool GetBoolAttribute(string name)
        {
            var att = this.Attribute(name);
            if (att == null) return false;
            return att.Value.ToLower() == "true";
        }

        public bool Required
        {
            get
            {
                return GetBoolAttribute(KeyinLabels.Required);
            }
            set
            {
                this.SetAttributeValue(KeyinLabels.Required, value);
            }
        }

        public bool Default
        {
            get
            {
                return GetBoolAttribute(KeyinLabels.Default);
            }
            set
            {
                this.SetAttributeValue(KeyinLabels.Default, value);
            }
        }

        public bool TryParse
        {
            get
            {
                return GetBoolAttribute(KeyinLabels.TryParse);
            }
            set
            {
                this.SetAttributeValue(KeyinLabels.TryParse, value);
            }
        }

        public bool Hidden
        {
            get
            {
                return GetBoolAttribute(KeyinLabels.Hidden);
            }
            set
            {
                this.SetAttributeValue(KeyinLabels.Hidden, value);
            }
        }
    }
}
