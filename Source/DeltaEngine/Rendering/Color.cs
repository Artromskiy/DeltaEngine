using System;
using System.Runtime.CompilerServices;

namespace Delta.Rendering;

internal class Color
{
    public byte r;
    public byte g;
    public byte b;
    public byte a;

    public Color(byte r, byte g, byte b, byte a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Color(float r, float g, float b, float a)
    {
        this.r = ByteFromFloat01(r);
        this.g = ByteFromFloat01(g);
        this.b = ByteFromFloat01(b);
        this.a = ByteFromFloat01(a);
    }
    public Color(string hexColor)
    {
        Span<char> chars = stackalloc char[9];
        hexColor.CopyTo(chars);
        chars = chars[1..];
        if (hexColor.Length == 6)
            chars[6] = chars[7] = 'F';

        r = byte.Parse(chars.Slice(0,2), System.Globalization.NumberStyles.HexNumber);
        g = byte.Parse(chars.Slice(2,2), System.Globalization.NumberStyles.HexNumber);
        b = byte.Parse(chars.Slice(4,2), System.Globalization.NumberStyles.HexNumber);
        a = byte.Parse(chars.Slice(6,2), System.Globalization.NumberStyles.HexNumber);
    }

    public static implicit operator uint(Color rgba) => Unsafe.As<Color, uint>(ref rgba);
    public static implicit operator float(Color rgba) => Unsafe.As<Color, float>(ref rgba);
    public static implicit operator Color(string hex) => new(hex);

    private static byte ByteFromFloat01(float f) => (byte)float.Round(float.Clamp(f, 0, 1) * 255f);
}
