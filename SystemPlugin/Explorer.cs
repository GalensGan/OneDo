using OneDo.Plugin;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SystemPlugin
{
    /// <summary>
    /// 打开当前目录或者文件
    /// </summary>
    public class Explorer : IPlugin
    {
        public bool RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            return true;
        }
    }
}
