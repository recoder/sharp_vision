using SharpVision.Drawing;
using SharpVision.Events;

using static SharpVision.Platform.TDisplay;

namespace SharpVision.Platform;

/// <summary>
/// The screen manager: sets up the console, holds the screen buffer and dimensions, and
/// suspends and resumes the display.
/// </summary>
/// <remarks>
/// Upstream's constructor and destructor are <see cref="Initialize"/> and
/// <see cref="Done"/>; the application calls them.
/// </remarks>
// Ported from include/tvision/system.h and source/tvision/tscreen.cpp (class TScreen).
public static class TScreen
{
    /// <summary>The screen mode before the application started.</summary>
    public static ushort StartupMode { get; set; } = 0xFFFF;

    /// <summary>The cursor shape before the application started.</summary>
    public static ushort StartupCursor { get; set; }

    /// <summary>The current screen mode (<c>TDisplay.smXxx</c>).</summary>
    public static ushort ScreenMode { get; set; }

    /// <summary>Screen width in columns.</summary>
    public static ushort ScreenWidth { get; set; }

    /// <summary>Screen height in rows.</summary>
    public static ushort ScreenHeight { get; set; }

    /// <summary>Whether the screen has more than 25 rows.</summary>
    public static bool HiResScreen { get; set; }

    /// <summary>Unused; kept for compatibility (CGA snow checking).</summary>
    public static bool CheckSnow { get; set; } = true;

    /// <summary>
    /// The screen's cells. When a group's buffer is this array (the application's), writes
    /// to it are also sent to the display.
    /// </summary>
    public static TScreenCell[]? ScreenBuffer { get; set; }

    /// <summary>The normal cursor's size: the low byte is the percentage of the cell it fills.</summary>
    public static ushort CursorLines { get; set; }

    /// <summary>Whether to clear the screen when suspending.</summary>
    public static bool ClearOnSuspend { get; set; } = true;

    /// <summary>Sets up the console and the screen buffer (upstream's constructor).</summary>
    public static void Initialize()
    {
        THardwareInfo.SetUpConsole();
        StartupMode = GetCrtMode();
        StartupCursor = GetCursorType();
        ScreenBuffer = THardwareInfo.AllocateScreenBuffer();
        SetCrtData();
    }

    /// <summary>Restores the console (upstream's destructor).</summary>
    public static void Done()
    {
        Suspend();
        THardwareInfo.FreeScreenBuffer(ScreenBuffer);
    }

    /// <summary>Takes back the console after <see cref="Suspend"/>.</summary>
    public static void Resume()
    {
        THardwareInfo.SetUpConsole();
        StartupMode = GetCrtMode();
        StartupCursor = GetCursorType();
        if (ScreenMode != StartupMode)
            SetCrtMode(ScreenMode);
        SetCrtData();
    }

    /// <summary>Gives the console back to the system (e.g. to run a shell).</summary>
    public static void Suspend()
    {
        if (StartupMode != ScreenMode)
            SetCrtMode(StartupMode);
        if (ClearOnSuspend)
            ClearScreen();
        SetCursorType(StartupCursor);
        THardwareInfo.RestoreConsole();
    }

    /// <summary>Adjusts a requested mode to one the display supports.</summary>
    public static ushort FixCrtMode(ushort mode) => mode;

    /// <summary>Reads the screen mode, size and cursor shape, and hides the cursor.</summary>
    public static void SetCrtData()
    {
        ScreenMode = GetCrtMode();
        ScreenWidth = GetCols();
        ScreenHeight = GetRows();
        HiResScreen = ScreenHeight > 25;
        CursorLines = GetCursorType();
        SetCursorType(0);
    }

    /// <summary>Clears the screen.</summary>
    public static void ClearScreen() => TDisplay.ClearScreen(ScreenWidth, ScreenHeight);

    /// <summary>Sends pending screen changes to the display.</summary>
    public static void FlushScreen() => THardwareInfo.FlushScreen();

    /// <summary>
    /// Changes the screen mode, or with <c>smUpdate</c> reloads the screen size (after the
    /// terminal was resized), reallocating the screen buffer.
    /// </summary>
    public static void SetVideoMode(ushort mode)
    {
        if (mode != smUpdate)
            SetCrtMode(FixCrtMode(mode));
        else
        {
            THardwareInfo.FreeScreenBuffer(ScreenBuffer);
            ScreenBuffer = THardwareInfo.AllocateScreenBuffer();
        }
        SetCrtData();
        if (TMouse.Present())
            TMouse.SetRange((ushort)(GetCols() - 1), (ushort)(GetRows() - 1));
    }
}
