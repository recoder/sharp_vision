using System;

using SharpVision.Drawing;

namespace SharpVision.Platform;

/// <summary>
/// The display operations <see cref="THardwareInfo"/> forwards to (upstream's <c>Platform</c>
/// instance). Implemented by console backends and by test doubles.
/// </summary>
internal interface IDisplayOutput
{
    /// <summary>Writes <paramref name="cells"/> to row <paramref name="y"/> starting at column <paramref name="x"/>.</summary>
    void ScreenWrite(int x, int y, ReadOnlySpan<TScreenCell> cells);

    /// <summary>Moves the caret.</summary>
    void SetCaretPosition(int x, int y);

    /// <summary>Sets the caret size, 1-100 (percentage of the cell), or 0 to hide it.</summary>
    void SetCaretSize(int size);
}
