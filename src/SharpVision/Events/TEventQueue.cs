using System;

using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Platform;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Events;

/// <summary>
/// Turns the console's raw mouse and keyboard reports into Turbo Vision events: mouse
/// down/up/move/auto-repeat/wheel with double and triple clicks, and key presses with
/// paste detection.
/// </summary>
/// <remarks>Upstream's constructor and destructor are <see cref="Initialize"/> and <see cref="Done"/>.</remarks>
// Ported from include/tvision/system.h and source/tvision/tevent.cpp (class TEventQueue).
public static class TEventQueue
{
    private const int MinPasteEventCount = 3;

    private static MouseEventType _lastMouse;
    private static MouseEventType _curMouse;
    private static MouseEventType _downMouse;
    private static uint _downTicks;
    private static bool _mouseEvents;
    private static bool _pendingMouseUp;
    private static uint _autoTicks;
    private static uint _autoDelay;

    private static string? _pasteText;
    private static int _pasteTextIndex;

    private static readonly TEvent[] _keyEventQueue = new TEvent[MinPasteEventCount];
    private static int _keyEventCount;
    private static int _keyEventIndex;
    private static bool _pasteState;
    private static bool _shouldSkipLf;

    /// <summary>The maximum interval, in clock ticks, between the clicks of a double click.</summary>
    public static ushort DoubleDelay { get; set; } = 8;

    /// <summary>The delay, in clock ticks, before a held mouse button starts sending <c>evMouseAuto</c>.</summary>
    public static ushort RepeatDelay { get; set; } = 8;

    /// <summary>Whether the left and right mouse buttons are swapped.</summary>
    public static bool MouseReverse { get; set; }

    /// <summary>Starts reading mouse events (upstream's constructor).</summary>
    public static void Initialize() => Resume();

    /// <summary>Stops reading mouse events and discards pending paste text (upstream's destructor).</summary>
    public static void Done()
    {
        Suspend();
        _pasteText = null;
    }

    /// <summary>Starts reading mouse events.</summary>
    public static void Resume()
    {
        if (!TMouse.Present())
            TMouse.Resume();
        if (!TMouse.Present())
            return;

        TMouse.GetEvent(ref _curMouse);
        _lastMouse = _curMouse;
        THardwareInfo.ClearPendingEvent();
        _mouseEvents = true;
        TMouse.SetRange((ushort)(TScreen.ScreenWidth - 1), (ushort)(TScreen.ScreenHeight - 1));
    }

    /// <summary>Stops reading mouse events.</summary>
    public static void Suspend() => TMouse.Suspend();

    /// <summary>Gets the next mouse event, or <c>evNothing</c>.</summary>
    public static void GetMouseEvent(ref TEvent ev)
    {
        if (_mouseEvents)
        {
            if (_pendingMouseUp)
            {
                ev.What = evMouseUp;
                ev.Mouse = _lastMouse;
                _lastMouse.Buttons = 0;
                _pendingMouseUp = false;
                return;
            }
            if (!GetMouseState(ref ev, out var ticks))
                return;

            ev.Mouse.EventFlags = 0;

            if (ev.Mouse.Buttons == 0 && _lastMouse.Buttons != 0)
            {
                if (ev.Mouse.Where == _lastMouse.Where)
                {
                    ev.What = evMouseUp;
                    var buttons = _lastMouse.Buttons;
                    _lastMouse = ev.Mouse;
                    ev.Mouse.Buttons = buttons;
                }
                else
                {
                    ev.What = evMouseMove;
                    var up = ev.Mouse;
                    var where = up.Where;
                    ev.Mouse = _lastMouse;
                    ev.Mouse.Where = where;
                    ev.Mouse.EventFlags |= meMouseMoved;
                    up.Buttons = _lastMouse.Buttons;
                    _lastMouse = up;
                    _pendingMouseUp = true;
                }
                return;
            }

            if (ev.Mouse.Buttons != 0 && _lastMouse.Buttons == 0)
            {
                if (ev.Mouse.Buttons == _downMouse.Buttons &&
                    ev.Mouse.Where == _downMouse.Where &&
                    ticks - _downTicks <= DoubleDelay)
                {
                    if ((_downMouse.EventFlags & (meDoubleClick | meTripleClick)) == 0)
                        ev.Mouse.EventFlags |= meDoubleClick;
                    else if ((_downMouse.EventFlags & meDoubleClick) != 0)
                    {
                        ev.Mouse.EventFlags &= unchecked((ushort)~meDoubleClick);
                        ev.Mouse.EventFlags |= meTripleClick;
                    }
                }

                _downMouse = ev.Mouse;
                _autoTicks = _downTicks = ticks;
                _autoDelay = RepeatDelay;
                ev.What = evMouseDown;
                _lastMouse = ev.Mouse;
                return;
            }

            ev.Mouse.Buttons = _lastMouse.Buttons;

            if (ev.Mouse.Wheel != 0)
            {
                ev.What = evMouseWheel;
                _lastMouse = ev.Mouse;
                return;
            }

            if (ev.Mouse.Where != _lastMouse.Where)
            {
                ev.What = evMouseMove;
                ev.Mouse.EventFlags |= meMouseMoved;
                _lastMouse = ev.Mouse;
                return;
            }

            if (ev.Mouse.Buttons != 0 && ticks - _autoTicks > _autoDelay)
            {
                _autoTicks = ticks;
                _autoDelay = 1;
                ev.What = evMouseAuto;
                _lastMouse = ev.Mouse;
                return;
            }
        }

        ev.What = evNothing;
    }

