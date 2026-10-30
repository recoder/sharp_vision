namespace SharpVision.Platform;

/// <summary>Low-level display queries: size, mode and cursor shape.</summary>
/// Ported from include/tvision/system.h and source/tvision/tscreen.cpp (class TDisplay).
public static class TDisplay
{
    // videoModes

    /// <summary>Black and white, 80 columns.</summary>
    public const ushort smBW80 = 0x0002;

    /// <summary>Color, 80 columns.</summary>
    public const ushort smCO80 = 0x0003;

    /// <summary>Monochrome.</summary>
    public const ushort smMono = 0x0007;

    /// <summary>Small font (more rows).</summary>
    public const ushort smFont8x8 = 0x0100;

    /// <summary>256 colors are supported.</summary>
    public const ushort smColor256 = 0x0200;

    /// <summary>24-bit colors are supported.</summary>
    public const ushort smColorHigh = 0x0400;

    /// <summary>Reload the screen information without changing the mode.</summary>
    public const ushort smUpdate = 0x8000;

    /// <summary>Clears the screen.</summary>
    public static void ClearScreen(int w, int h) => THardwareInfo.ClearScreen(w, h);

    /// <summary>Sets the cursor shape: the low byte is the size in percent (0 hides it).</summary>
    public static void SetCursorType(ushort ct) => THardwareInfo.SetCaretSize(ct & 0xFF);

    /// <summary>The cursor size in percent.</summary>
    public static ushort GetCursorType() => (ushort)THardwareInfo.GetCaretSize();

    /// <summary>The screen height.</summary>
    public static ushort GetRows() => THardwareInfo.GetScreenRows();

    /// <summary>The screen width.</summary>
    public static ushort GetCols() => THardwareInfo.GetScreenCols();

    /// <summary>Sets the screen mode (has no effect on terminals).</summary>
    public static void SetCrtMode(ushort mode) => THardwareInfo.SetScreenMode(mode);

    /// <summary>The screen mode.</summary>
    public static ushort GetCrtMode() => THardwareInfo.GetScreenMode();
}
