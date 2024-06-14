#判断是否有 scoop
if(!(Get-Command scoop -ErrorAction SilentlyContinue)) {
    Write-Host "scoop not found, please install scoop first."
    return
}

# 判断是否有 onedo
if(!(Get-Command onedo -ErrorAction SilentlyContinue)) {
    Write-Host "onedo not found, please install onedo first."
    return
}

# 获取 ./build 下的 .7z 压缩包，并按时间排序取最新的一个
$latest = Get-ChildItem -Path ./build -Filter *.7z | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if($latest -eq $null) {
    Write-Host "No .7z file found in ./build"
    return
}

$scriptRoot = $PSScriptRoot

# 从文件名中获取版本号：格式 onedo-verion-win.7z
$nameSegments = $latest.Name.Split("-")
$version = $nameSegments[1]

# 查找 scoop 所在位置
$scoopLocation = (Get-Command scoop).Source
# 向上两级
$temp = Split-Path -Path $scoopLocation -Parent
$scoopInstallDir = Split-Path -Path $temp -Parent
$onedoConfig = "$scoopInstallDir\buckets\uamazing\bucket\onedo.json"
if(!(Test-Path $onedoConfig)) {
    Write-Host "onedo.json not found in $onedoConfig"
    return
}

# 读取 onedo.json
$onedoJson = Get-Content $onedoConfig | ConvertFrom-Json
$configVersion = New-Object System.Version($onedoJson.version)
$currentVersion = New-Object System.Version($version)
if($configVersion -ge $currentVersion) {
    Write-Host "Current version is $configVersion, no need to update to $currentVersion"
    return
}

# 上传文件
Write-Host "Uploading $latest onedo file..."
$urls = onedo minio soft -p $latest.FullName

# 更新 onedo.json
$onedoJson.version = $version
#url 更新成新的 url
$onedoJson.url = $urls[1]
# 保存文件
$onedoJson | ConvertTo-Json | Set-Content $onedoConfig

Write-Host "Update onedo.json in scoop"
$onedoBucketDir = Split-Path -Path $onedoConfig -Parent
Set-Location -Path $onedoBucketDir
git add .
git commit -m "onedo: Update to version $version"
git ps

# 回到当前目录
Set-Location -Path $scriptRoot

Write-Host "Update to version $version successfully!" -ForegroundColor Green