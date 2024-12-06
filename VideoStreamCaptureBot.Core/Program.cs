using VideoStreamCaptureBot.Core;
using VideoStreamCaptureBot.Core.Codecs;
using VideoStreamCaptureBot.Core.Configs;
using VideoStreamCaptureBot.Core.Services;
using VideoStreamCaptureBot.Core.Utils;
using VideoStreamCaptureBot.Impl.Tencent.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

//builder.UseLagrangeBots();
builder.UseTencentBots();

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