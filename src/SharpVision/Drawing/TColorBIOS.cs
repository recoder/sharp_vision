// Ported from include/tvision/colors.h (class TColorBIOS).

using System;

namespace SharpVision.Drawing;

/// <summary>
/// The 4-bit IRGB color encoding used originally by Turbo Vision on DOS. Terminal
/// emulators display it using the 16 basic ANSI colors. When converting from an
/// integer, the bits above the lower 4 are discarded.
/// </summary>
public struct TColorBIOS : IEquatable<TColorBIOS>
{
    private byte _irgb;

    /// <summary>Creates a color from an IRGB nibble. Bits 4 and above are discarded.</summary>
    public TColorBIOS(byte irgb)
    {
        _irgb = (byte)(irgb & 0xF);
    }

    /// <summary>Blue bit.</summary>
    public bool Blue
    {
        readonly get => (_irgb & 0x1) != 0;
        set => SetBit(0x1, value);
    }

    /// <summary>Green bit.</summary>
    public bool Green
    {
        readonly get => (_irgb & 0x2) != 0;
        set => SetBit(0x2, value);
    }

    /// <summary>Red bit.</summary>
    public bool Red
    {
        readonly get => (_irgb & 0x4) != 0;
        set => SetBit(0x4, value);
    }

    /// <summary>Intensity bit.</summary>
    public bool Intensity
    {
        readonly get => (_irgb & 0x8) != 0;
        set => SetBit(0x8, value);
    }

    private void SetBit(int mask, bool value) =>
        _irgb = (byte)(value ? _irgb | mask : _irgb & ~mask);

    /// <summary>Converts an IRGB nibble to a color.</summary>
    public static implicit operator TColorBIOS(byte irgb) => new(irgb);

    /// <summary>Converts the color to its IRGB nibble.</summary>
    public static implicit operator byte(TColorBIOS c) => c._irgb;

    /// <inheritdoc />
    public static bool operator ==(TColorBIOS a, TColorBIOS b) => a._irgb == b._irgb;

    /// <inheritdoc />
    public static bool operator !=(TColorBIOS a, TColorBIOS b) => a._irgb != b._irgb;

    /// <inheritdoc />
    public readonly bool Equals(TColorBIOS other) => _irgb == other._irgb;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is TColorBIOS other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => _irgb;

    /// <inheritdoc />
    public override readonly string ToString() => $"BIOS 0x{_irgb:X}";
}
