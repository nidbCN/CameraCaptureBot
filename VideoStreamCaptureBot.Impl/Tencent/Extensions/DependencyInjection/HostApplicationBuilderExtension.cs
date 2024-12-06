using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoStreamCaptureBot.Impl.Tencent.Utils.Sign;

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
