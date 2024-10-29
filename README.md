# 部署

## 依赖

版本要求: 7.x

Windows 下载地址: https://www.gyan.dev/ffmpeg/builds/#release-builds

## 配置

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
  "BotConfig": {
    "AllowGroups": [
      761972629
    ]
  },
  "StreamOption": {
    "Url": "https://img.cdn.gaein.cn/fakelive.flv", // must set, live stream Uri, can be rtsp/rtmp/http
    "ConnectTimeout": 1200,                         // default 1200, in ms, timeout value for connect to stream url
    "CodecTimeout": 6000,                           // default 6000, in ms, timeout to decode or encode a frame
                                                    // NOTE: when set `KeyFrameOnly` to `true`, this option is timeout for the sum of decode all frame before keyframe
    "CodecThreads": 4,                              // default 8, threads to decode and encode
    "KeyFrameOnly": true,                           // only use keyframe in live stream, will cause high delay but better quality
    "ffmpegRoot": "C:/Users/User/Tools/ffmpeg-7.0.2/bin",   // ffmpeg shared library location, should contain `avcodec.dll` on Windows or `libavcodec.a` on Linux/Unix
    "LogLevel": "WARNING"
  },
  "BotOption": {
    // KeyStore and DeviceInfo are located at ~/AppData/Local/IsolatedStorage/<random>\<random>\Url.<random>\AppFiles
    "KeyStoreFile": "keystore.json",        // default keystore.json
    "DeviceInfoFile": "deviceInfo.json",    // default deviceInfo.json
    
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
- [ ] 支持发送视频（Worker模式）
- [-] 优化 Bot 逻辑代码
