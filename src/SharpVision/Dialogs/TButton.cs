// Ported from source/tvision/tbutton.cpp, source/tvision/tvtext1.cpp (shadows, markers),
// include/tvision/dialogs.h (class TButton).

using System;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Events.EventCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Dialogs;

/// <summary>A push button with a shadow that sends its command when pressed.</summary>
/// <remarks>
/// Palette layout: 1 = normal text, 2 = default text, 3 = selected text, 4 = disabled text,
/// 5-7 = shortcut, 8 = shadow.
/// </remarks>
public class TButton : TView
{
    private const ushort cmGrabDefault = 61;
    private const ushort cmReleaseDefault = 62;

    private const uint AnimationDurationMs = 100;

    private const string Shadows = "▄█▀";
    private const string Markers = "[]";

    private static readonly TPalette _palette = new([0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0E, 0x0E, 0x0F]);

    private object? _animationTimer;

    /// <summary>The title, with the hotkey between <c>'~'</c>; or null.</summary>
    public string? Title;

    /// <summary>The command sent when the button is pressed.</summary>
    protected ushort Command;

    /// <summary>The <c>bfXxx</c> flags.</summary>
    protected byte Flags;

    /// <summary>Whether the button is currently the default one (pressed by Enter).</summary>
    protected bool AmDefault;

    /// <summary>Creates a button that sends <paramref name="aCommand"/>, disabled if the command is.</summary>
    public TButton(TRect bounds, string? aTitle, ushort aCommand, ushort aFlags)
        : base(bounds)
    {
        Title = aTitle;
        Command = aCommand;
        Flags = (byte)aFlags;
        AmDefault = (aFlags & bfDefault) != 0;
        Options |= ofSelectable | ofFirstClick | ofPreProcess | ofPostProcess;
        EventMask |= evBroadcast;
        if (!CommandEnabled(aCommand))
            State |= sfDisabled;
    }

    /// <summary>Stops the press animation timer (upstream's destructor).</summary>
    public override void ShutDown()
    {
        KillTimer(_animationTimer);
        base.ShutDown();
    }

    /// <summary>Draws the button up.</summary>
    public override void Draw() => DrawState(false);

    private void DrawTitle(TDrawBuffer b, int s, int i, TAttrPair cButton, bool down)
    {
        int l, scOff;
        if ((Flags & bfLeftJust) != 0)
            l = 1;
        else
        {
            l = (s - Cstrlen(Title) - 1) / 2;
            if (l < 1)
                l = 1;
        }
        b.MoveCStr(i + l, Title, cButton);

        if (ShowMarkers && !down)
        {
            if ((State & sfSelected) != 0)
                scOff = 0;
            else if (AmDefault)
                scOff = 2;
            else
                scOff = 4;
            b.PutChar(0, SpecialChars[scOff]);
            b.PutChar(s, SpecialChars[scOff + 1]);
        }
    }

