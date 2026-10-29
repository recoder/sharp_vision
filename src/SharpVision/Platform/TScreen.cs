// Ported from include/tvision/system.h (class TScreen).

namespace SharpVision.Platform;

/// <summary>
/// The screen manager: screen dimensions and display state.
/// </summary>
/// <remarks>
/// TBD: only the members needed by already-ported types exist so far. The rest
/// (screen mode, buffer, cursor, resume/suspend) arrives with the platform layer.
/// </remarks>
public static class TScreen
{
    /// <summary>Screen width in columns.</summary>
    public static ushort ScreenWidth { get; set; }

    /// <summary>Screen height in rows.</summary>
    public static ushort ScreenHeight { get; set; }
}
