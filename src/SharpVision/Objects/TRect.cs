using System;

namespace SharpVision.Objects;

/// <summary>
/// A rectangle in screen coordinates. <see cref="A"/> is the top-left corner
/// (inclusive) and <see cref="B"/> is the bottom-right corner (exclusive).
/// Ported from include/tvision/objects.h (class TRect).
/// </summary>
/// <remarks>
/// Mirrors Turbo Vision's <c>TRect</c>. It is a mutable value type and the
/// <c>Move</c>/<c>Grow</c>/<c>Intersect</c>/<c>Union</c> methods modify it in
/// place, as in the original.
/// </remarks>
/// <remarks>Creates a rectangle from two corner points.</remarks>
public struct TRect(TPoint p1, TPoint p2) : IEquatable<TRect>
{
    /// <summary>Top-left corner (inclusive).</summary>
    public TPoint A = p1;

    /// <summary>Bottom-right corner (exclusive).</summary>
    public TPoint B = p2;

    /// <summary>Creates a rectangle from corner coordinates.</summary>
    public TRect(int ax, int ay, int bx, int by) : this(new TPoint(ax, ay), new TPoint(bx, by))
    { }

    /// <summary>Translates the rectangle by (<paramref name="dx"/>, <paramref name="dy"/>).</summary>
    public void Move(int dx, int dy)
    {
        A.X += dx;
        A.Y += dy;
        B.X += dx;
        B.Y += dy;
    }

    /// <summary>
    /// Enlarges the rectangle by <paramref name="dx"/> columns and <paramref name="dy"/>
    /// rows on each side. Negative values shrink it.
    /// </summary>
    public void Grow(int dx, int dy)
    {
        A.X -= dx;
        A.Y -= dy;
        B.X += dx;
        B.Y += dy;
    }

    /// <summary>Sets this rectangle to its intersection with <paramref name="r"/>.</summary>
    public void Intersect(TRect r)
    {
        A.X = Math.Max(A.X, r.A.X);
        A.Y = Math.Max(A.Y, r.A.Y);
        B.X = Math.Min(B.X, r.B.X);
        B.Y = Math.Min(B.Y, r.B.Y);
    }

    /// <summary>Sets this rectangle to the smallest rectangle containing it and <paramref name="r"/>.</summary>
    public void Union(TRect r)
    {
        A.X = Math.Min(A.X, r.A.X);
        A.Y = Math.Min(A.Y, r.A.Y);
        B.X = Math.Max(B.X, r.B.X);
        B.Y = Math.Max(B.Y, r.B.Y);
    }

    /// <summary>Whether <paramref name="p"/> lies inside the rectangle.</summary>
    public readonly bool Contains(TPoint p) =>
        p.X >= A.X && p.X < B.X && p.Y >= A.Y && p.Y < B.Y;

    /// <summary>Whether the rectangle covers no cells.</summary>
    public readonly bool IsEmpty() => A.X >= B.X || A.Y >= B.Y;

    /// <inheritdoc />
    public static bool operator ==(TRect one, TRect two) => one.Equals(two);

    /// <inheritdoc />
    public static bool operator !=(TRect one, TRect two) => !one.Equals(two);

    /// <inheritdoc />
    public readonly bool Equals(TRect other) => A == other.A && B == other.B;

    /// <inheritdoc />
    public readonly override bool Equals(object? obj) => obj is TRect other && Equals(other);

    /// <inheritdoc />
    public readonly override int GetHashCode() => HashCode.Combine(A, B);

    /// <inheritdoc />
    public readonly override string ToString() => $"[{A} - {B}]";
}
