using OneDo.Plugin;
using OneDo.Utils;
using Spectre.Console;
using System.CommandLine;
using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace OneDo.ManagedAssemblyListPlugin
{
    public class Startup : IPlugin
    {
        public void RegisterCommand(RootCommand rootCommand, JsonNode config)
        {
            var managedCommand = new Command("managed", "程序集相关的操作");
            rootCommand.Add(managedCommand);

            var listCommand = new Command("list", "查看当前目录下托管的程序集");
            listCommand.AddAlias("ls");
            managedCommand.Add(listCommand);

            // 添加选项
            var showAllOption = new Option<bool>("--all", "显示所有目录中的程序集");
            showAllOption.AddAlias("-a");
            listCommand.Add(showAllOption);

            // 添加过滤
            var grepOption = new Option<string>("--grep", "过滤程序集");
            grepOption.AddAlias("--reg");
            listCommand.Add(grepOption);

            listCommand.SetHandler((allDir, grepOption) =>
            {
                // 获取目录下所有的 dll
                var dlls = Directory.GetFiles(Environment.CurrentDirectory, "*.dll",
                    allDir ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

                // 过滤
                if (!string.IsNullOrEmpty(grepOption))
                {
                    var regex = new Regex(grepOption, RegexOptions.IgnoreCase);
                    dlls = dlls.Where(x =>
                    {
                        var relativePath = Path.GetRelativePath(Environment.CurrentDirectory, x);
                        return regex.IsMatch(relativePath);
                    }).ToArray() ;
                }

                int totalCount = 0;
                foreach (var dll in dlls)
                {
                    if (IsNetAssembly(dll))
                    {
                        totalCount++;
                        AnsiConsole.MarkupLine($"{Path.GetRelativePath(Environment.CurrentDirectory, dll)}");
                    }
                }


                if (totalCount == 0)
                {
                    AnsiConsole.MarkupLine($"[springgreen1]该目录下没有任何托管程序集[/]");
                    return;
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[springgreen1]共找到 {totalCount} 个托管程序集[/]");
                AnsiConsole.WriteLine();
            }, showAllOption, grepOption);
        }


        /// <summary>
        /// 是否是 DotNet 程序集
        /// 参考：https://forums.codeguru.com/showthread.php?424454-Check-if-DLL-is-managed-or-not
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private bool IsNetAssembly(string filename)
        {
            bool isAssembly = false;
            bool isTagFound = false;
            bool isVersionFound = false;
            string tag = "BSJB";

            if (!File.Exists(filename))
            {
                return isAssembly;
            }

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[2048];
                int bytesRead;

                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string content = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (!isTagFound && content.Contains(tag))
                    {
                        isTagFound = true;
                    }

                    if (isTagFound && !isVersionFound)
                    {
                        for (int i = 1; i < 10; i++)
                        {
                            string version = $"v{i}.";

                            if (content.Contains(version))
                            {
                                isVersionFound = true;
                                break;
                            }
                        }
                    }

                    if (isTagFound && isVersionFound)
                    {
                        isAssembly = true;
                        break;
                    }
                }
            }

            return isAssembly;
        }
    }
}