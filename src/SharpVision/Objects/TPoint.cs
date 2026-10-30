using System;

namespace SharpVision.Objects;

/// <summary>
/// A point in screen (character cell) coordinates.
/// Ported from include/tvision/objects.h (class TPoint)
/// </summary>
/// <remarks>
/// Mirrors Turbo Vision's <c>TPoint</c>. It is a mutable value type, like the
/// original, so <c>+=</c>/<c>-=</c> update it in place.
/// </remarks>
/// <remarks>Creates a point at (<paramref name="x"/>, <paramref name="y"/>).</remarks>
public struct TPoint(int x, int y) : IEquatable<TPoint>
{
    /// <summary>Column.</summary>
    public int X = x;

    /// <summary>Row.</summary>
    public int Y = y;

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
    public readonly override bool Equals(object? obj) => obj is TPoint other && Equals(other);

    /// <inheritdoc />
    public readonly override int GetHashCode() => HashCode.Combine(X, Y);

    /// <inheritdoc />
    public readonly override string ToString() => $"({X}, {Y})";
}
