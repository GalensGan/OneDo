using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OneDo.Plugin
{
    /// <summary>
    /// 插件接口
    /// </summary>
    public interface IPlugin
    {
        /// <summary>
        /// 注册命令
        /// </summary>
        /// <param name="rootCommand"></param>
        /// <returns></returns>
        void RegisterCommand(RootCommand rootCommand, JsonNode config);
    }
}