    // UPSTREAM: upstream temporarily stores the tick count in 'ev.what'; it is returned
    // separately here, since 'What' is only 16 bits wide.
    private static bool GetMouseState(ref TEvent ev, out uint ticks)
    {
        ev.What = evNothing;
        ticks = 0;

        if (!THardwareInfo.GetMouseEvent(ref _curMouse))
            return false;

        if (MouseReverse && _curMouse.Buttons != 0 && _curMouse.Buttons != 3)
            _curMouse.Buttons ^= 3;

        ticks = THardwareInfo.GetTickCount();
        ev.Mouse = _curMouse;
        return true;
    }

    /// <summary>
    /// Gets the next key event, or <c>evNothing</c>. Pasted text arrives as key events
    /// with <c>kbPaste</c> and no key code, with CR and CRLF converted to LF.
    /// </summary>
    public static void GetKeyEvent(ref TEvent ev)
    {
        GetKeyOrPasteEvent(ref ev);

        if (_shouldSkipLf)
        {
            _shouldSkipLf = false;
            // Skip a LF, since we had previously read a CR.
            if (ev.What == evKeyDown && (ev.KeyDown.ControlKeyState & kbPaste) != 0 &&
                ((ev.KeyDown.Text is null && ev.KeyDown.CharScan.CharCode == '\n') || ev.KeyDown.Text == "\n"))
                GetKeyOrPasteEvent(ref ev);
        }

        if (ev.What == evKeyDown && (ev.KeyDown.ControlKeyState & kbPaste) != 0)
        {
            ev.KeyDown.Text ??= ((char)ev.KeyDown.CharScan.CharCode).ToString();
            // Convert CR and CRLF into LF.
            if (ev.KeyDown.Text.StartsWith('\r'))
            {
                ev.KeyDown.Text = "\n" + ev.KeyDown.Text[1..];
                _shouldSkipLf = true;
            }
            ev.KeyDown.KeyCode = 0;
        }
    }

    /// <summary>Makes <paramref name="text"/> arrive as a sequence of paste key events.</summary>
    public static void SetPasteText(ReadOnlySpan<char> text)
    {
        // Always initialize the paste text, even if empty, so that WaitForEvents
        // won't block in the next call.
        _pasteText = text.ToString();
        _pasteTextIndex = 0;
    }

    private static bool GetPasteEvent(ref TEvent ev)
    {
        if (_pasteText is not null)
        {
            var text = _pasteText.AsSpan(_pasteTextIndex);
            var length = TText.Next(text);
            if (length > 0)
            {
                ev.What = evKeyDown;
                ev.KeyDown = new KeyDownEvent { ControlKeyState = kbPaste, Text = text[..length].ToString() };
                _pasteTextIndex += length;
                return true;
            }
            _pasteText = null;
        }
        return false;
    }

    private static bool IsTextEvent(in TEvent ev) =>
        ev.What == evKeyDown &&
        (ev.KeyDown.Text is not null || ev.KeyDown.KeyCode == kbEnter || ev.KeyDown.KeyCode == kbTab);

    private static void GetKeyOrPasteEvent(ref TEvent ev)
    {
        if (GetPasteEvent(ref ev))
            return;
        if (_keyEventCount == 0)
        {
            var firstNonText = MinPasteEventCount;
            for (var i = 0; i < MinPasteEventCount; ++i)
            {
                if (!ReadKeyPress(ref _keyEventQueue[i]))
                    break;
                ++_keyEventCount;
                if (!IsTextEvent(_keyEventQueue[i]))
                {
                    firstNonText = i;
                    break;
                }
            }
            // At least MinPasteEventCount consecutive text events mean that a paste began.
            if (_keyEventCount == MinPasteEventCount && firstNonText == MinPasteEventCount)
                _pasteState = true;
            if (_pasteState)
            {
                for (var i = 0; i < Math.Min(_keyEventCount, firstNonText); ++i)
                    _keyEventQueue[i].KeyDown.ControlKeyState |= kbPaste;
            }
            if (_keyEventCount < MinPasteEventCount || firstNonText < MinPasteEventCount)
                _pasteState = false;
            _keyEventIndex = 0;
        }
        if (_keyEventCount != 0)
        {
            ev = _keyEventQueue[_keyEventIndex];
            ++_keyEventIndex;
            --_keyEventCount;
        }
        else
            ev.What = evNothing;
    }

    private static bool ReadKeyPress(ref TEvent ev)
    {
        ev = default;
        if (!THardwareInfo.GetKeyEvent(ref ev))
            ev.What = evNothing;
        return ev.What != evNothing;
    }

    /// <summary>Waits for events, unless some are already queued (see <see cref="THardwareInfo.WaitForEvents"/>).</summary>
    public static void WaitForEvents(int timeoutMs)
    {
        if (_pasteText is null && _keyEventCount == 0)
            THardwareInfo.WaitForEvents(timeoutMs);
    }

    /// <summary>Makes a pending <see cref="WaitForEvents"/> return. Safe to call from any thread.</summary>
    public static void WakeUp() => THardwareInfo.InterruptEventWait();
}
