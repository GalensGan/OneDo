# 清空 ./build/temp 目录
if ((Test-Path -Path ./build/temp)) {
    Remove-Item -Path ./build/temp -Recurse -Force
}
if ((Test-Path -Path ./build/linux-x64)) {
    Remove-Item -Path ./build/linux-x64 -Recurse -Force
}



# 获取当前目录及其子目录中的所有 .csproj 文件
$projects = Get-ChildItem -Path . -Filter *.csproj -Recurse

# 遍历每个项目文件
# 命令参考：https://learn.microsoft.com/zh-cn/dotnet/core/rid-catalog
foreach ($project in $projects) {
    # 使用 dotnet build 命令编译每个项目
    dotnet build $project.FullName -r linux-x64  -c Release --self-contained false -o ./build/temp/$($project.BaseName)
}

# 将文件复制到 windows 目录
# 创建 build/win 目录
New-Item -Path ./build/linux-x64 -ItemType Directory -Force

# Import the required module for the Copy-Item cmdlet
Import-Module -Name Microsoft.PowerShell.Management

# 定义一个方法，接受一个数组字符串和一个目录字符串参数，将数组中的文件复制到目录中
function CopyFilesToDir {
    param(
        $sourceDir,
        $files,
        $dir
    )

    if (!(Test-Path -Path $dir)) {
        New-Item -ItemType Directory -Force -Path $dir
    }

    foreach ($file in $files) {
        Copy-Item -Path "$sourceDir/$file" -Destination $dir -Force
    }
}

# 程序主体
$onedoFiles = @("onedo.deps.json", "onedo.dll", "onedo", "onedo.runtimeconfig.json", "Spectre.Console.dll", "System.CommandLine.dll")
CopyFilesToDir -sourceDir "./build/temp/OneDo" -files $onedoFiles -dir "./build/linux-x64"

# SystemPlugin.dll
$systemPluginFiles = @("SystemPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/SystemPlugin" -files $systemPluginFiles -dir "./build/linux-x64/plugins"

# WakeOnLANPlugin
$wolFiles = @("WakeOnLANPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/WakeOnLANPlugin" -files $wolFiles -dir "./build/linux-x64/plugins"

# MinIO
$minioFiles = @("Minio.dll", "MinioPlugin.dll", "System.Net.Http.Formatting.dll")
CopyFilesToDir -sourceDir "./build/temp/MinioPlugin" -files $minioFiles -dir "./build/linux-x64/plugins/minio"
# 复制 MinIO 中的 Shell
New-Item -Path "./build/linux-x64/plugins/minio/Shells" -ItemType Directory -Force
Copy-Item -Path "./Plugins/MinioPlugin/Shells/saveImageFromClipboard.ps1" -Destination "./build/linux-x64/plugins/minio/Shells" -Force

# FTP
$ftpFiles = @("FluentFTP.dll", "FtpPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/FtpPlugin" -files $ftpFiles -dir "./build/linux-x64/plugins/ftp"

# 打印结果
Write-Host "Build success!"
# 不关闭窗口
Read-Host -Prompt "Press Enter to exit"