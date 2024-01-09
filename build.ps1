# 获取当前目录及其子目录中的所有 .csproj 文件
$projects = Get-ChildItem -Path . -Filter *.csproj -Recurse

# 遍历每个项目文件
foreach ($project in $projects) {
    # 使用 dotnet build 命令编译每个项目
    dotnet build $project.FullName
}

# 定义一个字典
$dict = @{
    "ftp"  = @("Plugins/FTP", "FTPPlugin.dll", "FluentFTP.dll")
    "managed" = @("Plugins","ManagedAssemblyListPlugin.dll")
    "minio" = @("Plugins/MinIO","Minio.dll","MinioPlugin.dll","System.Net.Http.Formatting.dll")
}
