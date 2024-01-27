using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.Storage
{
    public class Header
    {
        public string Idn { get; set; }
        public int Length { get; set; }
        public StorageType StorageType { get; set; }
        public string StorageTypeName { get; set; }
    }
}
