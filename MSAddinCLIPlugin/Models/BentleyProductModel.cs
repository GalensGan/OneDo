using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDO.MSAddinCLIPlugin.Models
{
    internal class BentleyProductModel
    {
        public List<string> ProductNames { get; set; }
        public string RegistryKey { get; set; }
        public string InstallPath { get; set; }
    }
}
