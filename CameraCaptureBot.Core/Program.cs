using System.Globalization;
using System.Runtime.Serialization.Formatters.Binary;
using CameraCaptureBot.Core;
using CameraCaptureBot.Core.Codecs;
using CameraCaptureBot.Core.Configs;
using CameraCaptureBot.Core.Extensions.DependencyInjection;
using CameraCaptureBot.Core.Services;
using CameraCaptureBot.Core.Utils;
using FFmpeg.AutoGen;
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

builder.Services.AddScoped<IFormatProvider, BinarySizeFormatter>();

builder.Services.AddSingleton<FfmpegLoggerService>();
builder.Services.AddSingleton<FfmpegLibWebpEncoder>();
builder.Services.AddSingleton<CaptureService>();
builder.Services.AddIsoStorages();

builder.Services.AddBots(() => builder.Configuration
    .GetRequiredSection(nameof(BotOption))
    .Get<BotOption>()!);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var streamOption = host.Services.GetRequiredService<IOptions<StreamOption>>();

ConfigureFfMpeg(logger, streamOption.Value);

host.Run();
return;

static void ConfigureFfMpeg(ILogger logger, StreamOption config)
{
    ArgumentNullException.ThrowIfNull(config);

    logger.LogInformation("Bind ffmpeg root path to {path}.", ffmpeg.RootPath);

    // config ffmpeg
    ffmpeg.RootPath = config.FfmpegRoot;

    DynamicallyLoadedBindings.Initialize();

    // test ffmpeg load
    try
    {
        var version = ffmpeg.av_version_info();
        logger.LogInformation("Load ffmpeg version {v}", version ?? "unknown");
    }
    catch (NotSupportedException e)
    {
        logger.LogCritical(e, "Failed to load ffmpeg, exit.");
    }
}
