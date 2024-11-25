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

[JSON 配置文件示例](https://github.com/nidbCN/CameraCaptureBot/blob/master/CameraCaptureBot.Core/appsettings.Example.json) 

[JSON Schema](https://github.com/nidbCN/CameraCaptureBot/blob/master/CameraCaptureBot.Core/appsettings.schema.json)

## 配置方式

### 命令行参数

该配置优先级最高，会覆盖环境变量与配置文件。

参数以 `--` 开头，并使用 `:` 来表示下一级节点，使用空格分隔键值。如 `StreamOption.FfMpegLibrariesPath` 对应的命令行参数为 `--StreamOption:FfMpegLibrariesPath <path>`。

### 环境变量

使用 `__` 来表示下一级节点。如 `StreamOption.FfMpegLibrariesPath` 对应的环境变量为 `StreamOption__FfMpegLibrariesPath=<path>`。

### 配置文件

于 `appsettings.json` 中设定，使用 `JSON` 来表示节点的层级关系，如 `StreamOption.FfMpegLibrariesPath` 对应的 `JSON` 文件内容为：

```json
// ...
"StreamOption": {
    "FfMpegLibrariesPath": "<path>",
    // ...
}
// ...
```

## 配置节点

### `StreamOption` 节点

#### `StreamOption.FfMpegLibrariesPath`

默认值为 `null`。ffmpeg 库的路径设置，有以下几种情况：

1. 不定义该节点，将使用 `DllImport` 动态链接到 ffmpeg 库；
2. 赋值为 `null`，同 1；
3. 赋值为 `""`，使用系统默认的路径动态加载 ffmpeg 库；
4. 赋值为非空字符串，使用字符串作为路径动态加载 ffmpeg 库；

动态链接时，搜索路径由 `DllImport` 指定，详见 [Native library loading - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/native-interop/native-library-loading)。

动态加载时，系统默认的路径指：

1. Windows 下，是 `Kernel32` 中 `LoadLibray` 的默认搜索路径，详见 [LoadLibraryA function (libloaderapi.h) - Win32 apps | Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibrarya)；
2. Linux 下，是 `dlopen` 的默认搜索路径，包括 `ldconfig` 的配置文件和 `LD_LIBRARY_PATH` 环境变量等，详见 `man 3 dlopen`；

当赋值为非空字符串时，路径应该存在并且包含以下文件（文件名变种见路径的参考链接与命令）：

* Windows 系统下： `avutil-59.dll` `swscale-8.dll` `swresample-5.dll` `postproc-58.dll` `avcodec-61.dll` `avformat-61.dll` `avfilter-10.dll`
* Linux/FreeBSD 系统下： `libavutil.so.59` `swscale.so.8` `swresample.so.5` `postproc.so.58` `avcodec.so.61` `avformat.so.61` `avfilter.so.10`
* MacOS 系统下： `libavutil.59.dylib` `swscale.8.dylib` `swresample.5.dylib` `postproc.58.dylib` `avcodec.61.dylib` `avformat.61.dylib` `avfilter.10.dylib`

#### `StreamOption.Url`

必填。打开的媒体流链接，支持的协议、格式、编码由所加载的 ffmpeg 库决定。

#### `StreamOption.ConnectTimeout`

默认值为 `1200`。连接到媒体流的超时时间。单位毫秒。

#### `StreamOption.CodecTimeout`

默认值为 `5000`。编解码超时时间。由于需要持续解码直到关键帧才会输出，因此应该需根据关键帧之间的间隔与 FPS 进行调节并适当延长，确保不会解码失败。单位毫秒。

#### `StreamOption.CodecThreads`

默认值为 `4`。编解码线程数，应设置为与 CPU 线程数相同或至少为 1 。**当前版本不支持自动，请手动设置。**

### `BotOpion` 节点

#### `BotOpion.KeyStoreFile`

默认值为 `"keystore.json"`，登录密钥文件名，不需要修改。

* 在 Windows 存储位置参考：[Isolated Storage - .NET | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/io/isolated-storage#impact-in-multi-user-environments)
* 在 *unix 存储在 `~/.local/share/IsolatedStorage/`

#### `BotOpion.DeviceInfoFile`

默认值为 `"deviceInfo.json"`，设备信息文件名，不需要修改。

存储位置同 `BotOpion.KeyStoreFile`

#### `BotOpion.AccountPasswords`

默认值为 `null`。账号密码字典，形式如下：

```json
"BotOpion": {
    "AccountPasswords": {
        "114514": {   // QQ号
            "Hashed": true, // 使用 md5
            "Passowrd": "c4d038b4bed09fdb1471ef51ec3a32cd"  // 密码 md5
        },
        "1919810": {
            "Hashed": false,    // 使用明文
            "Password": "henghenghengaaa!"  // 明文密码
        }
    }
}
```

无论如何，首次登录均使用二维码，登录后使用已保存的 session 快捷登录。当首次登陆后、登录过期，且设置了与扫码登录匹配的账号时，将使用该账号下的密码进行登录，登录失败将回退到二维码登录。

##### `BotOpion.AccountPasswords.<Uin>`

账号与密码对象，键为 QQ号，值为密码及其信息，包括下述两个字段。

###### `BotOpion.AccountPasswords.<Uin>.Hashed`

默认值为 `false`，指示下面的 `Password` 是否为 `md5`。如果是已经计算好的 `md5` 应设置为 `true`，明文应该设置为 `false`。

###### `BotOpion.AccountPasswords.<Uin>.Password`

必填。密码的md5值或密码明文。

#### `BotOpion.AllowedGroups`

默认值为 `null`，允许使用 bot 的 QQ群。

* 设置为 `null` 时候允许所有群；
* 设置为 `[]` 不允许所有群；
* 设置为 `[<gourp>, <group> ...]` 时仅允许列表内的群；

#### `BotOpion.AllowedFriends`

默认值为 `null`，允许使用 bot 的 QQ好友。

* 设置为 `null` 时候允许所有好友；
* 设置为 `[]` 不允许所有好友；
* 设置为 `[<gourp>, <group> ...]` 时仅允许列表内的好友；

#### `BotOpion.AdminAccounts`

默认值为 `[]`，管理员账号，~~设置通知后会受到来自 bot 的骚扰消息。~~

#### `BotOpion.NotificationConfig`

默认为新对象，通知设置。

#### `BotOpion.NotificationConfig.NotifyAdminOnException`

默认为 `true`。在消息处理全过程（包括编解码）中抛异常时通过 QQ 发送消息通知管理员。

#### `BotOpion.NotificationConfig.NotifyWebhookOnException`

默认为 `false`。在消息处理全过程（包括编解码）中抛异常时通过 WebHook 发送消息通知。

【未实现】

#### `BotOpion.NotificationConfig.NotifyAdminOnHeartbeat`

默认为 `false`。通过 QQ 向管理员账号发送心跳消息。

#### `BotOpion.NotificationConfig.NotifyWebhookOnHeartbeat`

默认为 `false`。通过 WebHook 发送心跳消息。

#### `BotOpion.NotificationConfig.HeartbeatIntervalHour`

默认为 `6`。发送心跳间隔，单位小时。

#### `BotOpion.NotificationConfig.WebhookUrl`

默认为 `null`。Webhook 链接。

#### `BotOpion.NotificationConfig.WebhookHeaders`

默认为 `null`。Webhook 消息的 HTTP 头。

【未实现】

#### `BotOpion.FrameworkConfig`

默认值为：

* `BotOpion.FrameworkConfig.AutoReconnect`: `true`
* `BotOpion.FrameworkConfig.AutoReLogin`: `true`
* `BotOpion.FrameworkConfig.GetOptimumServer`:
* `BotOpion.FrameworkConfig.Protocol`: `Protocols.Linux`（或 `2`）
* `BotOpion.FrameworkConfig.UseIPv6Network`: `true`

Lagrange.Core 设置，详见 [创建 Bot 实例 | Lagrange 文档](https://lagrangedev.github.io/Lagrange.Doc/Lagrange.Core/CreateBot/#botconfig-%E9%85%8D%E7%BD%AE%E7%B1%BB)

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
