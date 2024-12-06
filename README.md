# VideoStreamCaptureBot

Build Status: ![Build](https://github.com/nidbCN/VideoStreamCaptureBot/actions/workflows/dotnet.yml/badge.svg)

## 文档

[快速开始](https://github.com/nidbCN/VideoStreamCaptureBot/wiki#%E5%BC%80%E5%A7%8B%E4%BD%BF%E7%94%A8)

[参考文档](https://github.com/nidbCN/VideoStreamCaptureBot/wiki/%E5%8F%82%E8%80%83)

## 发行版

### 源码

见 [Releases · nidbCN/VideoStreamCaptureBot](https://github.com/nidbCN/VideoStreamCaptureBot/releases)。

### 容器镜像

#### git

最新版，通常与代码同步（除非最新的代码构建失败），可能有 bug，可能会爆炸，但是是最新的。

* Tag: `git`
* Name: `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:git`

#### latest

最新发行版，通常与最新的 Release 相同

* Tag: `latest`
* Name: `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:latest`

#### Release v8.1.5.6

* Tag: `8.1.5.6`
* Id: `78e0abb15468ae527be724bc13a6e8ce250b513eb014329ac413719a9c757486`
* Digest: `2771b4a54d6c557dcba5a8eb0b551f0f9c173ae826d2bec43ff856f26153e9d1`
* Name: `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:8.1.5.6`

#### Release v8.1.5.4

* Tag: `8.1.5.4`
* Id: `219974ac3294967110f241c06ba5950154fd7d1594e3f4487a08e29385640b47`
* Digest: `60a217efb2397e13371ad0fa0349d8dc8776671caa00a794c15cb5170fea5d6a`
* Name: `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:8.1.5.4`

### Release 8.1.4.5

* Tag: `8.1.4.5`
* Id: `b03e5f393ffeb258997374f28fa2e80279cfb8403bd609b891ae642370a2b91b`
* Digest: `9affe100cc6787d913a25116099c27d5c7adf0540dd35a09b59497e21d4504c3`
* Name: `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:8.1.4.5`

### Release 8.1.4.4

* Tag: `8.1.4.4`
* Id: `32c38ea0e494054d3bada3978b03bbe61a445f65ab7e522570c20c7d047cfdc3`
* Digest: `836ac17988cd5a231362b4bde02f5d67443f8af57f7fa8d2b04b4cdc51ba25a0`
* Name: `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:8.1.4.4`

### Release 8.1.4.3

* Tag: `8.1.4.3`
* Id: `d17d446cd0fca45bd24daf5091a79c9c1cdcb4529c16d9060d01fc9edb204a3d`
* Digest: `833a6e1b7387c74d5d499029a4beb12acab4a08a72134119afc2fe19ca67210f`
* Name: `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:8.1.4.3`

### Release v8.1.2.2

* Tag: `v8.1.2.2`
* Id: `4eef20bd5a10569aa6ed6a6d43030eeb54a0d41618c3074768143ec6788d0faa`
* Digest: `bdc2196ad372df50dc28aa4e33b3c171bcf0b6adefef19e4a3e6ac487369b8fc`
* Name: `registry.cn-beijing.aliyuncs.com/nidb-cr/camera-capture-bot:v8.1.2.2`

### 二进制

暂时懒得打包，自己编译吧。

## 开发计划

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
