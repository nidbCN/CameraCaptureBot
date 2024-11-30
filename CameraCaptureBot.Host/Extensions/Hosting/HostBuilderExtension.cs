using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraCaptureBot.Host.Extensions.Hosting;
public static class HostBuilderExtension
{
    public static IHostBuilder UseTencentBot(this IHostBuilder hostBuilder)
    {
        hostBuilder.ConfigureLogging(c => { });

        return hostBuilder;
    }

    public static IHostBuilder UseLagrangeBot(this IHostBuilder hostBuilder)
    {
        return hostBuilder;
    }

    public static IHostBuilder UseFfMpeg(this IHostBuilder hostBuilder)
    {
        return hostBuilder;
    }
}

