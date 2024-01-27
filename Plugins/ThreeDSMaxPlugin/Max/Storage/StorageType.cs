using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OneDo.ThreeDSMaxPlugin.Max.Storage
{
    public enum StorageType
    {
        // Container types 100-199
        CONTAINER = 100,
        DLL_ENTRY = 101,

        CONFIG_SCRIPT = 102,
        CONFIG_SCRIPT_ENTRY = 103,

        // Value types 200-299
        VALUE = 200,
        DLL_DESCRIPTION = 201,
        DLL_NAME = 202,

        CLASS_DESCRIPTION = 203,
        CLASS_HEADER = 204,

        CONFIG_SIZE_HEADER = 205,
        CONFIG_SCRIPT_ENTRY_HEADER = 206,
        CONFIG_STRING = 207,
        CONFIG_FLOAT = 208,

        SCENE_OBJECT_NAME = 209
    }

    /// <summary>
    /// StorageType 的扩展方法
    /// </summary>
    public static class StorageTypeExtensions
    {
        /// <summary>
        /// 是否是值类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsValue(this StorageType type)
        {
            return type >= StorageType.VALUE;
        }

        /// <summary>
        /// 是否是容器
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsContainer(this StorageType type)
        {
            return type >= StorageType.CONTAINER && type < StorageType.VALUE;
        }

        /// <summary>
        /// 获取名称
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetName(this StorageType type)
        {
            return type.ToString();
        }
    }
}
