## 关于 OneDo

OneDo 旨在将繁杂的多个操作集成到一条命令执行。比如要新建一个 Addin 项目时，我们只需要在终端中输入：

``` powershell
onedo addin new wpf -n myWpfAddin -p ord
```

它将自动为我们新建项目 `myWpfAddin` 并自动配置好 Addin 和 Commands.xml。

## 特点

- 插件化

  OneDo 支持插件化开发，可以根据需要增减插件来实现个性化的需求

- 跨平台

  使用 .NET Core 框架，支持 Windows、Linux、MAC

- 化繁为简

  将多个操作简化成一条命令，提升效率

## 插件介绍

目前 OneDo 共有 9 个插件，下面逐一进行介绍。

| 插件名称                  | 说明                                                         | 备注                     |
| ------------------------- | ------------------------------------------------------------ | ------------------------ |
| SystemPlugin              | 提供插件的安装、卸载、配置等操作                             | 系统必须插件，无法被禁用 |
| FTPPlugin                 | 支持配置不同的 FTP 实现一键上传文件到 FTP                    |                          |
| MinioPlugin               | 支持向 Minio 服务器中上传文件                                |                          |
| ShellPlugin               | 支持执行脚本，调用其它命令                                   |                          |
| ManagedAssemblyListPlugin | 获取目录中有哪些 dll 文件是托管程序集                        |                          |
| MSAddinCLIPlugin          | 支持新建 Addin 项目，可以快速管理项目的参考，管理 addin 中的 keyin |                          |
| WakeOnLanPlugin           | 支持配置远程网络唤醒                                         |                          |
| WatermarkPlugin           | 批量给图片添加图片或文字水印                                 |                          |
| ThreeDSMaxPlugin          | 不需要启动 3ds max 即可从电脑中查找 max 文件的外部依赖，并归档到当前 max 文件路径下。 |                          |

### SystemPlugin

**命令**：

| 命令                          | 说明                                                         |
| ----------------------------- | ------------------------------------------------------------ |
| install                       | 安装 OneDo，安装后会将 OneDo 写入环境变量，可以在其它地方进行调用 |
| uninstall                     | 卸载 OneDo，清理配置文件                                     |
| conf user                     | 打开用户配置文件                                             |
| conf app                      | 打开 OneDo 安装目录                                          |
| open                          | 打开当前环境目录                                             |
| plugin enable \<pluginName\>  | 启用插件                                                     |
| plugin disable \<pluginName\> | 禁用插件                                                     |
| plugin list 或 plugin ls      | 显示可用插件                                                 |

**plugin ls 示例：**

![image-20231224113228253](https://obs.uamazing.cn:52443/public/files/images/image-20231224113228253.png)

### FTPPlugin

<video id="video" controls="" preload="none" poster="https://obs.uamazing.cn:52443/public/files/images/image-20240127230938331.png">
      <source id="mp4" src="https://obs.uamazing.cn:52443/public/files/video/onedo-ftp.mp4" type="video/mp4" />
</video>

**命令**：

| 命令               | 说明                      |
| ------------------ | ------------------------- |
| ftp \<配置名\>     | 调用指定配置进行 ftp 上传 |
| ftp list 或 ftp ls | 显示可调用的 ftp 配置     |

**ftp ls 示例**：

![image-20231224112307929](https://obs.uamazing.cn:52443/public/files/images/image-20231224112307929.png)

**配置约定**：

ftp 的配置需定义到 `ftps` 的数组中，其结构如下：

``` json
{
  "ftps": [
    {
      "name": "dist",
      "description": "上传前端到正式环境目录",
      "host": "192.168.23.11",
      "port": 21,
      "username": "frontEnd",
      "password": "whfy8888",
      "localPath": "D:/Develop/Work/swToolsFrontEnd/dist/",
      "remotePath": "/",
      "method": "put"
    }
  ]
}
```

> 目前 method 仅实现了 put，该字段可以缺省

### MinioPlugin

**命令**：

| 命令                   | 说明                                |
| ---------------------- | ----------------------------------- |
| minio \<配置名\>       | 调用指定配置向 minio 服务中上传文件 |
| minio list 或 minio ls | 显示可调用的 minio  配置            |

**配置约定**：

``` json
{
  "minios": [
    {
      "name": "img",
      "endpoint": "your.minio.com",
      "accessKey": "user",
      "secretKey": "password",
      "region": "",
      "sessionToken": "",
      "useSSL": true,
      "bucketName": "public",
      "objectDir": "files/images"
    }
  ]
}
```

### ShellPlugin

**命令**：

| 命令                   | 说明                                   |
| ---------------------- | -------------------------------------- |
| shell \<names\>        | 执行指定配置的 shell，可以同时调用多个 |
| shell list 或 shell ls | 显示可用的 shell 定义列表              |



**shell list 示例：**

![image-20231224113823814](https://obs.uamazing.cn:52443/public/files/images/image-20231224113823814.png)



### MSAddinCLIPlugin

<video id="video" controls="" preload="none" poster="https://obs.uamazing.cn:52443/public/files/images/image-20240127231135395.png">
      <source id="mp4" src="https://obs.uamazing.cn:52443/public/files/video/onedo-addinCli.mp4" type="video/mp4" />
</video>

### WatermarkPlugin

<video id="video" controls="" preload="none" poster="https://obs.uamazing.cn:52443/public/files/images/image-20240127231135395.png">
      <source id="mp4" src="https://obs.uamazing.cn:52443/public/files/video/onedo-watermark.mp4" />
</video>

### ThreeDSMaxPlugin

<video id="video" controls="" preload="none" poster="https://obs.uamazing.cn:52443/public/files/images/image-20240127231135395.png">
      <source id="mp4" src="https://obs.uamazing.cn:52443/public/files/video/onedo-max.mp4" />
</video>

## 安装使用

### 安装

1. windows 可通过 scoop 安装

   ``` powershell
   scoop add bucket uamazing https://gitee.com/galensgan/galens-bucket.git
   scoop install onedo
   ```
```
   
2. 直接下载安装包使用

   https://obs.uamazing.cn:52443/public/files/soft/OneDo-v1.0.0.7z

### 增加配置

## 插件开发

### 命名约定

### 插件位置


```