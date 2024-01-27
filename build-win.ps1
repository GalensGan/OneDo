# 清空 ./build/temp 目录
if ((Test-Path -Path ./build/temp)) {
    Remove-Item -Path ./build/temp -Recurse -Force
}
if ((Test-Path -Path ./build/win)) {
    Remove-Item -Path ./build/win -Recurse -Force
}



# 获取当前目录及其子目录中的所有 .csproj 文件
$projects = Get-ChildItem -Path . -Filter *.csproj -Recurse

# 遍历每个项目文件
# 命令参考：https://learn.microsoft.com/zh-cn/dotnet/core/rid-catalog
foreach ($project in $projects) {
    # 使用 dotnet build 命令编译每个项目
    dotnet build $project.FullName -r win-x64  -c Release --self-contained false -o ./build/temp/$($project.BaseName)
}

# 将文件复制到 windows 目录
# 创建 build/win 目录
New-Item -Path ./build/win -ItemType Directory -Force

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
$onedoFiles = @("onedo.deps.json", "onedo.dll", "onedo.exe", "onedo.runtimeconfig.json", "Spectre.Console.dll", "System.CommandLine.dll")
CopyFilesToDir -sourceDir "./build/temp/OneDo" -files $onedoFiles -dir "./build/win"

# 复制 plugin
# WakeOnLANPlugin
$wolFiles = @("WakeOnLANPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/WakeOnLANPlugin" -files $wolFiles -dir "./build/win/plugins"

# SystemPlugin.dll
$systemPluginFiles = @("SystemPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/SystemPlugin" -files $systemPluginFiles -dir "./build/win/plugins"

# ShellPlugin.dll
$shellPluginFiles = @("ShellPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/ShellPlugin" -files $shellPluginFiles -dir "./build/win/plugins"

# ManagedAssemblyListPlugin.dll
$managedAssemblyListPluginFiles = @("ManagedAssemblyListPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/ManagedAssemblyListPlugin" -files $managedAssemblyListPluginFiles -dir "./build/win/plugins"

# Watermark
$watermarkFiles = @("SixLabors.Fonts.dll", "SixLabors.ImageSharp.dll", "SixLabors.ImageSharp.Drawing.dll", "WatermarkPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/WatermarkPlugin" -files $watermarkFiles -dir "./build/win/plugins/watermark"

# MinIO
$minioFiles = @("Minio.dll", "MinioPlugin.dll", "System.Net.Http.Formatting.dll")
CopyFilesToDir -sourceDir "./build/temp/MinioPlugin" -files $minioFiles -dir "./build/win/plugins/minio"
# 复制 MinIO 中的 Shell
New-Item -Path "./build/win/plugins/minio/Shells" -ItemType Directory -Force
Copy-Item -Path "./Plugins/MinioPlugin/Shells/saveImageFromClipboard.ps1" -Destination "./build/win/plugins/minio/Shells" -Force

# FTP
$ftpFiles = @("FluentFTP.dll", "FtpPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/FtpPlugin" -files $ftpFiles -dir "./build/win/plugins/ftp"

# Addin
$addinFiles = @(".addinPlugin.json", "MSAddinCLIPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/MSAddinCLIPlugin" -files $addinFiles -dir "./build/win/plugins/addin"
$addinTemplates = @("AppAddin.cs", "KeyinFunctions.cs")
CopyFilesToDir -sourceDir "./Plugins/MSAddinCLIPlugin/Templates" -files $addinTemplates -dir "./build/win/plugins/addin/Templates"

# 3dsMax
$threeDsMaxFiles = @("OpenMcdf.dll", "ThreeDSMaxPlugin.deps.json", "ThreeDSMaxPlugin.dll")
CopyFilesToDir -sourceDir "./build/temp/ThreeDSMaxPlugin" -files $threeDsMaxFiles -dir "./build/win/plugins/3dsmax"
# 复制 Everything64.dll
Copy-Item -Path "./Plugins/ThreeDSMaxPlugin/dll/Everything64.dll" -Destination "./build/win/plugins/3dsmax" -Force

# 打印结果
Write-Host "Build success!"
# 不关闭窗口
Read-Host -Prompt "Press Enter to exit"