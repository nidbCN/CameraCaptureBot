﻿using System.IO.IsolatedStorage;
using System.Text.Json;
using CameraCaptureBot.Core.Configs;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;

namespace CameraCaptureBot.Core.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    public static void AddIsoStorages(this IServiceCollection services)
    {
        services.AddSingleton(IsolatedStorageFile.GetStore(
                IsolatedStorageScope.User | IsolatedStorageScope.Application, null, null)
            );
    }

    public static void AddBots(this IServiceCollection services, Func<BotOption> config)
    {
        var botOption = config.Invoke();
        var isoStore = IsolatedStorageFile.GetStore(
            IsolatedStorageScope.User | IsolatedStorageScope.Application, null, null);

        var deviceInfo = isoStore.FileExists(botOption.DeviceInfoFile)
            ? JsonSerializer.Deserialize<BotDeviceInfo>(
                  isoStore.OpenFile(botOption.DeviceInfoFile, FileMode.Open, FileAccess.Read))
              ?? BotDeviceInfo.GenerateInfo()
            : BotDeviceInfo.GenerateInfo();
        deviceInfo.DeviceName = "linux-capture";

        var keyStore = isoStore.FileExists(botOption.KeyStoreFile)
            ? JsonSerializer.Deserialize<BotKeystore>(
                  isoStore.OpenFile(botOption.KeyStoreFile, FileMode.Open, FileAccess.Read))
              ?? new()
            : new();

        services.AddSingleton(BotFactory.Create(botOption.FrameworkConfig, deviceInfo, keyStore));
    }
}