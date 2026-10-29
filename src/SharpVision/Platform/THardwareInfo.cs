// Ported from include/tvision/hardware.h and source/platform/hardware.cpp (class THardwareInfo).

using System;

using SharpVision.Drawing;

namespace SharpVision.Platform;

/// <summary>
/// Low-level access to the display.
/// </summary>
/// <remarks>
/// TBD: only the members needed by already-ported types exist so far. Until a backend is
/// installed in <see cref="Display"/>, the operations do nothing.
/// </remarks>
public static class THardwareInfo
{
    /// <summary>The display backend. Null when running headless.</summary>
    internal static IDisplayOutput? Display { get; set; }

    /// <summary>Sets the caret size, 1-100 (percentage of the cell), or 0 to hide it.</summary>
    public static void SetCaretSize(int size) => Display?.SetCaretSize(size);

    /// <summary>Moves the caret.</summary>
    public static void SetCaretPosition(int x, int y) => Display?.SetCaretPosition(x, y);

    /// <summary>Writes <paramref name="cells"/> to the screen at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public static void ScreenWrite(int x, int y, ReadOnlySpan<TScreenCell> cells) => Display?.ScreenWrite(x, y, cells);
}
