using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OneDo.Utils
{
    /// <summary>
    /// 展示列定义
    /// </summary>
    public class ListPluginConfs
    {
        private JsonArray _datas;
        private List<FieldMapper> _fieldsMapper;
        private bool _valid = true;
        private string _fieldName;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="config">配置对象节点</param>
        /// <param name="fieldName">数组字段名</param>
        /// <param name="fieldsMapper"></param>
        public ListPluginConfs(JsonNode config, string fieldName, List<FieldMapper> fieldsMapper) : this(config[fieldName] as JsonArray, fieldsMapper)
        {
            _fieldName = fieldName;

            if (config == null)
            {
                AnsiConsole.MarkupLine("[red]缺少根配置[/]");
                _valid = false;
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                AnsiConsole.MarkupLine("[red]需指定插件根字段名[/]");
                _valid = false;
            }
        }

        public ListPluginConfs(JsonArray? jsonArray, List<FieldMapper> fieldsMapper)
        {
            _datas = jsonArray;
            _fieldsMapper = fieldsMapper ?? new List<FieldMapper>();

            if (_datas == null)
            {
                AnsiConsole.MarkupLine("[red]配置数据就为数组类型[/]");
                _valid = false;
            }
        }

        /// <summary>
        /// 展示列表
        /// </summary>
        public bool Show()
        {
            if (!_valid) return false;

            // 查找数据
            if (_datas == null)
            {
                AnsiConsole.MarkupLine("[red]配置数据为空[/]");
                return false;
            }


            if (_fieldsMapper == null || _fieldsMapper.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]无展示的字段[/]");
                return false;
            }

            if (!string.IsNullOrEmpty(_fieldName)) AnsiConsole.MarkupLine($"Available {_fieldName} : ");

            var grid = new Grid();
            // 添加列定义
            _fieldsMapper.ForEach(x => grid.AddColumn());
            // 添加名称
            grid.AddRow(_fieldsMapper.Select(x =>
            {
                return new Text(x.DisplayName, new Style(Color.SpringGreen1)).LeftJustified();
            }).ToArray());
            // 添加分隔符
            grid.AddRow(_fieldsMapper.Select(x =>
            {
                // 获取 x 的字节数
                var length = GetSplitterLengh(x.DisplayName);

                var spliter = "";
                for (int i = 0; i < length; i++)
                {
                    spliter += "-";
                }
                return new Text(spliter, new Style(Color.SpringGreen1)).LeftJustified();
            }).ToArray());

            var array = _datas;
            List<List<Text>> rows = new List<List<Text>>();
            foreach (var x in array)
            {
                // 获取值
                var row = new List<Text>();
                _fieldsMapper.ForEach(fieldMap =>
                {
                    var valueNode = x[fieldMap.FieldName];
                    if (valueNode == null) row.Add(new Text(string.Empty));
                    else
                    {
                        // 如果有格式化，需要先调用
                        string nodeValue = string.Empty;
                        if (fieldMap.Formatter != null) nodeValue = fieldMap.Formatter(valueNode);
                        else nodeValue = valueNode.ToString();
                        var style = fieldMap.StyleFormatter?.Invoke(nodeValue);
                        row.Add(new Text(nodeValue,style));
                    }
                });
                rows.Add(row);
            }
            // 按第一个值升序排列
            rows = rows.OrderBy(x => x[0].ToString()).ToList();
            rows.ForEach(x => grid.AddRow(x.ToArray()));
            AnsiConsole.Write(grid);
            AnsiConsole.WriteLine();

            return true;
        }

        /// <summary>
        /// 获取分隔符长度
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private int GetSplitterLengh(string displayName)
        {
            // 按单个字符长度计算，若单个字符大于 2,则按 2 计算
            int lengh = 0;
            foreach (var chr in displayName)
            {
                if (chr > 255) lengh += 2;
                else lengh += 1;
            }
            return lengh;
        }
    }
}
