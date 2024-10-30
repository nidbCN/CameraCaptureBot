namespace CameraCaptureBot.Core.Utils;

public sealed class BinarySizeFormatter : IFormatProvider, ICustomFormatter
{
    public object? GetFormat(Type? formatType)
        => formatType == typeof(ICustomFormatter) ? this : null;

    public string Format(string? format, object? arg, IFormatProvider? formatProvider)
    {
        if (arg is uint size)
        {
            return FormatSize(size);
        }

        return arg?.ToString() ?? string.Empty;
    }

    private static string FormatSize(ulong size)
        => size switch
        {
            < 1024 => $"{size}B",
            < 1024 * 1024 => $"{size / 1024.0:F2}KB",
            < 1024 * 1024 * 1024 => $"{size / (1024 * 1024.0):F2}MB",
            < (long)1024 * 1024 * 1024 * 1024 => $"{size / (1024 * 1024 * 1024.0):F2}GB",
            _ => $"{size / (1024 * 1024 * 1024 * 1024.0):F2}TB"
        };

    private static string FormatSize(long size)
        => size < 0 ? size.ToString() : FormatSize((ulong)size);

    private static string FormatSize(uint size)
        => size switch
        {
            < 1024 => $"{size}B",
            < 1024 * 1024 => $"{size / 1024.0:F2}KB",
            < 1024 * 1024 * 1024 => $"{size / (1024 * 1024.0):F2}MB",
            _ => $"{size / (1024 * 1024 * 1024.0):F2}GB"
        };

    private static string FormatSize(int size)
        => size < 0 ? size.ToString() : FormatSize((uint)size);
}