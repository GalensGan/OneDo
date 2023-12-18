using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace OneDo.Utils
{
    /// <summary>
    /// 用于格式化 json 配置中的字段值
    /// </summary>
    public class FieldMapper
    {
        public FieldMapper(string fieldName) 
        {
            FieldName = fieldName;
        }

        public FieldMapper(string fieldName, string displayName):this(fieldName)
        {
            DisplayName = displayName;
        }

        /// <summary>
        /// 字段名称
        /// </summary>
        public string FieldName { get; set; }

        private string _displayName;
        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(_displayName))
                {
                    return FieldName;
                }
                return _displayName;
            }
            set
            {
                _displayName = value;
            }
        }

        /// <summary>
        /// 格式化显示
        /// </summary>
        public Func<JsonNode, string> Formatter { get; set; }
    }
}
