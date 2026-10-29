// Ported from include/tvision/colors.h (class TColorConversion) and
// source/platform/colors.cpp.

using System;

namespace SharpVision.Drawing;

/// <summary>
/// Conversions between color encodings.
/// </summary>
/// <remarks>
/// <para>
/// No conversion from XTerm indices 0-15 to RGB is provided, because how those colors are
/// displayed depends on the terminal's color scheme. The conversion from RGB to XTerm 0-15
/// is an approximation based on hue. XTerm indices 16-255 are assumed to map to the RGB
/// values XTerm uses.
/// </para>
/// <para>magiblot/tvision extension (not part of Turbo Vision 2.0).</para>
/// </remarks>
public static class TColorConversion
{
    // RGB to XTerm16 conversion algorithm (from upstream colors.cpp):
    //
    // XTerm16 is a 4-bit RGBI palette, which is not regular. Convert RGB to HSL as an
    // intermediate step, decide whether the color should be approximated to grayscale,
    // then pick by lightness (gray levels, or dark/bright variants) and by hue.
    // Integer arithmetic only, with at most one integer division.

    private const int HuePrecision = 32;
    private const int HueMax = 6 * HuePrecision;

    private static readonly TColorXTerm[] _xterm256ToXTerm16 = InitXTerm256ToXTerm16();
    private static readonly TColorRGB[] _xterm256ToRGB = InitXTerm256ToRGB();

    /// <summary>Converts a BIOS color to the equivalent XTerm index 0-15 (swaps red and blue).</summary>
    public static TColorXTerm BIOStoXTerm16(TColorBIOS c)
    {
        var aux = c.Blue;
        c.Blue = c.Red;
        c.Red = aux;
        return (byte)c;
    }

    /// <summary>Converts an XTerm index 0-15 to the equivalent BIOS color.</summary>
    public static TColorBIOS XTerm16toBIOS(TColorXTerm idx) => (byte)BIOStoXTerm16((byte)idx);

    /// <summary>Approximates an XTerm index 16-255 with an index 0-15. Indices 0-15 map to themselves.</summary>
    public static TColorXTerm XTerm256toXTerm16(TColorXTerm idx) => _xterm256ToXTerm16[(byte)idx];

    /// <summary>Converts an XTerm index 16-255 to RGB. Indices 0-15 yield black.</summary>
    public static TColorRGB XTerm256toRGB(TColorXTerm idx) => _xterm256ToRGB[(byte)idx];

    /// <summary>Approximates an RGB color with an XTerm index 0-15.</summary>
    public static TColorXTerm RGBtoXTerm16(TColorRGB c) => RGBtoXTerm16Impl(c.Red, c.Green, c.Blue);

    /// <summary>Approximates an RGB color with an XTerm index 16-255.</summary>
    public static TColorXTerm RGBtoXTerm256(TColorRGB c)
    {
        // The 6x6x6 cube's channel values [0..5] correspond to [0, 95, 135, 175, 215, 255],
        // so dark colors are underrepresented. Values [55..74] are allowed to map to 1, and
        // the grayscale ramp is used whenever the cube would round the color to pure black.
        static byte Scale(byte c)
        {
            if (c < 75)
                c += 20; // Compensate for underrepresented dark colors.
            return (byte)((Math.Max(c, (byte)35) - 35) / 40);
        }

        static byte CnvColor(TColorRGB c) =>
            (byte)(16 + ((Scale(c.Red) * 6) + Scale(c.Green)) * 6 + Scale(c.Blue));

        static byte CnvGray(byte l)
        {
            if (l < 8 - 5)
                return 16; // Totally black.
            if (l >= 238 + 5)
                return 231; // Totally white.
            // L is now in the range [3..242] and maps to one of the 24 grayscale colors.
            return (byte)(232 + (Math.Max(l, (byte)3) - 3) / 10);
        }

        var idx = CnvColor(c);
        if (c != XTerm256toRGB(idx))
        {
            var xmin = Math.Min(Math.Min(c.Red, c.Green), c.Blue);
            var xmax = Math.Max(Math.Max(c.Red, c.Green), c.Blue);
            var chroma = xmax - xmin;
            // For low-chroma or dark colors which are not exactly representable
            // in the 6x6x6 color cube, use the grayscale palette.
            if (chroma < 12 || idx == 16)
                idx = CnvGray((byte)((xmax + xmin) / 2));
        }
        return idx;
    }

