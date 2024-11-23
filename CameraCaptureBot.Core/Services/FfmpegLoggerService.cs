using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Options;

namespace CameraCaptureBot.Core.Services;

public class FfmpegLoggerService
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly av_log_set_callback_callback? _logCallback;

    private readonly ILogger<FfmpegLoggerService> _logger;

    public FfmpegLoggerService(ILogger<FfmpegLoggerService> logger, IOptions<LoggerFilterOptions> loggerOptions)
    {
        _logger = logger;

        // 设置日志
        var level = loggerOptions.Value.MinLevel switch
        {
            LogLevel.Trace => ffmpeg.AV_LOG_TRACE,
            LogLevel.Debug => ffmpeg.AV_LOG_DEBUG,
            LogLevel.Information => ffmpeg.AV_LOG_INFO,
            LogLevel.Warning => ffmpeg.AV_LOG_WARNING,
            LogLevel.Error => ffmpeg.AV_LOG_ERROR,
            LogLevel.Critical => ffmpeg.AV_LOG_PANIC,
            LogLevel.None => ffmpeg.AV_LOG_QUIET,
            _ => ffmpeg.AV_LOG_INFO
        };

        unsafe
        {
            _logCallback = FfMpegLogInvoke;
            ffmpeg.av_log_set_level(level);
            ffmpeg.av_log_set_callback(_logCallback);
        }
    }

    unsafe void FfMpegLogInvoke(void* p0, int level, string format, byte* vl)
    {
        if (level > ffmpeg.av_log_get_level()) return;

        const int lineSize = 128;
        var lineBuffer = stackalloc byte[lineSize];
        var printPrefix = ffmpeg.AV_LOG_SKIP_REPEATED | ffmpeg.AV_LOG_PRINT_LEVEL;

        ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
        var line = Marshal.PtrToStringAnsi((IntPtr)lineBuffer);

        if (line is null) return;

        line = line.ReplaceLineEndings();

        using (_logger.BeginScope(nameof(ffmpeg)))
        {
            switch (level)
            {
                case ffmpeg.AV_LOG_PANIC:
                    _logger.LogCritical("{msg}", line);
                    break;
                case ffmpeg.AV_LOG_FATAL:
                    _logger.LogCritical("{msg}", line);
                    break;
                case ffmpeg.AV_LOG_ERROR:
                    _logger.LogError("{msg}", line);
                    break;
                case ffmpeg.AV_LOG_WARNING:
                    _logger.LogWarning("{msg}", line);
                    break;
                case ffmpeg.AV_LOG_INFO:
                    _logger.LogInformation("{msg}", line);
                    break;
                case ffmpeg.AV_LOG_VERBOSE:
                    _logger.LogInformation("{msg}", line);
                    break;
                case ffmpeg.AV_LOG_DEBUG:
                    _logger.LogDebug("{msg}", line);
                    break;
                case ffmpeg.AV_LOG_TRACE:
                    _logger.LogTrace("{msg}", line);
                    break;
                default:
                    _logger.LogWarning("Log unknown level:{level}, msg: {msg}", level, line);
                    break;
            }

        }
    }
}