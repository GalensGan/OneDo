using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OneDo.Utils
{
    /// <summary>
    /// Json 帮助类
    /// </summary>
    public class JsonHelper
    {
        /// <summary>
        /// 获取文档中的数组
        /// 如果错误，内容会进行错误输出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="fieldName"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool GetJsonArray<T>(JsonNode root, string fieldName, out List<T> result)
        {
            result = default;
            if (root == null) return false;
            if (string.IsNullOrEmpty(fieldName)) return false;

            var node = root[fieldName];
            if (node == null)
            {
                AnsiConsole.MarkupLine($"[red]没有找到 {fieldName} 定义[/]");
                return false;
            };
            var array = node.AsArray();
            if (array == null)
            {
                AnsiConsole.MarkupLine($"[red]{fieldName}应是数组[/]");
                return false;
            }

            result = JsonSerializer.Deserialize<List<T>>(array, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            return true;
        }
    }
}
