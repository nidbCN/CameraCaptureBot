# CameraCaptureBot

Build Status: ![Build](https://github.com/nidbCN/CameraCaptureBot/actions/workflows/dotnet.yml/badge.svg)

# 部署

## docker compose [推荐]

```
mkdir CameraCaptureBot
wget https://raw.githubusercontent.com/nidbCN/CameraCaptureBot/refs/heads/master/docker-compose.yaml
cd CameraCaptureBot
docker compose up -d
```

NOTE: 默认使用的是 git master 分支构建的镜像，可能会存在 bug。可以更改为 `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:latest-release` 或 `:v8.x.x.x`

## 直接使用二进制

### 依赖

版本要求: 7.x

Windows 下载地址: https://www.gyan.dev/ffmpeg/builds/#release-builds

# 配置

配置文件：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "Console": {
      "FormatterName": "simple",
      "FormatterOptions": {
        "IncludeScopes": true,
        "TimestampFormat": "HH:mm:ss ",
        "UseUtcTimestamp": false
      }
    }
  },
  "StreamOption": {
    "ffmpegRoot": "/usr/lib/x86_64-linux-gnu/",     // ffmpeg shared library location, should contain `avcodec.dll` on Windows or `libavcodec.a` on Linux/Unix
                                                    // This value is ffmpeg library location in pre-built docker image.
    "Url": "https://img.cdn.gaein.cn/fakelive.flv", // must set, live stream Uri, can be rtsp/rtmp/http
    "ConnectTimeout": 1200,                         // default 1200, in ms, timeout value for connect to stream url
    "CodecTimeout": 6000,                           // default 6000, in ms, timeout to decode or encode a frame
                                                    // NOTE: when set `KeyFrameOnly` to `true`, this option is timeout for the sum of decode all frame before keyframe
    "CodecThreads": 4,                              // default 8, threads to decode and encode
    "KeyFrameOnly": true,                           // only use keyframe in live stream, will cause high delay but better quality
    "LogLevel": "WARNING"   // will be deprecated.
  },
  "BotOption": {
    // KeyStore and DeviceInfo are located at ~/AppData/Local/IsolatedStorage/<random>\<random>\Url.<random>\AppFiles
    "KeyStoreFile": "keystore.json",        // default keystore.json
    "DeviceInfoFile": "deviceInfo.json",    // default deviceInfo.json

    // group/friend qq number in un unisgned int
    "AllowedGroups": null,          // default null, a list of allowed group, null for allow all
    "AllowedFriends": null,         // default null, a list of allowed friend, null for allow all
    "AdminAccounts": [],            // default empty, a list of admin friend
    "NotifyAdminOnException": true  // default true, send error message to admin account when message process error

    "FrameworkConfig": {
      "AutoReconnect": true,        // default true
      "AutoReLogin": true,          // default true
      "UseIPv6Network": false,      // default false
      "GetOptimumServer": true,     // default true
      "Protocol": 2                 // 0 for Windows, 1 for MacOS, 2 for Linux(default)
    }
  }
}
```

# 开发计划

- [x] 解码RTSP并发送图片
- [x] 并发使用
- [x] 支持输出 ffmpeg 日志
- [x] 使用 webp(libwebp) 替代 png
- [ ] ~~支持发送视频（Worker模式）~~
- [x] 优化 Bot 逻辑代码
- [ ] 对识别到的人脸提供打码选项
- [ ] 使用 ONNX Runtime 运行推理
- [ ] 优化 .NET 泛型主机代码，实现 `BotController` 等
- [ ] 优化编解码逻辑，实现 线程池+工厂 设计模式
