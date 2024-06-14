using OneDo.Plugin;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace DevOpsPlugin
{
    internal class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var devOpsServerCommand = new Command("dev", "开发运维管理");
            rootCommand.AddCommand(devOpsServerCommand);

            #region 让服务器执行命令
            var runCommand = new Command("run", "让服务器执行特定命令");
            var nameOption = new Option<string>("--name", "指定配置名称");
            nameOption.AddAlias("-n");
            runCommand.AddOption(nameOption);
            runCommand.SetHandler(() => { });
            #endregion

            #region 向服务器发送文件
            var sendFileCommand = new Command("send-file", "发送文件");
            devOpsServerCommand.Add(sendFileCommand);
            sendFileCommand.Add(nameOption);
            sendFileCommand.SetHandler(() => { });
            #endregion

            #region 服务器端
            var serviceOption = new Option<bool>("--server", "启动服务端");
            serviceOption.AddAlias("-s");
            devOpsServerCommand.Add(serviceOption);
            devOpsServerCommand.SetHandler(() => {
                // 使用 tcpListener 监听端口

            });
            #endregion
        }
    }
}
