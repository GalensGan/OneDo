using OneDo.MSAddinCLIPlugin.Keyin;
using OneDo.MSAddinCLIPlugin.Utils;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Builders
{
    internal class KeyinCommandsBuilder : BuilderBase
    {
        private string _defaultKeyin;
        private string _defaultFunction;
        public KeyinCommandsBuilder(string defaultKeyin, string defaultFunction)
        {
            _defaultKeyin = defaultKeyin;
            _defaultFunction = defaultFunction;
        }

        public KeyinCommandsBuilder() { }

        public override bool Build(BuilderContext context)
        {
            // 获取项目 csproj 文件           
            var csprojXDocument = context.CSProjectDocument;

            // 验证是否继续
            var isDetectProject = csprojXDocument != null;
            if (!isDetectProject)
            {
                var askResult = AnsiConsole.Ask<string>("未在当前目录中找到项目，是否继续（Y/N）?", "N");
                if (!new List<string>() { "y", "yes" }.Contains(askResult.ToLower()))
                {
                    AnsiConsole.MarkupLine("[red]创建中断[/]");
                    return false;
                }
            }

            // 仅初始化化命令表
            // 判断是否存在命令表
            if (Helper.FindCommandTables(out var docs, out var commandsXmlFiles))
            {
                // 如果命令表没有添加到项目中，则要添加
                if (docs.Count > 1)
                {
                    AnsiConsole.MarkupLine("[red]当前目录下存在多个命令表，请进行清理[/]");
                    return false;
                }

                // 判断是否有项目，若有项目，将 commands .xml 添加到项目中并设置正确的格式
                if (isDetectProject)
                {
                    var commandFile = commandsXmlFiles[0];
                    var xmlRelativePath = Path.GetRelativePath(context.CSProjectDir, commandFile);
                    var fileElement = csprojXDocument.Descendants().FirstOrDefault(x => x.Attribute("Include")?.Value == xmlRelativePath);
                    if (fileElement == null || fileElement.Name.LocalName != "EmbeddedResource")
                    {
                        if (fileElement != null)
                            // 如果父节点下没有子节点了，则删除父节点
                            if (fileElement.Parent.Elements().Count() == 1)
                            {
                                fileElement.Parent.Remove();
                            }
                            else
                            {
                                // 仅删除这个节点
                                fileElement.Remove();
                            }

                        // 找到第一个 EmbeddedResource 节点，将命令表添加到这个节点的父类中
                        var embeddedResourceElement = csprojXDocument.Descendants().FirstOrDefault(x => x.Name.LocalName == "EmbeddedResource");
                        var embeddedResourceParentElement = embeddedResourceElement?.Parent;
                        if (embeddedResourceParentElement == null)
                        {
                            embeddedResourceParentElement = new XElement("ItemGroup");
                            csprojXDocument.Root.Add(embeddedResourceParentElement);
                        }
                        embeddedResourceParentElement.Add(new XElement("EmbeddedResource"
                           , new XAttribute("Include", xmlRelativePath)
                           , new XElement("LogicalName", "CommandTable.xml")
                           , new XElement("SubType", "Designer")));
                        csprojXDocument.Save(context.CSProjectPath);
                        AnsiConsole.MarkupLine($"[springgreen1]已将命令表 {xmlRelativePath} 添加到项目中[/]");
                        return true;
                    }

                    AnsiConsole.MarkupLine("[springgreen1]当前目录中已经存在命令表，无须初始化[/]");
                    return true;
                }
            }

            // 当不存在命令表时，新建
            // 向项目中添加命令表
            var keyinDocument = new KeyinDocument();
            if (string.IsNullOrEmpty(_defaultKeyin)) _defaultKeyin = "app default keyin";
            if (string.IsNullOrEmpty(_defaultFunction)) _defaultFunction = "Namespace.App.KeyinFunctions.Func";

            keyinDocument.AddKeyin(_defaultKeyin, _defaultFunction);

            var savePath = Path.Combine(Environment.CurrentDirectory, "commands.xml");

            if (!isDetectProject)
            {
                // 说明没有项目，就在当前目录下创建
                AnsiConsole.MarkupLine($"[springgreen1]keyin 表创建成功：{Path.GetRelativePath(Environment.CurrentDirectory, savePath)}[/]");
                return true;
            }

            var projectDir = context.CSProjectDir;
            var startupDir = Path.Combine(projectDir, "Startup");
            Directory.CreateDirectory(startupDir);
            savePath = Path.Combine(startupDir, "commands.xml");
            keyinDocument.Save(savePath);

            // 保存到项目中
            // 向根中新增 ItemGroup
            //<ItemGroup>
            //  < EmbeddedResource Include = "Access\rebars.commands.xml" >
            //    < LogicalName > CommandTable.xml </ LogicalName >
            //    < SubType > Designer </ SubType >
            //  </ EmbeddedResource >
            //</ ItemGroup >

            var root = csprojXDocument.Root;
            var itemGroup = new XElement("ItemGroup".ToXNameWithNSP(root));
            root.Add(itemGroup);
            var embeddedResource = new XElement("EmbeddedResource".ToXNameWithNSP(root)
                , new XAttribute("Include", "Startup\\commands.xml")
                , new XElement("LogicalName".ToXNameWithNSP(root), "CommandTable.xml")
                , new XElement("SubType".ToXNameWithNSP(root), "Designer"));
            itemGroup.Add(embeddedResource);

            AnsiConsole.MarkupLine($"[springgreen1]keyin 表创建成功：{Path.GetRelativePath(Environment.CurrentDirectory, savePath)}[/]");
            return true;
        }
    }
}
