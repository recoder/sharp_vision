// Ported from include/tvision/internal/platform.h (class DisplayAdapter).

using System;

using SharpVision.Drawing;
using SharpVision.Objects;

namespace SharpVision.Platform;

/// <summary>Writes cells to an actual display. The base implementation does nothing.</summary>
internal class DisplayAdapter
{
    /// <summary>Re-reads the display size.</summary>
    public virtual TPoint ReloadScreenInfo() => default;

    /// <summary>The number of colors supported (0 for monochrome).</summary>
    public virtual int GetColorCount() => 0;

    /// <summary>The font size in pixels, or 0 if unknown.</summary>
    public virtual TPoint GetFontSize() => default;

    /// <summary>Writes one cell's text at <paramref name="pos"/>.</summary>
    public virtual void WriteCell(TPoint pos, ReadOnlySpan<char> text, TColorAttr attr, bool doubleWidth)
    {
    }

    /// <summary>Moves the caret.</summary>
    public virtual void SetCaretPosition(TPoint pos)
    {
    }

    /// <summary>Sets the caret size, 1-100, or 0 to hide it.</summary>
    public virtual void SetCaretSize(int size)
    {
    }

    /// <summary>Clears the display.</summary>
    public virtual void ClearScreen()
    {
    }

    /// <summary>Sends buffered output to the display.</summary>
    public virtual void Flush()
    {
    }
}
