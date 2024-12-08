﻿using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;

namespace VideoStreamCaptureBot.Core.Extensions;
public static class FfMpegExtension
{
    public static unsafe string? av_strerror(int error)
    {
        const int bufferSize = 1024;
        var buffer = stackalloc byte[bufferSize];
        ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
        var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
        return message;
    }

    public static int ThrowExceptionIfError(this int error)
    {
        if (error < 0) throw new ApplicationException(av_strerror(error));
        return error;
    }
}
