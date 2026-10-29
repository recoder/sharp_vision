// Ported from include/tvision/objects.h (class TPoint).

using System;

namespace SharpVision.Objects;

/// <summary>
/// A point in screen (character cell) coordinates.
/// </summary>
/// <remarks>
/// Mirrors Turbo Vision's <c>TPoint</c>. It is a mutable value type, like the
/// original, so <c>+=</c>/<c>-=</c> update it in place.
/// </remarks>
public struct TPoint : IEquatable<TPoint>
{
    /// <summary>Column.</summary>
    public int X;

    /// <summary>Row.</summary>
    public int Y;

    /// <summary>Creates a point at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public TPoint(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>Component-wise sum.</summary>
    public static TPoint operator +(TPoint one, TPoint two) => new(one.X + two.X, one.Y + two.Y);

    /// <summary>Component-wise difference.</summary>
    public static TPoint operator -(TPoint one, TPoint two) => new(one.X - two.X, one.Y - two.Y);

    /// <inheritdoc />
    public static bool operator ==(TPoint one, TPoint two) => one.Equals(two);

    /// <inheritdoc />
    public static bool operator !=(TPoint one, TPoint two) => !one.Equals(two);

    /// <inheritdoc />
    public readonly bool Equals(TPoint other) => X == other.X && Y == other.Y;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is TPoint other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => HashCode.Combine(X, Y);

    /// <inheritdoc />
    public override readonly string ToString() => $"({X}, {Y})";
}
