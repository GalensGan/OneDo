using Microsoft.VisualBasic;
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
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var wolCommand = new Command("wol", "唤醒电脑");
            rootCommand.Add(wolCommand);

            var nameArg = new Argument<List<string>>("name", "指定要运行的唤醒配置名称")
            {
                Arity = ArgumentArity.ZeroOrOne
            };
            wolCommand.Add(nameArg);
            var ipOption = new Option<string>("--ip", "指定要唤醒的电脑 IP 地址");
            wolCommand.Add(ipOption);
            var macOption = new Option<string>("--mac", "指定要唤醒的电脑 MAC 地址");
            wolCommand.Add(macOption);
            var portOption = new Option<int>("--port", "指定端口号");
            portOption.AddAlias("-p");
            wolCommand.Add(portOption);

            wolCommand.SetHandler((names, ip, mac, port) =>
            {
                var name = names.FirstOrDefault();

                // 判断参数
                if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(ip))
                {
                    AnsiConsole.MarkupLine($"[red]请指定 name 或者 ip[/]");
                    return;
                }

                // 判断端口号范围是否正确
                if (port < 0 || port > 65535)
                {
                    AnsiConsole.MarkupLine($"[red]端口号范围不正确，请输入: 0~65535 之间的整数[/]");
                    return;
                }

                // name 不为空，执行 name 数据
                WolByName(name, config, port);

                // ip 不为空，执行 ip 数据
                WolByIp(ip, mac, port);

            }, nameArg, ipOption, macOption, portOption);

            // 添加展示命令的列表
            var listCommand = new Command("list", "展示所有的唤醒配置");
            listCommand.AddAlias("ls");
            wolCommand.Add(listCommand);
            listCommand.SetHandler(() =>
            {
                var list = new ListPluginConfs(config, "wols", new List<FieldMapper>()
                {
                    new FieldMapper("name","名称"),
                    new FieldMapper("description","描述"),
                    new FieldMapper( "ip","IP地址"),
                    new FieldMapper("mac","MAC地址"),
                    new FieldMapper("port","端口号"),
                });
                list.Show();
            });
        }

        private bool WolByName(string name, JsonNode config, int port)
        {
            if (!string.IsNullOrEmpty(name))
            {
                // 查找所有 wol
                if (!JsonHelper.GetJsonArray<WolModel>(config, "wols", out var wolConfigs)) return false;
                // 查找特定的 name
                var targetWols = wolConfigs.FindAll(x => x.Name.ToLower() == name.ToLower());
                if (targetWols.Count == 0)
                {
                    AnsiConsole.MarkupLine($"[red]未能找到 WOL 配置: {name}[/]");
                    return false;
                }

                // 执行命令
                foreach (var wol in targetWols)
                {
                    var client = new WakeOnLanClient(wol.IP, port > 0 ? port : wol.Port);
                    client.SendMagicPacket(wol.MAC);
                    AnsiConsole.MarkupLine($"[springgreen1]已唤醒 {wol.IP}:{wol.Port} --> {wol.MAC}[/]");
                }
            }

            return true;
        }

        private bool WolByIp(string ip, string mac, int port)
        {
            if (!string.IsNullOrEmpty(ip))
            {
                if (string.IsNullOrEmpty(mac))
                {
                    AnsiConsole.MarkupLine($"[red]请指定 --mac[/]");
                    return false;
                }

                if (port == 0) port = 9;

                var client = new WakeOnLanClient(ip, port);
                client.SendMagicPacket(mac);
                AnsiConsole.MarkupLine($"[springgreen1]已唤醒 {ip}:{port} --> {mac}[/]");
            }

            return true;
        }
    }
}
