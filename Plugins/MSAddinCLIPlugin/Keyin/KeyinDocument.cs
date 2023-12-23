using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace OneDo.MSAddinCLIPlugin.Keyin
{
    /// <summary>
    /// Keyin 类
    /// </summary>
    public class KeyinDocument : XDocument
    {
        public KeyinDocument() : base(new XDeclaration("1.0", "utf-8", null))
        {
            Init();
        }
        /// <summary>
        /// KeyinTree
        /// </summary>
        public KeyinTree? KeyinTree
        {
            get
            {
                return this.Elements().FirstOrDefault(x=>x.Name.LocalName==KeyinLabels.KeyinTree) as KeyinTree;
            }
        }

        /// <summary>
        /// 初始化命令表
        /// 在新建的时候调用
        /// </summary>
        private void Init()
        {
            var tree = new KeyinTree();
            tree.Add(new RootKeyinTable());
            tree.Add(new SubKeyinTables());
            tree.Add(new KeyinHandlers());
            this.Add(tree);
        }

        /// <summary>
        /// 添加 keyin 命令
        /// </summary>
        /// <param name="keyin"></param>
        /// <param name="function"></param>
        /// <returns></returns>
        public bool AddKeyin(string keyin, string function)
        {
            if (string.IsNullOrEmpty(keyin))
            {
                throw new ArgumentNullException(nameof(keyin), $"{KeyinLabels.Keyin} 不能为空");
            }

            if (string.IsNullOrEmpty(function))
            {
                throw new ArgumentNullException(nameof(function), $"{KeyinLabels.Function} 不能为空");
            }

            // 验证 function 格式：命名空间.类名称.方法名
            var dotCount = function.Count(x => x == '.');
            if (dotCount < 3) throw new ArgumentException($"{KeyinLabels.Function} 格式应为: 命名空间.类名.方法名");

            // 格式化 keyin: 将多个空格转换成一个空格
            var formatKeyin = Regex.Replace(keyin, @"\s+", " ").Trim();

            // 判断是否已经存在相同 keyin，查找 KeyinTree/KeyinHandlers/KeyinHandler 中的 Keyin 属性值
            var keyinValues = from kh in this.Descendants("KeyinHandler")
                              select kh.Attribute("Keyin")?.Value;
            if (keyinValues.Contains(formatKeyin)) return false;

            // 添加 keyin
            var keywords = formatKeyin.Split(" ");
            // 第一级从 RootKeyinTable 中查找
            var fistWord = keywords.First();
            var rootKeyinTable = this.KeyinTree.RootKeyinTable;
            var keyword = rootKeyinTable.GetKeyword(fistWord);
            if (keyword == null)
            {
                keyword = new Keyword(fistWord);
                // 向 KeyinTree/RootKeyinTable 中添加一个 keyword
                rootKeyinTable.Add(keyword);
                if (keywords.Length > 1)
                {
                    keyword.SubtableRef = fistWord;
                }
            }

            // 开始添加其它级别的命令
            AddSubKeyword(keyword.SubtableRef, keywords.Skip(1));

            // 添加 keyinHandler
            this.KeyinTree.KeyinHandlers.Add(new KeyinHandler(formatKeyin, function));
            return true;
        }

        private void AddSubKeyword(string keyinTableId, IEnumerable<string> keywords)
        {
            if (keywords == null || keywords.Count() == 0) return;

            var keyword = keywords.First();
            // 获取 keyinTable
            var keyinTable = this.KeyinTree.SubKeyinTables.Elements().FirstOrDefault(x => x.Attribute(KeyinLabels.ID)?.Value == keyinTableId);
            if (keyinTable == null)
            {
                // 添加一个表
                keyinTable = new SubKeyinTable(keyinTableId);
                this.KeyinTree.SubKeyinTables.Add(keyinTable);
            }
            // 向 keyinTable 中添加 word
            var keywordNode = keyinTable.Elements().FirstOrDefault(x => x.Attribute(KeyinLabels.Keyword)?.Value == keyword) as Keyword;
            if (keywordNode == null)
            {
                // 添加新的 keyword
                keywordNode = new Keyword(keyword);
                // 向 KeyinTree/RootKeyinTable 中添加一个 keyword
                keyinTable.Add(keywordNode);
                if (keywords.Count() > 1)
                {
                    // 添加下一级表
                    keywordNode.SubtableRef = keyword;
                }
            }

            AddSubKeyword(keywordNode.SubtableRef, keywords.Skip(1));
        }

        /// <summary>
        /// 从文件加载 keyinCommands
        /// </summary>
        /// <returns></returns>
        public static KeyinDocument Load(Stream stream)
        {
            var xDocument = XDocument.Load(stream);
            var keyinDocument = new KeyinDocument();
            foreach (var element in xDocument.Elements())
            {
                CopyNodeToKeyinDocument(element, keyinDocument);
            }
            return keyinDocument;
        }

        private static void CopyNodeToKeyinDocument(XElement origin, XContainer targetContainer)
        {
            // 复制当前元素
            var copyElement = CopyOneNode(origin);
            targetContainer.Add(copyElement);

            // 继续复制子级
            foreach (var element in origin.Elements())
            {
                CopyNodeToKeyinDocument(element, copyElement);
            }
        }

        /// <summary>
        /// 复制当前节点
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        private static XElement CopyOneNode(XElement origin)
        {
            var name = origin.Name;
            if (name == KeyinLabels.KeyinTree)
                return new KeyinTree(origin.Name, origin.Attributes());

            if (name == KeyinLabels.Keyword)
                return new Keyword(origin.Name, origin.Attributes());

            if (name == KeyinLabels.Option)
                return new Option(origin.Name, origin.Attributes());

            if (name == KeyinLabels.SubKeyinTables)
                return new SubKeyinTables(origin.Name, origin.Attributes());

            if (name == KeyinLabels.SubKeyinTable)
                return new SubKeyinTable(origin.Name, origin.Attributes());

            if (name == KeyinLabels.KeyinHandlers)
                return new KeyinHandlers(origin.Name, origin.Attributes());

            if (name == KeyinLabels.KeyinHandler)
                return new KeyinHandler(origin.Name, origin.Attributes());

            return new XElement(origin.Name, origin.Attributes());
        }
    }
}

