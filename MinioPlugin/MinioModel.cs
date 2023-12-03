using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinioPlugin
{
    internal class MinioModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Endpoint { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Region { get; set; } = "us-east-1";
        public string SessionToken { get; set; }
        public bool UseSSL { get; set; }
        public string BucketName { get; set; }
        public string ObjectDir { get; set; }
        public bool CreateWhenBucketNotExist { get; set; } = false;

        public bool Validate()
        {
            if (string.IsNullOrEmpty(Endpoint))
            {
                AnsiConsole.MarkupLine($"[red]{Name} 缺失 endPoint[/]");
                return false;
            }

            if (string.IsNullOrEmpty(AccessKey))
            {
                AnsiConsole.MarkupLine($"[red]{Name} 缺失 accessKey[/]");
                return false;
            }

            if (string.IsNullOrEmpty(SecretKey))
            {
                AnsiConsole.MarkupLine($"[red]{Name} 缺失 secretKey[/]");
                return false;
            }

            if (string.IsNullOrEmpty(BucketName))
            {
                AnsiConsole.MarkupLine($"[red]{Name} 缺失 bucketName[/]");
                return false;
            }

            if (string.IsNullOrEmpty(ObjectDir))
            {
                AnsiConsole.MarkupLine($"[red]{Name} 缺失 objectDir[/]");
                return false;
            }

            return true;
        }
    }
}
