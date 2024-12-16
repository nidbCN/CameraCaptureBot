﻿using FFmpeg.AutoGen.Abstractions;

namespace StreamingCaptureBot.Core.FfMpeg.Net.Utils;

public static class TimeSpanUtil
{
    public static TimeSpan? FromFfmpeg(long value, AVRational timebase)
    {
        if (timebase.den <= 0 || timebase.num < 0 || value == ffmpeg.AV_NOPTS_VALUE)
        {
            timebase.num = 1;
            timebase.den = ffmpeg.AV_TIME_BASE;
        }

        var seconds = (double)(value * timebase.num) / timebase.den;
        return TimeSpan.FromSeconds(seconds);
    }
}
