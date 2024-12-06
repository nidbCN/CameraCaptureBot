using System.IO.IsolatedStorage;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CameraCaptureBot.Impl.Tencent.Extensions.DependencyInjection;
public static class HostApplicationBuilderExtension
{
    public static IHostApplicationBuilder UseTencentBots(this IHostApplicationBuilder builder)
    {

        return builder;
    }
}
