# MiniServer

一个将提供REST API服务器程序!

## 已实现API
- /markdown `将Markdown内容转为图片`
- /commamd `执行系统命令`




## 启动参数


## 配置

```json
{
    "Logging": {
        "LogLevel": {
            "Default": "Information",
            "Microsoft.AspNetCore": "Warning"
        }
    },
    "EnableHeadLess": true, //是否开启无头模式
    "Server": {
        "Port": 55119 //端口
    }
}
```
