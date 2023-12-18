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
        private JsonNode _config;
        private string _fieldName;
        private List<FieldMapper> _fieldsMapper;

        public ListPluginConfs(JsonNode config, string fieldName, List<FieldMapper> fieldsMapper)
        {
            _config = config;
            _fieldName = fieldName;
            _fieldsMapper = fieldsMapper;
        }

        /// <summary>
        /// 展示列表
        /// </summary>
        public bool Show()
        {
            // 查找数据
            if (_config == null)
            {
                AnsiConsole.MarkupLine("[red]配置数据为空[/]");
                return false;
            }

            if (string.IsNullOrEmpty(_fieldName))
            {
                AnsiConsole.MarkupLine("[red]需指定插件根字段名[/]");
                return false;
            }

            if (_fieldsMapper == null || _fieldsMapper.Count == 0)
            {
                AnsiConsole.MarkupLine("[red]无展示的字段[/]");
                return false;
            }

            AnsiConsole.MarkupLine($"Available {_fieldName} : ");

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
                var length = Encoding.UTF8.GetBytes(x.DisplayName).Length;
                var spliter = "";
                for (int i = 0; i < length; i++)
                {
                    spliter += "-";
                }
                return new Text(spliter, new Style(Color.SpringGreen1)).LeftJustified();
            }).ToArray());

            // 获取所有的值
            if (_config[_fieldName] == null)
            {
                AnsiConsole.MarkupLine($"[red]配置中不存在 {_fieldName}[/]");
                return false;
            }

            var array = _config[_fieldName].AsArray();
            if (array == null)
            {
                AnsiConsole.MarkupLine($"[red]配置{_fieldName} 应是数组形式[/]");
                return false;
            }

            List<List<string>> rows = new List<List<string>>();
            foreach (var x in array)
            {
                // 获取值
                var row = new List<string>();
                _fieldsMapper.ForEach(k =>
                {
                    var valueNode = x[k.FieldName];
                    if (valueNode == null) row.Add(string.Empty);
                    else
                    {
                        // 如果有格式化，需要先调用
                        string nodeValue = string.Empty;
                        if (k.Formatter != null) nodeValue = k.Formatter(valueNode);
                        else nodeValue = valueNode.ToString();
                        row.Add(nodeValue);
                    }
                });
                rows.Add(row);
            }
            // 按第一个值升序排列
            rows = rows.OrderBy(x => x[0]).ToList();
            rows.ForEach(x => grid.AddRow(x.ToArray()));
            AnsiConsole.Write(grid);
            AnsiConsole.WriteLine();

            return true;
        }
    }
}
