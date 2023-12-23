using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace OneDO.MSAddinCLIPlugin.Models
{
    internal class BentleyProductModel
    {
        /// <summary>
        /// 名称，用于命令行输入
        /// </summary>
        public List<string> Names { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// 注册表键值，比如：{096F30E0-5B8E-47E1-800A-C57F3E1BCF05}
        /// </summary>
        public string ProductRegistryKey { get; set; }

        /// <summary>
        /// 产品名称，若指定注册表，则必须要指定产品名称
        /// </summary>
        public string ProductName { get; set; }

        private string _installPath;
        /// <summary>
        /// 本地安装路径
        /// 与 ProductRegistryKey 二选一
        /// </summary>
        public string InstallPath
        {
            get => _installPath;
            set => _installPath = value.TrimEnd('/', '\\');
        }

        /// <summary>
        /// 产品是否安装
        /// </summary>
        public bool IsInstalled { get; private set; }

        /// <summary>
        /// 解析安装路径
        /// </summary>
        public void ResolveInstallPath()
        {
            if (!string.IsNullOrEmpty(InstallPath))
            {
                // 判断是否存在
                IsInstalled = System.IO.Directory.Exists(InstallPath);
                if (IsInstalled) return;
            }

            // 从注册表中获取安装路径，从 HKEY_LOCAL_MACHINE\SOFTWARE\Bentley\Installed_Products 中获取
            if (!string.IsNullOrEmpty(ProductRegistryKey))
            {
                using RegistryKey key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Bentley\Installed_Products\{ProductRegistryKey}");
                if (key != null)
                {
                    var installDir = key.GetValue("InstallDir");
                    if (installDir != null)
                    {
                        // 查找产品名称
                        var productName = key.GetValue("ProductName");
                        if (productName != null)
                            InstallPath = Path.Combine(installDir.ToString(), productName.ToString());
                    }
                }
            }

            if (!string.IsNullOrEmpty(InstallPath))
            {
                // 判断是否存在
                IsInstalled = System.IO.Directory.Exists(InstallPath);
            }
        }
    }
}
