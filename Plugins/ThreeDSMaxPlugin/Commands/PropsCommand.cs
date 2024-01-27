using OneDo.ThreeDSMaxPlugin.Max;
using OneDo.ThreeDSMaxPlugin.Max.SummaryInfo;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Commands
{
    internal class PropsCommand : IMaxCommand
    {
        public void RegisterCommand(Command maxCommand, JsonNode config)
        {
            var propsCommand = new Command("props", "显示 max 文件中所有的属性");
            maxCommand.Add(propsCommand);

            var fileOption = new Option<string>("--file", "max 文件路径")
            {
                IsRequired = true
            };
            fileOption.AddAlias("-f");
            propsCommand.Add(fileOption);

            propsCommand.SetHandler(filePath =>
            {
                // 验证文件路径
                if (!StreamCommand.ValidateFilePath(filePath)) return;

                var results = new MaxProperties(filePath);
                // 使用 Json 序列化 List<StorageContainer>
                string jsonString = JsonSerializer.Serialize(results, Max.Utils.JsonSerializerOptions);
                AnsiConsole.WriteLine(jsonString);
            }, fileOption);
        }
    }
}
