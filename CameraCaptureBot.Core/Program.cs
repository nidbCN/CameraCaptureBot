using CameraCaptureBot.Core;
using CameraCaptureBot.Core.Codecs;
using CameraCaptureBot.Core.Configs;
using CameraCaptureBot.Core.Extensions.DependencyInjection;
using CameraCaptureBot.Core.Services;
using CameraCaptureBot.Core.Utils;

var builder = Host.CreateApplicationBuilder(args);

builder.UseLagrangeBots();

builder.Services.AddWindowsService(s =>
{
    s.ServiceName = "Live stream capture bot";
});

builder.Services.Configure<StreamOption>(
    builder.Configuration.GetRequiredSection(nameof(StreamOption)));
builder.Services.Configure<BotOption>(
    builder.Configuration.GetRequiredSection(nameof(BotOption)));

builder.Services.AddTransient<BinarySizeFormatter>();

builder.Services.AddSingleton<FfmpegLibWebpEncoder>();
builder.Services.AddSingleton<CaptureService>();

builder.Services.AddHostedService<FfMpegConfigureHost>();
builder.Services.AddHostedService<HeartBeatWorker>();

builder.Services.AddLogging();

builder.Build().Run();