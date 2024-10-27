using System.IO.IsolatedStorage;
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
        using var isoStore = IsolatedStorageFile.GetStore(
            IsolatedStorageScope.User | IsolatedStorageScope.Application, null, null);

        var deviceInfo = ReadAsJsonOrDelete<BotDeviceInfo>(isoStore, botOption.DeviceInfoFile);
        deviceInfo.DeviceName = "linux-capture";

        var keyStore = ReadAsJsonOrDelete<BotKeystore>(isoStore, botOption.KeyStoreFile);
        services.AddSingleton(BotFactory.Create(botOption.FrameworkConfig, deviceInfo, keyStore));
    }

    private static T ReadAsJsonOrDelete<T>(IsolatedStorageFile handler, string filename) where T : new()
    {
        if (!handler.FileExists(filename))
            return new();

        try
        {
            var stream = handler.OpenFile(filename, FileMode.Open, FileAccess.Read);
            return JsonSerializer.Deserialize<T>(stream) ?? new();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            handler.DeleteFile(filename);
            return new();
        }
    }
}