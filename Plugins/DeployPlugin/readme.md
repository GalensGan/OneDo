# ���������

## �����߼�

1. ǰ�˲���
	- ������Ȩ
	- �������
	- ѹ������
	- �ϴ�����
	- ��ѹ�ļ�
	- ִ�������ű�

2. ��˲���
	- ������Ȩ
	- �����ȡ����
	- ���²���

## ���ø�ʽ

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