    /// <summary>Draws the button pressed (shifted right, no shadow) or up.</summary>
    public void DrawState(bool down)
    {
        TAttrPair cButton, cShadow;
        var b = new TDrawBuffer();

        if ((State & sfDisabled) != 0)
            cButton = GetColor(0x0404);
        else
        {
            cButton = GetColor(0x0501);
            if ((State & sfActive) != 0)
            {
                if ((State & sfSelected) != 0)
                    cButton = GetColor(0x0703);
                else if (AmDefault)
                    cButton = GetColor(0x0602);
            }
        }
        cShadow = GetColor(8);
        var s = Size.X - 1;
        var t = Size.Y / 2 - 1;
        var ch = ' ';
        for (var y = 0; y <= Size.Y - 2; y++)
        {
            int i;
            b.MoveChar(0, ' ', cButton, Size.X);
            b.PutAttribute(0, cShadow);
            if (down)
            {
                b.PutAttribute(1, cShadow);
                ch = ' ';
                i = 2;
            }
            else
            {
                b.PutAttribute(s, cShadow);
                if (ShowMarkers)
                    ch = ' ';
                else
                {
                    if (y == 0)
                        b.PutChar(s, Shadows[0]);
                    else
                        b.PutChar(s, Shadows[1]);
                    ch = Shadows[2];
                }
                i = 1;
            }

            if (y == t && Title is not null)
                DrawTitle(b, s, i, cButton, down);

            if (ShowMarkers && !down)
            {
                b.PutChar(1, Markers[0]);
                b.PutChar(s - 1, Markers[1]);
            }
            WriteLine(0, y, Size.X, 1, b);
        }
        b.MoveChar(0, ' ', cShadow, 2);
        b.MoveChar(2, ch, cShadow, s - 1);
        WriteLine(0, Size.Y - 1, Size.X, 1, b);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>Presses the button on click, hotkey, Space, or Enter (when default), and tracks default and command state.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        TPoint mouse;
        TRect clickRect;

        clickRect = GetExtent();
        clickRect.A.X++;
        clickRect.B.X--;
        clickRect.B.Y--;

        if (ev.What == evMouseDown)
        {
            mouse = MakeLocal(ev.Mouse.Where);
            if (!clickRect.Contains(mouse))
                ClearEvent(ref ev);
        }
        if ((Flags & bfGrabFocus) != 0)
            base.HandleEvent(ref ev);

        var c = HotKey(Title);
        switch (ev.What)
        {
            case evMouseDown:
                if ((State & sfDisabled) == 0)
                {
                    clickRect.B.X++;
                    var down = false;
                    do
                    {
                        mouse = MakeLocal(ev.Mouse.Where);
                        if (down != clickRect.Contains(mouse))
                        {
                            down = !down;
                            DrawState(down);
                        }
                    } while (MouseEvent(ref ev, evMouseMove));
                    if (down)
                    {
                        Press();
                        DrawState(false);
                    }
                }
                ClearEvent(ref ev);
                break;

            case evKeyDown:
                if (ev.KeyDown.KeyCode != 0 &&
                    (ev.KeyDown.KeyCode == GetAltCode(c) ||
                     (Owner!.Phase == PhaseType.phPostProcess &&
                      c != 0 &&
                      c == char.ToUpperInvariant((char)ev.KeyDown.CharScan.CharCode)) ||
                     ((State & sfFocused) != 0 &&
                      ev.KeyDown.CharScan.CharCode == ' ')))
                {
                    DrawState(true);
                    _animationTimer ??= SetTimer(AnimationDurationMs);
                    ClearEvent(ref ev);
                }
                break;

            case evBroadcast:
                switch (ev.Message.Command)
                {
                    case cmDefault:
                        if (AmDefault && (State & sfDisabled) == 0)
                        {
                            DrawState(true);
                            _animationTimer ??= SetTimer(AnimationDurationMs);
                            ClearEvent(ref ev);
                        }
                        break;

                    case cmGrabDefault:
                    case cmReleaseDefault:
                        if ((Flags & bfDefault) != 0)
                        {
                            AmDefault = ev.Message.Command == cmReleaseDefault;
                            DrawView();
                        }
                        break;

                    case cmCommandSetChanged:
                        SetState(sfDisabled, !CommandEnabled(Command));
                        DrawView();
                        break;

                    case cmTimerExpired:
                        if (_animationTimer is not null && ev.Message.InfoPtr == _animationTimer)
                        {
                            _animationTimer = null;
                            DrawState(false);
                            Press();
                            ClearEvent(ref ev);
                        }
                        break;
                }
                break;
        }
    }

    /// <summary>
    /// Makes a non-default button the default (or gives the default back to the
    /// <c>bfDefault</c> button) by broadcasting to the owner.
    /// </summary>
    public void MakeDefault(bool enable)
    {
        if ((Flags & bfDefault) == 0)
        {
            Message(Owner, evBroadcast, enable ? cmGrabDefault : cmReleaseDefault, this);
            AmDefault = enable;
            DrawView();
        }
    }

    /// <summary>Redraws on selection or activation changes and takes the default while focused.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if ((aState & (sfSelected | sfActive)) != 0)
            DrawView();
        if ((aState & sfFocused) != 0)
            MakeDefault(enable);
    }

    /// <summary>Sends the button's command, as an event or (with <c>bfBroadcast</c>) a broadcast to the owner.</summary>
    public virtual void Press()
    {
        Message(Owner, evBroadcast, cmRecordHistory, null);
        if ((Flags & bfBroadcast) != 0)
            Message(Owner, evBroadcast, Command, this);
        else
        {
            var e = new TEvent { What = evCommand };
            e.Message.Command = Command;
            e.Message.InfoPtr = this;
            PutEvent(ref e);
        }
    }
}
