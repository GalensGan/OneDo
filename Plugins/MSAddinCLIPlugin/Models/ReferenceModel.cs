using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDO.MSAddinCLIPlugin.Models
{
    internal class ReferenceModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<string> References { get; set; }
    }
}
