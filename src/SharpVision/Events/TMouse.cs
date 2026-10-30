using SharpVision.Platform;

namespace SharpVision.Events;

/// <summary>The mouse: presence, visibility and state.</summary>
/// <remarks>Upstream's <c>THWMouse</c> base is merged in.</remarks>
// Ported from include/tvision/system.h and source/tvision/tmouse.cpp (THWMouse, TMouse).
public static class TMouse
{
    private static int _buttonCount;

    /// <summary>Detects the mouse and shows its cursor (upstream's constructor).</summary>
    public static void Resume()
    {
        _buttonCount = THardwareInfo.GetButtonCount();
        Show();
    }

    /// <summary>Hides the mouse cursor and stops using the mouse.</summary>
    public static void Suspend()
    {
        Hide();
        _buttonCount = 0;
    }

    /// <summary>Shows the mouse cursor.</summary>
    public static void Show() => THardwareInfo.CursorOn();

    /// <summary>Hides the mouse cursor.</summary>
    public static void Hide() => THardwareInfo.CursorOff();

    /// <summary>Limits the mouse position (has no effect on terminals).</summary>
    public static void SetRange(ushort rx, ushort ry)
    {
    }

    /// <summary>Reads the current mouse state into <paramref name="me"/>, if a new one is available.</summary>
    public static void GetEvent(ref MouseEventType me) => THardwareInfo.GetMouseEvent(ref me);

    /// <summary>Whether a mouse is present.</summary>
    public static bool Present() => _buttonCount != 0;
}
