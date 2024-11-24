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
    // ReSharper disable StringLiteralTypo
    private static readonly IDictionary<string, Func<uint>> LibrariesInfos = new Dictionary<string, Func<uint>>
    {
        { "avutil", ffmpeg.avutil_version },
        { "swscale", ffmpeg.swscale_version },
        { "swresample", ffmpeg.swresample_version },
        { "postproc", ffmpeg.postproc_version },
        { "avcodec", ffmpeg.avcodec_version },
        { "avformat", ffmpeg.avformat_version },
        { "avfilter", ffmpeg.avfilter_version }
    };

    private void ConfigureFfMpegLibrary()
    {
        // use linked
        if (options.Value.FfmpegRoot is null)
        {
            logger.LogInformation("ffmpeg library path not set, use {bind}.", nameof(FFmpeg.AutoGen.Bindings.DynamicallyLinked));

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                {
                    NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), LinuxFfMpegDllImportResolver);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), MacOsFfMpegDllImportResolver);
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }
            }

            DynamicallyLinkedBindings.Initialize();
        }
        //else
        {
            DynamicallyLoadedBindings.LibrariesPath = options.Value.FfmpegRoot;

            if (options.Value.FfmpegRoot == string.Empty)
            {
                logger.LogInformation("ffmpeg library path set to system default search path, use {bind}.",
                    nameof(FFmpeg.AutoGen.Bindings.DynamicallyLoaded));
            }
            else
            {
                logger.LogInformation("ffmpeg library path set to {path}, use {bind}.",
                    DynamicallyLoadedBindings.LibrariesPath = options.Value.FfmpegRoot,
                    nameof(FFmpeg.AutoGen.Bindings.DynamicallyLoaded));
            }

            DynamicallyLoadedBindings.ThrowErrorIfFunctionNotFound = true;
            DynamicallyLoadedBindings.Initialize();
        }

        // test ffmpeg load
        try
        {
            var version = ffmpeg.av_version_info();
            var libraryVersion = new StringBuilder(48 * LibrariesInfos.Count);

            foreach (var (name, func) in LibrariesInfos)
            {
                var libVersion = new FfMpegVersionStruct(func());

                libraryVersion.AppendLine($"\tLibrary {name} version {libVersion}.");
            }

            libraryVersion.Remove(libraryVersion.Length - 1, 1);    // remove '\n'

            logger.LogInformation("Load ffmpeg, version {v}", version);
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
        var partedName = libraryName.Split('-');

        if (partedName.Length != 2)
            return NativeLibrary.Load(libraryName, assembly, searchPath);

        var unixStyleName = $"{partedName[0]}.{extension}.{partedName[1]}";
        return NativeLibrary.Load(unixStyleName, assembly, searchPath);
    }

    public Task StartAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task StartingAsync(CancellationToken cancellationToken)
    {
        ConfigureFfMpegLibrary();

        return Task.CompletedTask;
    }

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
        [FieldOffset(0)] public ushort Major;
        [FieldOffset(2)] public byte Minor;
        [FieldOffset(3)] public byte Patch;

        public override string ToString()
            => $"{Major}.{Minor}.{Patch}";
    }
}
