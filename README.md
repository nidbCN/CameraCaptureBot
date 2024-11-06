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

Windows 下载地址 [ffmpeg built by gyan](https://www.gyan.dev/ffmpeg/builds/#release-builds)

# 配置

[JSON 配置文件示例](https://github.com/nidbCN/CameraCaptureBot/blob/master/CameraCaptureBot.Core/appsettings.Example.json) [JSON Schema](https://github.com/nidbCN/CameraCaptureBot/blob/master/CameraCaptureBot.Core/appsettings.schema.json)

# 开发计划

- [x] 解码RTSP并发送图片
- [x] 并发使用
- [x] 支持输出 ffmpeg 日志
- [x] 使用 webp(libwebp) 替代 png
- [ ] ~~支持发送视频（Worker模式）~~
- [x] 优化 Bot 逻辑代码
- [ ] 对识别到的人脸提供打码选项
- [ ] 使用 ONNX Runtime 运行推理
- [ ] 优化 StarUp，使用 HostedService
- [ ] 优化 .NET 泛型主机代码，实现 `BotController` 等
- [ ] 优化编解码逻辑，实现 线程池+工厂 设计模式
- [ ] 优化 Notification，创建 LoggerProvider
