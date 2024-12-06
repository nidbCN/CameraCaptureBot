using VideoStreamCaptureBot.Impl.Tencent.Options;
using VideoStreamCaptureBot.Impl.Tencent.Utils.Sign;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace VideoStreamCaptureBot.Impl.Tencent.Extensions.DependencyInjection;
public static class HostApplicationBuilderExtension
{
    public static IHostApplicationBuilder UseTencentBots(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions();
        builder.Services.AddTransient<ISignProvider, SodiumSignProvider>();
        builder.Services.AddHostedService<TencentWebhookWorker>();

        return builder;
    }
}
