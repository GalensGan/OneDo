using OneDo.ThreeDSMaxPlugin.Max.Storage;
using OneDo.Utils;
using OpenMcdf;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace OneDo.ThreeDSMaxPlugin.Commands
{
    internal class StreamCommand : IMaxCommand
    {
        public void RegisterCommand(Command maxCommand, JsonNode config)
        {
            var streamCommand = new Command("stream", "max 流相关的操作");
            maxCommand.Add(streamCommand);

            var fileOption = new Option<string>("--file", "max 文件")
            {
                IsRequired = true
            };
            fileOption.AddAlias("-f");

            #region 获取可使用流的名称
            var listNamesCommand = new Command("list", "列出可获取的流");
            listNamesCommand.AddAlias("ls");
            streamCommand.Add(listNamesCommand);

            listNamesCommand.Add(fileOption);

            listNamesCommand.SetHandler((filePath) =>
            {
                if (!ValidateFilePath(filePath)) return;

                using CompoundFile cf = new(filePath);
                List<CFItem> cFItems = cFItems = GetCFItems(cf);

                // 显示结果
                AnsiConsole.MarkupLine($"Available Entries:");
                AnsiConsole.WriteLine();

                // 向用户展示数据
                JsonArray showResults = new JsonArray();
                foreach (var cfItem in cFItems)
                {
                    showResults.Add(new JsonObject()
                    {
                        { nameof(cfItem.Name),cfItem.Name},
                        { nameof(cfItem.Size),cfItem.Size},
                        { nameof(cfItem.CLSID),cfItem.CLSID},
                        { nameof(cfItem.IsStream),cfItem.IsStream},
                        { nameof(cfItem.IsStorage),cfItem.IsStorage},
                        { nameof(cfItem.IsRoot),cfItem.IsRoot},
                        { nameof(cfItem.CreationDate),cfItem.CreationDate.ToString("g")},
                        { nameof(cfItem.ModifyDate),cfItem.ModifyDate.ToString("g")},
                    });
                }
                var listShow = new ListPluginConfs(showResults, new List<FieldMapper>()
                {
                    new("Name"),
                    new("Size","Size (byte)"),
                    new("CLSID"),
                    new("IsStream"),
                    new("IsStorage"),
                    new("IsRoot"),
                    new("CreationDate"),
                    new("ModifyDate")
                });
                listShow.Show();
                AnsiConsole.WriteLine();
            }, fileOption);
            #endregion

            #region Dump
            var dumpCommand = new Command("dump", "dump 流");
            streamCommand.Add(dumpCommand);

            dumpCommand.Add(fileOption);
            var streamNameOption = new Option<string>("--name", "导出名称：")
            {
                IsRequired = true
            };
            streamNameOption.AddAlias("-n");
            dumpCommand.Add(streamNameOption);
            var outOption = new Option<string>("--out", "保存的文件名");
            outOption.AddAlias("-o");
            dumpCommand.AddOption(outOption);

            dumpCommand.SetHandler((filePath, streamName, outFile) =>
            {
                if (!ValidateFilePath(filePath)) return;

                // 判断流是否存在
                using CompoundFile cf = new(filePath);
                if (!ValidateStreamName(cf, streamName)) return;

                // 读取流
                CFStream stream = cf.RootStorage.GetStream(streamName);
                byte[] bytes = stream.GetData();
                // 保存流
                SaveStream(outFile, filePath, streamName, bytes);

            }, fileOption, streamNameOption, outOption);
            #endregion

            #region Parse
            var parseCommand = new Command("parse", "解析 max 文件");
            streamCommand.Add(parseCommand);

            parseCommand.Add(fileOption);
            parseCommand.Add(streamNameOption);

            parseCommand.SetHandler((filePath, streamName) =>
            {
                // 判断文件是否存在
                if (!ValidateFilePath(filePath)) return;

                // 判断流是否存在
                using CompoundFile cf = new(filePath);
                if (!ValidateStreamName(cf, streamName)) return;

                List<StorageContainer> containers = BinaryReaderHelper.StorageParser(cf, streamName);

                // 使用 Json 序列化 List<StorageContainer>
                string jsonString = JsonSerializer.Serialize(containers, Max.Utils.JsonSerializerOptions);
                AnsiConsole.WriteLine(jsonString);
            }, fileOption, streamNameOption);
            #endregion
        }

        /// <summary>
        /// 验证文件路径
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool ValidateFilePath(string filePath)
        {
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine($"[red]max 文件：{filePath} 不存在[/]");
                return false;
            }
            if (!Path.HasExtension(".max"))
            {
                AnsiConsole.MarkupLine($"[red]文件：{filePath} 不是 max 文件[/]");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 获取CFItem
        /// </summary>
        /// <param name="cf"></param>
        /// <returns></returns>
        public static List<CFItem> GetCFItems(CompoundFile cf)
        {
            List<CFItem> cFItems = [];
            cf.RootStorage.VisitEntries(cfItem =>
            {
                cFItems.Add(cfItem);
            }, true);

            return cFItems;
        }

        /// <summary>
        /// 验证是否存在 StreamName
        /// </summary>
        /// <param name="cf"></param>
        /// <param name="streamName"></param>
        /// <returns></returns>
        public static bool ValidateStreamName(CompoundFile cf, string streamName)
        {
            List<CFItem> cfItems = GetCFItems(cf);
            var cfItem = cfItems.Find(x => x.Name.Equals(streamName, StringComparison.CurrentCultureIgnoreCase));
            if (cfItem == null)
            {
                AnsiConsole.MarkupLine($"[red]文件中不存在流 {streamName}[/]");
                return false;
            }

            return true;
        }
        /// <summary>
        /// 保存流到文件
        /// </summary>
        /// <param name="outPath"></param>
        /// <param name="cFStream"></param>
        /// <returns></returns>
        private void SaveStream(string outPath, string maxFileName, string streamName, byte[] bytes)
        {
            // 没有指定时，保存到当前目录下
            if (string.IsNullOrEmpty(outPath))
            {
                outPath = Path.Combine(Environment.CurrentDirectory, $"{Path.GetFileNameWithoutExtension(maxFileName)}_{streamName}.bin");
            };
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, bytes);

            AnsiConsole.MarkupLine($"[springgreen1]保存成功![/] 位置：{outPath}");
        }
    }
}
