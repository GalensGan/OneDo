using OneDo.Plugin;
using OneDo.Utils;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OneDo.WakeOnLanPlugin
{
    /// <summary>
    /// 注册 WOL 命令
    /// </summary>
    public class Startup : IPlugin
    {
        public bool RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var wolCommand = new Command("wol", "唤醒电脑");
            rootCommand.Add(wolCommand);

            var nameArg = new Argument<string>("name", "指定要运行的唤醒配置名称");
            wolCommand.Add(nameArg);
            var ipOption = new Option<string>("--ip", "指定要唤醒的电脑 IP 地址");
            wolCommand.Add(ipOption);
            var macOption = new Option<string>("--mac", "指定要唤醒的电脑 MAC 地址");
            macOption.AddAlias("-m");
            wolCommand.Add(macOption);
            var portOption = new Option<int>("--port", "指定端口号")
            {
                IsRequired = false,
            };
            portOption.AddAlias("-p");
            portOption.SetDefaultValue(9);
            wolCommand.Add(portOption);

            wolCommand.SetHandler((name, ip, mac, port) =>
            {
                // 判断参数
                if(string.IsNullOrEmpty(name) && string.IsNullOrEmpty(ip))
                {
                    AnsiConsole.MarkupLine($"[red]请指定 name 或者 ip[/]");
                    return;
                }

                // name 不为空，执行 name 数据
                if(!string.IsNullOrEmpty(name))
                {
                    // 查找所有 wol
                    JsonHelper.GetJsonArray<WolModel>(config,"wols",out var wolConfigs);
                    // 查找特定的 name
                    var targetWols = wolConfigs.FindAll(x => x.Name.ToLower() == name.ToLower());
                    if (targetWols.Count == 0)
                    {
                        AnsiConsole.MarkupLine($"[red]未能找到 WOL 配置: {name}[/]");
                        return;
                    }

                    // 执行命令
                    foreach(var wol in targetWols)
                    {
                        var client = new WakeOnLanClient(wol.IP, port);
                        client.SendMagicPacket(wol.MAC);
                        AnsiConsole.MarkupLine($"[springgreen1]已唤醒 {wol.IP}:{wol.Port}/{wol.MAC}[/]");
                    }
                }

                // ip 不为空，执行 ip 数据
                if (!string.IsNullOrEmpty(ip))
                {
                    if (string.IsNullOrEmpty(mac))
                    {
                        AnsiConsole.MarkupLine($"[red]请指定 --mac[/]");
                        return;
                    }

                    // 判断端口号范围是否正确
                    if(port<0 || port > 65535)
                    {
                        AnsiConsole.MarkupLine($"[red]端口号范围不正确，请输入: 0~65535 之间的整数[/]");
                        return;
                    }

                    if (port == 0) port = 9;

                    var client = new WakeOnLanClient(ip, port);
                    client.SendMagicPacket(mac);
                    AnsiConsole.MarkupLine($"[springgreen1]已唤醒 {ip}:{port}/{mac}[/]");
                }

            }, nameArg, ipOption, macOption, portOption);

            // 添加展示命令的列表
            var listCommand = new Command("list", "展示所有的唤醒配置");
            listCommand.AddAlias("ls");
            wolCommand.Add(listCommand);
            listCommand.SetHandler(() =>
            {
                var list = new ListPluginConfs(config, "wols", new Dictionary<string, string>()
                {
                    { "name","名称"},
                    { "description","描述" },
                    { "ip","IP地址"},
                    { "mac","MAC地址"},
                    { "port","端口号"},
                });
                list.Show();
            });

            return true;
        }
    }
}
