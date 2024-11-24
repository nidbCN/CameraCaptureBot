using System.IO.IsolatedStorage;
using System.Text.Json;
using CameraCaptureBot.Core.Configs;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;

namespace CameraCaptureBot.Core.Extensions.DependencyInjection;

public static class ServiceCollectionExtension
{
    private static readonly Random RandomGen = new();

    public static void AddIsoStorages(this IServiceCollection services)
    {
        services.AddSingleton(IsolatedStorageFile.GetStore(
            IsolatedStorageScope.User | IsolatedStorageScope.Application, null, null)
        );
    }

    public static void AddBots(this IServiceCollection services, Func<BotOption> config)
    {
        var botOption = config.Invoke()
            ?? throw new ArgumentNullException(nameof(config));
        using var isoStore = IsolatedStorageFile.GetStore(
            IsolatedStorageScope.User | IsolatedStorageScope.Application, null, null);

        var deviceInfo = ReadAsJsonOrDelete<BotDeviceInfo>(isoStore, botOption.DeviceInfoFile)
            ?? GenerateInfo();

        var keyStore = ReadAsJsonOrDelete<BotKeystore>(isoStore, botOption.KeyStoreFile) 
                       ?? new();

        services.AddSingleton(BotFactory.Create(botOption.FrameworkConfig, deviceInfo, keyStore));

        isoStore.Close();
    }

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