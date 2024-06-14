# 软件部署插件

## 部署逻辑

1. 前端部署
	- 连接授权
	- 编译代码
	- 压缩代码
	- 上传代码
	- 解压文件
	- 执行其它脚本

2. 后端部署
	- 连接授权
	- 后端拉取代码
	- 重新部署

## 配置格式

```json
{
	"deploys": [
		{
			"name":"sf",
			"host":"",
			"token":"",
			"flows":[
				"npm run build",
				"7z a -tzip archive.zip",
				"upload file",
				"remote-run cp",
				"remote-run unzip"
			]
		}
	]
```