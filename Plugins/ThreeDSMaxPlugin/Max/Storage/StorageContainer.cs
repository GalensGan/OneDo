using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.Storage
{
    public class StorageContainer
    {
        public Header Header { get; set; }
        public List<StorageContainer> Childs { get; set; } = new List<StorageContainer>();
        public int Count
        {
            get { return Childs.Count; }
        }
    }
}
