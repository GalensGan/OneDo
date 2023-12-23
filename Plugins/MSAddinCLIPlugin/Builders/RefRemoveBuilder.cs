using OneDo.MSAddinCLIPlugin.Utils;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Builders
{
    internal class RefRemoveBuilder : BuilderBase
    {
        private string _refName;
        private string _regexFilter;

        public RefRemoveBuilder(string refName,string regexFilter="")
        {
            _refName = refName;
            _regexFilter = regexFilter;
        }

        public override bool Build(BuilderContext context)
        {
            if (string.IsNullOrEmpty(_refName))
            {
                AnsiConsole.MarkupLine($"refName 参数为空,请指定非空值");
                return false;
            }

            // 判断是否有配置文件
            var configPath = Helper.GetThisConfigPath();
            if (!File.Exists(configPath))
            {
                AnsiConsole.MarkupLine($"[red]未能找到配置文件: .addinPlugin.json[/]");
                return false;
            }

            // 将配置读取成 Config 对象
            if (!Helper.GetConfigObject(out var config)) return false;

            // 判断是否存在 name 对应的引用
            var references = config.References.FindAll(x => x.Name.ToLower() == _refName.ToLower());

            if (references.Count == 0)
            {
                AnsiConsole.MarkupLine($"[red]不已存在名称为 {_refName} 的引用配置[/]");
                return false;
            }

            // 使用 xml 读取
            var csprojXmlDocument = context.CSProjectDocument;
            var rootElement = csprojXmlDocument.Root;

            HashSet<string> variableSet = new HashSet<string>();
            var regex = new Regex(@"^\$\((.*?)\)");
            var toRemove = new List<XElement>();
            // 移除引用
            foreach (var reference in references.SelectMany(x => x.References))
            {
                if (!string.IsNullOrEmpty(_regexFilter))
                {
                    if (!Regex.IsMatch(reference, _regexFilter)) continue;
                }

                var referencePath = reference;
                // 判断是否存在引用
                var referenceName = Path.GetFileNameWithoutExtension(referencePath);
                var referenceItems = rootElement.DescendantsWithNSP("Reference").Where(x =>
                {
                    var includeAttr = x.Attribute("Include");
                    if (includeAttr == null) return false;
                    return includeAttr.Value.EndsWith(referenceName);
                });
                // 移除引用
                foreach (var referenceItem in referenceItems)
                {
                    // 获取下面的 HintPath
                    var hintPathValue = referenceItem.DescendantsWithNSP("HintPath").FirstOrDefault()?.Value;
                    if (!string.IsNullOrEmpty(hintPathValue))
                    {
                        // 从两个值中解析 $(xxx) 的值                       
                        var match = regex.Match(hintPathValue);
                        if (match.Success)
                        {
                            variableSet.Add(match.Groups[1].Value);
                        }
                    }
                    toRemove.Add(referenceItem);
                }
            }

            foreach (var item in toRemove)
            {
                item.Remove();
                AnsiConsole.MarkupLine($"[red]已移除 {item.Attribute("Include").Value} [/]");
            }

            AnsiConsole.MarkupLine($"[green]移除 {_refName} 成功，共计 {toRemove.Count} 项[/]");

            // 判断是否需要移除变量定义
            if (variableSet.Count > 0)
            {
                foreach (var variable in variableSet)
                {
                    var findHintPath = rootElement.DescendantsWithNSP("HintPath").Any(x => x.Value.Contains(variable));
                    if (findHintPath) continue;

                    // 移除环境变量
                    var property = rootElement.DescendantsWithNSP(variable.ToUpper()).FirstOrDefault();
                    property?.Remove();
                }
            }
            return true;
        }
    }
}
