using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CameraCaptureBot.Core.Configs;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLinked;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using Microsoft.Extensions.Options;

namespace CameraCaptureBot.Core;

public class FfMpegConfigureHost(ILogger<FfMpegConfigureHost> logger, IOptions<StreamOption> options)
    : IHostedLifecycleService
{
    private void ConfigureFfMpegLibrary()
    {
        // use linked
        if (options.Value.FfMpegLibrariesPath is null)
        {
            logger.LogInformation("ffmpeg library path not set, use {bind}.", nameof(FFmpeg.AutoGen.Bindings.DynamicallyLinked));

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var bindingAssembly = typeof(DynamicallyLinkedBindings).Assembly;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                {
                    logger.LogInformation("Linux/FreeBSD platform, use resolver {name} for assembly `{asm}`.", nameof(LinuxFfMpegDllImportResolver), bindingAssembly.GetName());
                    NativeLibrary.SetDllImportResolver(bindingAssembly, LinuxFfMpegDllImportResolver);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    logger.LogInformation("OSX platform, use resolver {name} for assembly `{asm}`.", nameof(LinuxFfMpegDllImportResolver), bindingAssembly.GetName());
                    NativeLibrary.SetDllImportResolver(bindingAssembly, MacOsFfMpegDllImportResolver);
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }
            }

            DynamicallyLinkedBindings.Initialize();
        }
        else
        {
            DynamicallyLoadedBindings.LibrariesPath = options.Value.FfMpegLibrariesPath;

            if (options.Value.FfMpegLibrariesPath == string.Empty)
            {
                logger.LogInformation("ffmpeg library path set to system default search path, use {bind}.",
                    nameof(FFmpeg.AutoGen.Bindings.DynamicallyLoaded));
            }
            else
            {
                logger.LogInformation("ffmpeg library path set to `{path}`, use {bind}.",
                    DynamicallyLoadedBindings.LibrariesPath = options.Value.FfMpegLibrariesPath,
                    nameof(FFmpeg.AutoGen.Bindings.DynamicallyLoaded));
            }

            DynamicallyLoadedBindings.ThrowErrorIfFunctionNotFound = true;
            DynamicallyLoadedBindings.Initialize();
        }

        // test ffmpeg load
        try
        {
            var version = ffmpeg.av_version_info();
            var libraryVersion = new StringBuilder(48 * DynamicallyLoadedBindings.LibraryVersionMap.Count);

            foreach (var (library, requiredVersion) in DynamicallyLoadedBindings.LibraryVersionMap)
            {
                var versionFunc = typeof(ffmpeg).GetMethod($"{library}_version");
                var versionInfo = new FfMpegVersionStruct((uint)(versionFunc?.Invoke(null, null) ?? 0u));

                libraryVersion.AppendLine($"\tLibrary: {library}, require `{requiredVersion}`, load `{versionInfo}`");
            }

            libraryVersion.Remove(libraryVersion.Length - 1, 1);    // remove '\n'

            logger.LogInformation("Load ffmpeg, version `{v}`", version);
            logger.LogInformation("Load ffmpeg, libraries:\n{libInfo}", libraryVersion);
        }
        catch (NotSupportedException e)
        {
            logger.LogCritical(e, "Failed to load ffmpeg, exit.");
        }
    }

    public static IntPtr LinuxFfMpegDllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        => FfMpegDllImportResolverCore(libraryName, assembly, searchPath, "so");

    public static IntPtr MacOsFfMpegDllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        => FfMpegDllImportResolverCore(libraryName, assembly, searchPath, "dylib");


    public static IntPtr FfMpegDllImportResolverCore(string libraryName, Assembly assembly, DllImportSearchPath? searchPath, string extension)
    {
        var libraryNameSpan = libraryName.AsSpan();
        var partedIndex = libraryNameSpan.IndexOf('-');

        if (libraryNameSpan.LastIndexOf('-') != partedIndex)
        {
            // format not ffmpeg library
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }

        var pureName = libraryNameSpan[..partedIndex];

        if (!DynamicallyLoadedBindings.LibraryVersionMap.ContainsKey(pureName.ToString()))
        {
            // not ffmpeg library
            return NativeLibrary.Load(libraryName, assembly, searchPath);
        }

        var versionName = libraryNameSpan[(partedIndex + 1)..];

        var styledName = $"{pureName}.{extension}.{versionName}";
        return NativeLibrary.Load(styledName, assembly, searchPath);
    }

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        ConfigureFfMpegLibrary();
        return Task.CompletedTask;
    }
    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StartedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppingAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    [StructLayout(LayoutKind.Explicit)]
    private struct FfMpegVersionStruct(uint version)
    {
        [FieldOffset(0)] public uint Version = version;
        [FieldOffset(2)] public ushort Major;
        [FieldOffset(1)] public byte Minor;
        [FieldOffset(0)] public byte Patch;

        public override string ToString()
            => $"{Major}.{Minor}.{Patch}";
    }
}
