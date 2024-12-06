using System.Security.Cryptography;

namespace StreamCaptureBot.Utils.Extensions;
public static class ByteExtension
{
    public static string ToHex(this byte b, ByteHex.HexCasing casing = ByteHex.HexCasing.LowerCase)
      => ByteHex.ByteToHex(b, casing).ToString();

    public static string ToHex(this byte[] bytes, ByteHex.HexCasing casing = ByteHex.HexCasing.LowerCase)
    {
        var buffer = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var hex = ByteHex.ByteToHex(bytes[i], casing);
            buffer[2 * i] = hex.High;
            buffer[2 * i + 1] = hex.Low;
        }

        return new(buffer);
    }

    public static string ToMd5Hex(this byte[] bytes, ByteHex.HexCasing casing = ByteHex.HexCasing.LowerCase) 
        => MD5.HashData(bytes).ToHex(casing);

}
