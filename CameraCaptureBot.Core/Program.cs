using System.Runtime.InteropServices;
using CameraCaptureBot.Core;
using CameraCaptureBot.Core.Codecs;
using CameraCaptureBot.Core.Configs;
using CameraCaptureBot.Core.Extensions.DependencyInjection;
using CameraCaptureBot.Core.Services;
using CameraCaptureBot.Core.Utils;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLinked;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(s =>
{
    s.ServiceName = "Live stream capture bot";
});

builder.Services.Configure<StreamOption>(
    builder.Configuration.GetRequiredSection(nameof(StreamOption)));
builder.Services.Configure<BotOption>(
    builder.Configuration.GetRequiredSection(nameof(BotOption)));

builder.Services.AddTransient<BinarySizeFormatter>();

builder.Services.AddSingleton<FfmpegLoggerService>();
builder.Services.AddSingleton<FfmpegLibWebpEncoder>();
builder.Services.AddSingleton<CaptureService>();
builder.Services.AddIsoStorages();

builder.Services.AddBots(() => builder.Configuration
    .GetRequiredSection(nameof(BotOption))
    .Get<BotOption>()!);
builder.Services.AddHostedService<BotHost>();
builder.Services.AddHostedService<HeartBeatWorker>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var streamOption = host.Services.GetRequiredService<IOptions<StreamOption>>();

ConfigureFfMpeg(logger, streamOption.Value);

host.Run();
return;

static void ConfigureFfMpeg(ILogger logger, StreamOption config)
{
    // ReSharper disable StringLiteralTypo
    var libraryDict = new Dictionary<string, Func<uint>>
    {
        { "avutil", ffmpeg.avutil_version },
        { "swscale", ffmpeg.swscale_version },
        { "swresample", ffmpeg.swresample_version },
        { "postproc", ffmpeg.postproc_version },
        { "avcodec", ffmpeg.avcodec_version },
        { "avformat", ffmpeg.avformat_version },
        { "avfilter", ffmpeg.avfilter_version }
    };

    ArgumentNullException.ThrowIfNull(config);

    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        DynamicallyLinkedBindings.Initialize();
    }
    else
    {
        if (config.FfmpegRoot is not null)
            DynamicallyLoadedBindings.LibrariesPath = config.FfmpegRoot;

        logger.LogInformation("Bind ffmpeg root path to {path}.", DynamicallyLoadedBindings.LibrariesPath);

        DynamicallyLoadedBindings.ThrowErrorIfFunctionNotFound = true;
        DynamicallyLoadedBindings.Initialize();
    }

    // test ffmpeg load
    try
    {
        var version = ffmpeg.av_version_info();
        logger.LogInformation("Load ffmpeg version {v}", version ?? "unknown");

        foreach (var (name, func) in libraryDict)
        {
            logger.LogInformation("Library {name} version {ver}", name, func.Invoke());
        }
    }
    catch (NotSupportedException e)
    {
        logger.LogCritical(e, "Failed to load ffmpeg, exit.");
    }
}
