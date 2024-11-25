# CameraCaptureBot

Build Status: ![Build](https://github.com/nidbCN/CameraCaptureBot/actions/workflows/dotnet.yml/badge.svg)

# 部署

## docker compose [推荐]

```
mkdir CameraCaptureBot
cd CameraCaptureBot

wget https://raw.githubusercontent.com/nidbCN/CameraCaptureBot/refs/heads/master/docker-compose.yaml
wget https://raw.githubusercontent.com/nidbCN/CameraCaptureBot/refs/heads/master/CameraCaptureBot.Core/appsettings.Example.json

# 修改 appsettings.Example.json

mv appsettings.Example.json appsettings.json
docker compose up -d
```

注意: 默认使用的是 git master 分支构建的镜像，可能会存在 bug。可以更改为 `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:latest` 或 `:8.x.x.x`

### 镜像内置 ffmpeg

镜像中内置的 ffmpeg 为 [Release Auto-Build 2024-04-30 12:51 · BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases/tag/autobuild-2024-04-30-12-51) ，位置在 `/usr/lib/x86_64-linux-gnu/` 。

使用镜像中内置的 ffmpeg 时候， `appsettings.json` 中的 `StreamOption.FfMpegLibrariesPath` 应设置为 `""` （空字符串）或 `null` 或不填写。

当需要使用自定义 ffmpeg 库时候，应该设置为自定义库的路径。

## 直接使用二进制

### ffmpeg 依赖

版本要求: 7.0

Windows 下载地址 [Builds - CODEX FFMPEG @ gyan.dev](https://www.gyan.dev/ffmpeg/builds/#release-builds)

Linux pre-built 地址 [Releases · BtbN/FFmpeg-Builds](https://github.com/BtbN/FFmpeg-Builds/releases/)

#### 所需文件

实际上 ffmpeg 二进制并不是必须的，需要的是库文件，在 Linux 下为以下文件：

* libavcodec.so.61 
* libavdevice.so.61 
* libavfilter.so.10 
* libavformat.so.61 
* libavutil.so.59 
* libswresample.so.5 
* libswscale.so.8 

#### 依赖路径

##### Linux

`StreamOption.FfMpegLibrariesPath` 不填写或为 `null` 时，使用 `DllImport`，路径由 .NET 运行时决定。

`StreamOption.FfMpegLibrariesPath` 设置为 `""` （空字符串）时，将自动从默认路径中寻找，包括：

1. `/etc/ld.so.conf` 以及 `/etc/ld.so.conf.d/*.conf` 中所设置的路径；
2. 环境变量 `LD_LIBRARY_PATH` 中包括的路径；
3. `/lib` 与 `/usr/lib`；

详细行为见：[dlopen(3) - Linux manual page](https://www.man7.org/linux/man-pages/man3/dlopen.3.html)

`StreamOption.FfMpegLibrariesPath` 设置为其它非空字符串时，将从设置的路径中寻找，并忽略上述默认路径。

### 部署

下载二进制或从源代码编译

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
