// Ported from include/tvision/colors.h (class TColorXTerm).

using System;

namespace SharpVision.Drawing;

/// <summary>
/// Index into the 256-color palette of the <c>xterm-256color</c> terminal type.
/// Indices 0-15 are the basic ANSI colors, 16-231 a 6x6x6 RGB cube and 232-255 a
/// 24-step grayscale ramp.
/// </summary>
/// <remarks>magiblot/tvision extension (not part of Turbo Vision 2.0).</remarks>
public readonly struct TColorXTerm : IEquatable<TColorXTerm>
{
    private readonly byte _idx;

    /// <summary>Creates a color from a palette index.</summary>
    public TColorXTerm(byte idx)
    {
        _idx = idx;
    }

    /// <summary>Converts a palette index to a color.</summary>
    public static implicit operator TColorXTerm(byte idx) => new(idx);

    /// <summary>Converts the color to its palette index.</summary>
    public static implicit operator byte(TColorXTerm c) => c._idx;

    /// <inheritdoc />
    public static bool operator ==(TColorXTerm a, TColorXTerm b) => a._idx == b._idx;

    /// <inheritdoc />
    public static bool operator !=(TColorXTerm a, TColorXTerm b) => a._idx != b._idx;

    /// <inheritdoc />
    public bool Equals(TColorXTerm other) => _idx == other._idx;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TColorXTerm other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _idx;

    /// <inheritdoc />
    public override string ToString() => $"XTerm {_idx}";
}
