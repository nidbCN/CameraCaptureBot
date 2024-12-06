using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace StreamCaptureBot.Utils;

[StructLayout(LayoutKind.Explicit)]
public struct ByteHex
{
    /// <summary>
    /// By Executor-Cheng 
    /// </summary>
    /// <see cref="https://github.com/KonataDev/Lagrange.Core/pull/344#pullrequestreview-2027515322"/>
    /// <param name="data">sign byte</param>
    /// <param name="casing">0x200020u for lower, 0x00u for upper</param>
    /// <returns>High 16bit, Low 16 bit</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByteHex ByteToHex(byte data, HexCasing casing = HexCasing.LowerCase)
    {
        var difference = BitConverter.IsLittleEndian
            ? ((uint)data >> 4) + ((data & 0x0Fu) << 16) - 0x0089_0089u
            : ((data & 0xF0u) << 12) + (data & 0x0Fu) - 0x0089_0089u;
        return new()
        {
            TwoChar = ((((uint)-(int)difference & 0x0070_0070u) >> 4) + difference + 0x00B9_00B9u)
                      | (uint)casing
        };
    }

    [FieldOffset(0)] public uint TwoChar;
    [FieldOffset(0)] public char High;
    [FieldOffset(2)] public char Low;

    public override string ToString()
        => "0x" + High + Low;

    // ReSharper disable once MemberCanBePrivate.Local
    public enum HexCasing
    {
        LowerCase = 0x0020_0020,
        UpperCase = 0x00
    }
}