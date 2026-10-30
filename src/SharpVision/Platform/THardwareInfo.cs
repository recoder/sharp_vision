using System;
using System.Diagnostics;

using SharpVision.Drawing;
using SharpVision.Events;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Platform;

/// <summary>
/// Low-level access to the console: screen, caret, keyboard and mouse events.
/// </summary>
/// <remarks>
/// Display operations go to <see cref="Display"/>, which is the platform backend once
/// <see cref="SetUpConsole"/> has run, and can be replaced by a test double.
/// </remarks>
/// Ported from include/tvision/hardware.h and source/platform/hardware.cpp (class THardwareInfo).
public static class THardwareInfo
{
    private const int EventQSize = 24;

    private static readonly TEvent[] _eventQ = new TEvent[EventQSize];
    private static int _eventCount;
    private static readonly bool _alwaysFlush = ConsoleCtl.EnvInt("TVISION_MAX_FPS", 0) < 0;

    /// <summary>Whether insert mode is on (toggled by the Ins key).</summary>
    public static bool InsertState { get; set; } = true;

    /// <summary>Where display operations go. Null until a backend or a test double is installed.</summary>
    internal static IDisplayOutput? Display { get; set; }

    private static Platform Platf => Platform.Instance;

    /// <summary>Sets the caret size, 1-100 (percentage of the cell), or 0 to hide it.</summary>
    public static void SetCaretSize(int size) => Display?.SetCaretSize(size);

    /// <summary>Moves the caret.</summary>
    public static void SetCaretPosition(int x, int y) => Display?.SetCaretPosition(x, y);

    /// <summary>The caret size, 1-100.</summary>
    public static int GetCaretSize() => Platf.GetCaretSize();

    /// <summary>Whether the caret is visible.</summary>
    public static bool IsCaretVisible() => Platf.IsCaretVisible();

    /// <summary>The screen height.</summary>
    public static ushort GetScreenRows() => (ushort)Platf.GetScreenRows();

    /// <summary>The screen width.</summary>
    public static ushort GetScreenCols() => (ushort)Platf.GetScreenCols();

    /// <summary>The screen mode (<c>TDisplay.smXxx</c>).</summary>
    public static ushort GetScreenMode() => Platf.GetScreenMode();

    /// <summary>Does nothing: the screen mode cannot be changed.</summary>
    public static void SetScreenMode(ushort mode)
    {
    }

    /// <summary>Clears the screen.</summary>
    public static void ClearScreen(int w, int h) => Platf.ClearScreen();

    /// <summary>Writes <paramref name="cells"/> to the screen at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public static void ScreenWrite(int x, int y, ReadOnlySpan<TScreenCell> cells)
    {
        Display?.ScreenWrite(x, y, cells);
        if (_alwaysFlush && Display == Platf)
            FlushScreen();
    }

    /// <summary>Allocates the screen buffer for the current screen size.</summary>
    public static TScreenCell[] AllocateScreenBuffer() => Platf.ReloadScreenInfo();

    /// <summary>Releases the screen buffer.</summary>
    public static void FreeScreenBuffer(TScreenCell[]? buffer) => Platf.FreeScreenBuffer();

    /// <summary>The number of mouse buttons. Turbo Vision only checks whether it is non-zero.</summary>
    public static int GetButtonCount() => 2;

    /// <summary>Shows the mouse cursor (nothing to do on terminals).</summary>
    public static void CursorOn()
    {
    }

    /// <summary>Hides the mouse cursor (nothing to do on terminals).</summary>
    public static void CursorOff()
    {
    }

    /// <summary>Sends pending screen changes to the display.</summary>
    public static void FlushScreen() => Platf.FlushScreen();

    /// <summary>Puts the console into the mode Turbo Vision needs, and starts sending display operations to it.</summary>
    public static void SetUpConsole()
    {
        Platf.SetUpConsole();
        Display = Platf;
    }

    /// <summary>Restores the console to the state it was in before <see cref="SetUpConsole"/>.</summary>
    public static void RestoreConsole() => Platf.RestoreConsole();

    private static bool GetPendingEvent(ref TEvent ev, bool mouse)
    {
        for (var i = 0; i < _eventCount; ++i)
        {
            if (((_eventQ[i].What & evMouse) != 0) == mouse)
            {
                ev = _eventQ[i];
                for (; i + 1 < _eventCount; ++i)
                    _eventQ[i] = _eventQ[i + 1];
                --_eventCount;
                return true;
            }
        }
        return false;
    }

    /// <summary>Returns the next mouse state report, if any.</summary>
    public static bool GetMouseEvent(ref MouseEventType ev)
    {
        var e = new TEvent();
        if (GetPendingEvent(ref e, true))
        {
            ev = e.Mouse;
            return true;
        }
        return false;
    }

    /// <summary>Returns the next keyboard (or other non-mouse) event, if any.</summary>
    public static bool GetKeyEvent(ref TEvent ev)
    {
        ReadEvents();
        if (GetPendingEvent(ref ev, false))
        {
            if ((ev.What & evKeyboard) != 0)
            {
                if (ev.KeyDown.KeyCode == kbIns)
                    InsertState = !InsertState;
                if (InsertState)
                    ev.KeyDown.ControlKeyState |= kbInsState;
            }
            return ev.What != evNothing;
        }
        return false;
    }

    /// <summary>Discards nothing: kept for compatibility with upstream's <c>clearPendingEvent</c>.</summary>
    public static void ClearPendingEvent()
    {
    }

    private static void ReadEvents()
    {
        // Do not read any more events until the queue is empty.
        if (_eventCount == 0)
        {
            while (_eventCount < EventQSize)
            {
                _eventQ[_eventCount] = default;
                if (!Platf.GetEvent(ref _eventQ[_eventCount]))
                    break;
                ++_eventCount;
            }
        }
    }

    /// <summary>
    /// Waits until an event arrives or <paramref name="timeoutMs"/> elapse (forever if
    /// negative). The screen is flushed first, since all pending events have been processed.
    /// </summary>
    public static void WaitForEvents(int timeoutMs)
    {
        if (_eventCount == 0)
        {
            FlushScreen();
            Platf.WaitForEvents(timeoutMs);
        }
    }

    /// <summary>Makes a pending <see cref="WaitForEvents"/> return. Safe to call from any thread.</summary>
    public static void InterruptEventWait() => Platf.InterruptEventWait();

    /// <summary>Milliseconds from an arbitrary starting point.</summary>
    public static ulong GetTickCountMs() => (ulong)(Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency);

    /// <summary>The time in BIOS clock ticks (about 18.2 per second).</summary>
    public static uint GetTickCount() => (uint)(GetTickCountMs() / 55);
}