    private static (byte H, byte C, byte L) RGBtoHCL(byte r, byte g, byte b)
    {
        var xmin = Math.Min(Math.Min(r, g), b);
        var xmax = Math.Max(Math.Max(r, g), b);
        var v = xmax;
        var l = (byte)((xmax + xmin) / 2);
        var c = (byte)(xmax - xmin);
        var h = 0;
        if (c != 0)
        {
            if (v == r)
                h = (short)(HuePrecision * (g - b)) / c;
            else if (v == g)
                h = (short)(HuePrecision * (b - r)) / c + 2 * HuePrecision;
            else if (v == b)
                h = (short)(HuePrecision * (r - g)) / c + 4 * HuePrecision;

            if (h < 0)
                h += HueMax;
            else if (h >= HueMax)
                h -= HueMax;
        }
        return ((byte)h, c, l);
    }

    private static byte U8(double d) => (byte)(d * 255);

    private static TColorXTerm RGBtoXTerm16Impl(byte r, byte g, byte b)
    {
        var c = RGBtoHCL(r, g, b);

        if (c.C >= 12) // Color if Chroma >= 12.
        {
            ReadOnlySpan<byte> hueToXTerm = [1, 3, 2, 6, 4, 5];
            var hue = (c.H < HueMax - HuePrecision / 2
                          ? c.H + HuePrecision / 2
                          : c.H - (HueMax - HuePrecision / 2)) / HuePrecision;
            if (c.L < U8(0.5))
                return hueToXTerm[hue];
            if (c.L < U8(0.925))
                return (byte)(hueToXTerm[hue] + 8); // Bright: {9, 11, 10, 14, 12, 13}.
            return 15;
        }

        if (c.L < U8(0.25))
            return 0;
        if (c.L < U8(0.625))
            return 8;
        if (c.L < U8(0.875))
            return 7;
        return 15;
    }

    private static byte CubeLevel(int i) => (byte)(i != 0 ? 55 + i * 40 : 0);

    private static TColorXTerm[] InitXTerm256ToXTerm16()
    {
        var lut = new TColorXTerm[256];
        for (var i = 0; i < 16; ++i)
            lut[i] = (byte)i;
        for (var i = 0; i < 6; ++i)
        {
            for (var j = 0; j < 6; ++j)
            {
                for (var k = 0; k < 6; ++k)
                    lut[16 + (i * 6 + j) * 6 + k] = RGBtoXTerm16Impl(CubeLevel(i), CubeLevel(j), CubeLevel(k));
            }
        }
        for (var i = 0; i < 24; ++i)
        {
            var l = (byte)(i * 10 + 8);
            lut[232 + i] = RGBtoXTerm16Impl(l, l, l);
        }
        return lut;
    }

    private static TColorRGB[] InitXTerm256ToRGB()
    {
        // Indices 16..255 only.
        var lut = new TColorRGB[256];
        for (var i = 0; i < 6; ++i)
        {
            for (var j = 0; j < 6; ++j)
            {
                for (var k = 0; k < 6; ++k)
                    lut[16 + (i * 6 + j) * 6 + k] = new TColorRGB(CubeLevel(i), CubeLevel(j), CubeLevel(k));
            }
        }
        for (var i = 0; i < 24; ++i)
        {
            var l = (byte)(i * 10 + 8);
            lut[232 + i] = new TColorRGB(l, l, l);
        }
        return lut;
    }
}
