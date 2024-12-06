using Microsoft.Extensions.Hosting;

namespace CameraCaptureBot.Impl.Tencent.Extensions.DependencyInjection;
public static class HostApplicationBuilderExtension
{
    public static IHostApplicationBuilder UseTencentBots(this IHostApplicationBuilder builder)
    {

        return builder;
    }
}
