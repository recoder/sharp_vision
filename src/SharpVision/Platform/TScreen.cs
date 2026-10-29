// Ported from include/tvision/system.h (class TScreen).

using SharpVision.Drawing;

namespace SharpVision.Platform;

/// <summary>
/// The screen manager: screen dimensions and display state.
/// </summary>
/// <remarks>
/// TBD: only the members needed by already-ported types exist so far. The rest
/// (screen mode, resume/suspend, flushing) arrives with the platform layer.
/// </remarks>
public static class TScreen
{
    /// <summary>Screen width in columns.</summary>
    public static ushort ScreenWidth { get; set; }

    /// <summary>Screen height in rows.</summary>
    public static ushort ScreenHeight { get; set; }

    /// <summary>
    /// The screen's cells. When a group's buffer is this array (the application's), writes
    /// to it are also sent to the display.
    /// </summary>
    public static TScreenCell[]? ScreenBuffer { get; set; }

    /// <summary>The normal cursor's size: the low byte is the percentage of the cell it fills.</summary>
    public static ushort CursorLines { get; set; }
}
