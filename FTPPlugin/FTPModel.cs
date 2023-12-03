using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPPlugin
{
    /// <summary>
    /// 上传选项
    /// </summary>
    internal class FTPModel
    {
        /// <summary>
        /// 名称，必须要有值
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 21;

        /// <summary>
        /// 本机路径，上传时用
        /// </summary>
        public string LocalPath { get; set; }

        /// <summary>
        /// 远程路径
        /// </summary>
        public string RemotePath { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// 方法：put 更新文件
        /// </summary>
        public string Method { get; set; } = "put";

        /// <summary>
        /// 验证数据
        /// </summary>
        /// <returns></returns>
        internal bool Validate(out string message)
        {
            message = "success";
            if (string.IsNullOrEmpty(Host))
            {
                message = "请在配置文件中指定 host";
                return false;
            }

            if (Port <= 0)
            {
                message = "端口号必须为正整数";
                return false;
            }

            if (string.IsNullOrEmpty(Username))
            {
                message = "请配置用户名: username";
                return false;
            }

            if(string.IsNullOrEmpty(LocalPath)){
                message = "请指定 localPath";
                return false;
            }

            if (string.IsNullOrEmpty(RemotePath))
            {
                message = "请指定 remotePath";
                return false;
            }

            // 密码可为空
            if (string.IsNullOrEmpty(Method))
            {
                Method = "put";
            }

            return true;
        }
    }
}