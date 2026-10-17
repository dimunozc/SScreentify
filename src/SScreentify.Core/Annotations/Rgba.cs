using System.Globalization;

namespace SScreentify.Core.Annotations;

public readonly record struct Rgba(byte R, byte G, byte B, byte A = 255)
{
    /// <summary>Parses "#RRGGBB" or "#AARRGGBB".</summary>
    public static Rgba Parse(string hex)
    {
        if (hex is null || hex.Length is not (7 or 9) || hex[0] != '#'
            || !uint.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var v))
            throw new FormatException($"Color no válido: '{hex}'");

        return hex.Length == 7
            ? new Rgba((byte)(v >> 16), (byte)(v >> 8), (byte)v)
            : new Rgba((byte)(v >> 16), (byte)(v >> 8), (byte)v, (byte)(v >> 24));
    }

    public string ToHex() => A == 255 ? $"#{R:X2}{G:X2}{B:X2}" : $"#{A:X2}{R:X2}{G:X2}{B:X2}";
}
