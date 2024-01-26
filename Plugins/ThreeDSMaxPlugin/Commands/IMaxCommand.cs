using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Commands
{
    internal interface IMaxCommand
    {
        /// <summary>
        /// 注册命令
        /// </summary>
        /// <param name="maxCommand"></param>
        /// <param name="config"></param>
        void RegisterCommand(Command maxCommand, JsonNode config);
    }
}
