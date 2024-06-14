using OneDo.Plugin;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DeployPlugin
{
    /// <summary>
    /// 程序部署插件
    /// </summary>
    internal class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var deployCommand = new Command("deploy", "将程序部署到服务器");
            rootCommand.Add(deployCommand);

            var tokenCommand = new Command("token", "授权相关操作");
            deployCommand.Add(tokenCommand);
            // 创建 token
            var addCommand = new Command("add", "创建token");
            deployCommand.Add(addCommand);
            addCommand.SetHandler(() =>
            {

            });

            var removeTokenCommand = new Command("remote", "移除token");
        }
    }
}
