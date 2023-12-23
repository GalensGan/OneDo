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
    internal class RefAddBuilder : BuilderBase
    {
        private string _refName;
        private string _productName;
        private bool _useDefaultProductWhenNotFound = false;

        public RefAddBuilder(string refName, string productName, bool useDefaultProductWhenNotFound = false)
        {
            this._refName = refName;
            this._productName = productName;
            this._useDefaultProductWhenNotFound |= useDefaultProductWhenNotFound;
        }

        public override bool Build(BuilderContext context)
        {
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

            if (references.Count == 0 && _useDefaultProductWhenNotFound && config.References.Count > 0)
            {
                references = config.References.Take(1).ToList();
            }

            if (references.Count == 0)
            {
                AnsiConsole.MarkupLine($"[red]不已存在名称为 {_refName} 的引用配置[/]");
                return false;
            }

            // 解析安装路径
            var product = config.Products.FirstOrDefault(x =>
            {
                x.ResolveInstallPath();
                return x.IsInstalled && x.Names.Contains(_productName);
            });
            if (product == null)
            {
                AnsiConsole.MarkupLine($"[red]未能找到名称为 {_productName} 的产品[/]");
                return false;
            }

            // 使用 xml 读取
            var csprojXmlDocument = context.CSProjectDocument;
            var rootElement = csprojXmlDocument.Root;
            // 判断是否有变量定义
            var variableName = product.Names[0].ToUpper();
            var variableNode = rootElement.DescendantsWithNSP(variableName).FirstOrDefault();
            if (variableNode == null)
            {
                // 添加变量
                var propertyGroupNode = rootElement.DescendantsWithNSP("PropertyGroup").First();
                variableNode = new XElement(variableName.ToXNameWithNSP(rootElement), product.InstallPath);
                propertyGroupNode.Add(variableNode);
            }

            // 添加引用
            foreach (var reference in references.SelectMany(x => x.References))
            {
                var referencePath = reference;

                // 如果是以 $( 开头，说明用户直接指定了变量，不需要替换，只需要增加即可
                if (!reference.StartsWith("$("))
                {
                    // 将引用使用变量进行替换
                    var fileInfo = new FileInfo(referencePath);
                    if (!fileInfo.Exists)
                    {
                        var dllFullPath = Path.Combine(product.InstallPath, referencePath);
                        if (!File.Exists(dllFullPath))
                        {
                            AnsiConsole.MarkupLine($"[red]添加引用失败: {dllFullPath}[/]。未能找到引用文件");
                            continue;
                        }

                        fileInfo = new FileInfo(dllFullPath);
                    }

                    referencePath = fileInfo.FullName.Replace(product.InstallPath, $"$({_productName.ToUpper()})");
                }

                // 判断是否已经存在引用
                var referenceName = Path.GetFileNameWithoutExtension(referencePath);
                var referenceItem = rootElement.DescendantsWithNSP("Reference").FirstOrDefault(x => x.Attribute("Include")?.Value == referenceName);
                if (referenceItem != null)
                {
                    AnsiConsole.MarkupLine($"[red]引用已存在: {referenceName}[/]");
                    continue;
                };

                // 找到 Reference 节点，通过它获取父节点
                var referenceElement = rootElement.DescendantsWithNSP("Reference").FirstOrDefault();
                var refItemGroupElement = referenceElement?.Parent;
                if (refItemGroupElement == null)
                {
                    // 新建 ItemGroup 节点
                    refItemGroupElement = new XElement("ItemGroup".ToXNameWithNSP(rootElement));
                    rootElement.Add(refItemGroupElement);
                }

                // 创建引用节点
                var newReferenceElement = new XElement("Reference".ToXNameWithNSP(rootElement), new XAttribute("Include", referenceName)
                    , new XElement("HintPath".ToXNameWithNSP(rootElement), referencePath)
                    , new XElement("Private".ToXNameWithNSP(rootElement), "False")
                    );
                refItemGroupElement.Add(newReferenceElement);
                AnsiConsole.MarkupLine($"[springgreen1]已添加引用: {referencePath}[/]");
            }

            return true;
        }
    }
}
