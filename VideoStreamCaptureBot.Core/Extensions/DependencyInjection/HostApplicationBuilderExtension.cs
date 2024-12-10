﻿using System.IO.IsolatedStorage;
using System.Text.Json;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using VideoStreamCaptureBot.Core.Bots.LagrangeBot;
using VideoStreamCaptureBot.Core.Configs;

namespace VideoStreamCaptureBot.Core.Extensions.DependencyInjection;
public static class HostApplicationBuilderExtension
{
    public static IHostApplicationBuilder UseLagrangeBots(this IHostApplicationBuilder builder)
    {
        var option = builder.Configuration
            .GetSection(nameof(BotOption))
            .Get<BotOption>() ?? new();

        var isoStore = IsolatedStorageFile.GetStore(
            IsolatedStorageScope.User | IsolatedStorageScope.Application, null, null);

        var deviceInfo = ReadAsJsonOrDelete<BotDeviceInfo>(isoStore, option.LagrangeBotConfig.DeviceInfoFile)
                         ?? GenerateInfo();

        var keyStore = ReadAsJsonOrDelete<BotKeystore>(isoStore, option.LagrangeBotConfig.DeviceInfoFile)
                       ?? new();

        builder.Services.AddSingleton(_
            => isoStore);
        builder.Services.AddSingleton(_
            => BotFactory.Create(option.LagrangeBotConfig.LagrangeConfig, deviceInfo, keyStore));

        builder.Services.AddOptions();
        builder.Services.Configure<BotOption>(
            builder.Configuration.GetRequiredSection(nameof(BotOption)));

        builder.Services.AddHostedService<LagrangeHost>();

        return builder;
    }

    private static readonly Random RandomGen = new();

    private static BotDeviceInfo GenerateInfo()
    {
        var macAddress = new byte[6];
        RandomGen.NextBytes(macAddress);

        return new()
        {
            Guid = Guid.NewGuid(),
            MacAddress = macAddress,
            DeviceName = "linux-capture",
            SystemKernel = "Ubuntu 24.04.1 LTS",
            KernelVersion = "6.8.0-48-generic"
        };
    }

    private static T? ReadAsJsonOrDelete<T>(IsolatedStorageFile handler, string filename) where T : new()
    {
        if (!handler.FileExists(filename))
            return default;

        try
        {
            using var stream = handler.OpenFile(filename, FileMode.Open, FileAccess.Read);
            return JsonSerializer.Deserialize<T>(stream) ?? new();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            handler.DeleteFile(filename);
            return default;
        }
    }
}
