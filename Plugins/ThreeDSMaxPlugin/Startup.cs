using OneDo.Plugin;
using OneDo.ThreeDSMaxPlugin.Commands;
using OneDo.Utils;
using OpenMcdf;
using Spectre.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace OneDo.ThreeDSMaxPlugin
{
    public class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var maxCommand = new Command("max", "提供 3D Studio Max 解析和操作的工具");
            rootCommand.Add(maxCommand);

            List<IMaxCommand> commands = new()
            {
                new ArchiveCommand(),
                new StreamCommand()
            };
            commands.ForEach(x => x.RegisterCommand(maxCommand, config));
        }
    }
}
