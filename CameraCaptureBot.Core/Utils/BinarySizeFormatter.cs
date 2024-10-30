using System.Drawing;

namespace CameraCaptureBot.Core.Utils;

public sealed class BinarySizeFormatter : IFormatProvider, ICustomFormatter
{
    public object? GetFormat(Type? formatType)
        => formatType == typeof(ICustomFormatter) ? this : null;

    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
     => arg switch
     {
         uint usize => FormatSize(usize),
         int size => size < 0 ? '-' + FormatSize((uint)-size) : FormatSize((uint)size),
         ulong uLongSize => FormatSize(uLongSize),
         long longSize => longSize < 0 ? '-' + FormatSize((ulong)-longSize) : FormatSize((ulong)longSize),
         _ => arg?.ToString() ?? string.Empty
     };

    private static string FormatSize(ulong size)
        => size switch
        {
            < 1024 => $"{size}B",
            < 1024 * 1024 => $"{size / 1024.0:F2}KB",
            < 1024 * 1024 * 1024 => $"{size / (1024 * 1024.0):F2}MB",
            < (long)1024 * 1024 * 1024 * 1024 => $"{size / (1024 * 1024 * 1024.0):F2}GB",
            _ => $"{size / (1024 * 1024 * 1024 * 1024.0):F2}TB"
        };

    private static string FormatSize(uint size)
        => size switch
        {
            < 1024 => $"{size}B",
            < 1024 * 1024 => $"{size / 1024.0:F2}KB",
            < 1024 * 1024 * 1024 => $"{size / (1024 * 1024.0):F2}MB",
            _ => $"{size / (1024 * 1024 * 1024.0):F2}GB"
        };
}