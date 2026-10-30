using System;

using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Menus;
using SharpVision.Objects;
using SharpVision.Platform;
using SharpVision.Views;

using static SharpVision.App.AppPalettes;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.App;

/// <summary>
/// The application object without the console setup: a group covering the screen that holds
/// the desktop, status line and menu bar, and runs the main event loop.
/// </summary>
/// <remarks>
/// Upstream's <c>TProgInit</c> virtual base, which receives the factory functions for the
/// status line, menu bar and desktop, becomes the constructor's delegate parameters.
/// </remarks>
/// Ported from include/tvision/app.h (TProgInit, TProgram), source/tvision/tprogram.cpp and
/// source/tvision/tvtext2.cpp (exitText).
public class TProgram : TGroup
{
    /// <summary>The text of the default status line's exit item.</summary>
    public const string ExitText = "~Alt-X~ Exit";

    private static readonly TPalette[] _palettes =
    [
        new(cpAppColor),
        new(cpAppBlackWhite),
        new(cpAppMonochrome),
    ];

    /// <summary>The running application, or null.</summary>
    public static TProgram? Application;

    /// <summary>The status line, or null.</summary>
    public static TStatusLine? StatusLine;

    /// <summary>The menu bar, or null.</summary>
    public static TMenuBar? MenuBar;

    /// <summary>The desktop, or null.</summary>
    public static TDeskTop? DeskTop;

    /// <summary>The palette in use (<c>apColor</c>, <c>apBlackWhite</c> or <c>apMonochrome</c>).</summary>
    public static int AppPalette = apColor;

    /// <summary>How long to wait for events before calling <see cref="Idle"/>, in milliseconds (negative waits forever).</summary>
    public static int EventTimeoutMs = 20; // 50 wake-ups per second.

    /// <summary>The event put back with <see cref="PutEvent"/>.</summary>
    protected static TEvent Pending;

    /// <summary>The timers started with <see cref="SetTimer"/>.</summary>
    protected static TTimerQueue TimerQueue = new();

    /// <summary>Creates the application with the default status line, menu bar and desktop.</summary>
    public TProgram()
        : this(InitStatusLine, InitMenuBar, InitDeskTop)
    {
    }

    /// <summary>
    /// Creates the application, building its parts with the given factories (null to omit a
    /// part). The screen must already be initialized (see <see cref="TApplication"/>).
    /// </summary>
    public TProgram(
        Func<TRect, TStatusLine?>? createStatusLine,
        Func<TRect, TMenuBar?>? createMenuBar,
        Func<TRect, TDeskTop?>? createDeskTop)
        : base(new TRect(0, 0, TScreen.ScreenWidth, TScreen.ScreenHeight))
    {
        Application = this;
        InitScreen();
        State = sfVisible | sfSelected | sfFocused | sfModal | sfExposed;
        Options = 0;
        Buffer = TScreen.ScreenBuffer;

        if (createDeskTop is not null && (DeskTop = createDeskTop(GetExtent())) is not null)
            Insert(DeskTop);
        if (createStatusLine is not null && (StatusLine = createStatusLine(GetExtent())) is not null)
            Insert(StatusLine);
        if (createMenuBar is not null && (MenuBar = createMenuBar(GetExtent())) is not null)
            Insert(MenuBar);
    }

    /// <inheritdoc />
    public override void ShutDown()
    {
        StatusLine = null;
        MenuBar = null;
        DeskTop = null;
        base.ShutDown();
        Application = null;
    }

    /// <summary>Whether the desktop lets the focus move to another window.</summary>
    public virtual bool CanMoveFocus() => DeskTop!.Valid(cmReleasedFocus);

    private static int EventWaitTimeout()
    {
        var timerTimeoutMs = TimerQueue.TimeUntilNextTimeout();
        if (timerTimeoutMs < 0)
            return EventTimeoutMs;
        if (EventTimeoutMs < 0)
            return timerTimeoutMs;
        return Math.Min(EventTimeoutMs, timerTimeoutMs);
    }

    /// <summary>
    /// Waits for and returns the next event: the pending one, a mouse event, a key event, or
    /// nothing (after calling <see cref="Idle"/>). The status line sees keys and its clicks first.
    /// </summary>
    public override void GetEvent(ref TEvent ev)
    {
        if (Pending.What != evNothing)
        {
            ev = Pending;
            Pending.What = evNothing;
        }
        else
        {
            TEventQueue.WaitForEvents(EventWaitTimeout());
            ev.GetMouseEvent();
            if (ev.What == evNothing)
            {
                ev.GetKeyEvent();
                if (ev.What == evNothing)
                    Idle();
            }
        }

        var mouseEvent = ev;
        if (StatusLine is not null &&
            ((ev.What & evKeyDown) != 0 ||
             ((ev.What & evMouseDown) != 0 && FirstThat(p => ViewHasMouse(p, mouseEvent)) == StatusLine)))
            StatusLine.HandleEvent(ref ev);

        if (ev.What == evCommand && ev.Message.Command == cmScreenChanged)
        {
            SetScreenMode(TDisplay.smUpdate);
            ClearEvent(ref ev);
        }
    }

    private static bool ViewHasMouse(TView p, in TEvent ev) =>
        (p.State & sfVisible) != 0 && p.MouseInView(ev.Mouse.Where);

    /// <summary>The application palette selected by <see cref="AppPalette"/>.</summary>
    public override TPalette GetPalette() => _palettes[AppPalette];

    /// <summary>Handles Alt+1..9 (select window by number) and <c>cmQuit</c>.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        if (ev.What == evKeyDown)
        {
            var c = GetAltChar(ev.KeyDown.KeyCode);
            if (c is >= '1' and <= '9')
            {
                if (CanMoveFocus())
                {
                    // UPSTREAM: upstream passes the number through infoPtr.
                    var e = new TEvent { What = evBroadcast };
                    e.Message.Command = cmSelectWindowNum;
                    e.Message.InfoLong = c - '0';
                    DeskTop!.HandleEvent(ref e);
                    if (e.What == evNothing)
                        ClearEvent(ref ev);
                }
                else
                    ClearEvent(ref ev);
            }
        }

        base.HandleEvent(ref ev);

        if (ev is { What: evCommand, Message.Command: cmQuit })
        {
            EndModal(cmQuit);
            ClearEvent(ref ev);
        }
    }

    /// <summary>
    /// Called when there are no events: updates the status line, broadcasts
    /// <c>cmCommandSetChanged</c> and <c>cmTimerExpired</c>.
    /// </summary>
    public virtual void Idle()
    {
        StatusLine?.Update();

        if (CommandSetChanged)
        {
            Message(this, evBroadcast, cmCommandSetChanged, null);
            CommandSetChanged = false;
        }

        TimerQueue.CollectExpiredTimers(id => Message(this, evBroadcast, cmTimerExpired, id));
    }

    /// <summary>Creates the default desktop: the whole screen except the first and last rows.</summary>
    public static TDeskTop? InitDeskTop(TRect r)
    {
        r.A.Y++;
        r.B.Y--;
        return new TDeskTop(r);
    }

    /// <summary>Creates the default, empty, menu bar on the first row.</summary>
    public static TMenuBar? InitMenuBar(TRect r)
    {
        r.B.Y = r.A.Y + 1;
        return new TMenuBar(r, (TMenu?)null);
    }

    /// <summary>Picks the shadow size, markers and palette for the screen mode.</summary>
    public virtual void InitScreen()
    {
        if ((TScreen.ScreenMode & 0x00FF) != TDisplay.smMono)
        {
            ShadowSize.X = (TScreen.ScreenMode & TDisplay.smFont8x8) != 0 ? 1 : 2;
            ShadowSize.Y = 1;
            ShowMarkers = false;
            AppPalette = (TScreen.ScreenMode & 0x00FF) == TDisplay.smBW80 ? apBlackWhite : apColor;
        }
        else
        {
            ShadowSize.X = 0;
            ShadowSize.Y = 0;
            ShowMarkers = true;
            AppPalette = apMonochrome;
        }
    }

    /// <summary>Creates the default status line on the last row, with Alt-X, F10, Alt-F3, F5 and Ctrl-F5.</summary>
    public static TStatusLine? InitStatusLine(TRect r)
    {
        r.A.Y = r.B.Y - 1;
        return new TStatusLine(r,
            new TStatusDef(0, 0xFFFF) +
                new TStatusItem(ExitText, kbAltX, cmQuit) +
                new TStatusItem(null, kbF10, cmMenu) +
                new TStatusItem(null, kbAltF3, cmClose) +
                new TStatusItem(null, kbF5, cmZoom) +
                new TStatusItem(null, kbCtrlF5, cmResize));
    }

    /// <summary>Stops the timer <paramref name="id"/>.</summary>
    public override void KillTimer(object? id) => TimerQueue.KillTimer(id);

    /// <summary>Called when a view could not be created for lack of memory. Does nothing by default.</summary>
    public virtual void OutOfMemory()
    {
    }

    /// <summary>Makes <paramref name="ev"/> the next event returned by <see cref="GetEvent"/>.</summary>
    public override void PutEvent(ref TEvent ev) => Pending = ev;

    /// <summary>Runs the application until <c>cmQuit</c>.</summary>
    public virtual void Run() => Execute();

    /// <summary>Changes the screen mode (or reloads the screen size with <c>smUpdate</c>) and redraws.</summary>
    public void SetScreenMode(ushort mode)
    {
        TMouse.Hide();
        TScreen.SetVideoMode(mode);
        InitScreen();
        Buffer = TScreen.ScreenBuffer;
        var r = new TRect(0, 0, TScreen.ScreenWidth, TScreen.ScreenHeight);
        ChangeBounds(r);
        SetState(sfExposed, false);
        SetState(sfExposed, true);
        Redraw();
        TMouse.Show();
    }

    /// <summary>Starts a timer (see <see cref="TView.SetTimer"/>).</summary>
    public override object? SetTimer(uint timeoutMs, int periodMs = -1) => TimerQueue.SetTimer(timeoutMs, periodMs);

    /// <summary>
    /// Runs <paramref name="pD"/> modally on the desktop and destroys it. If <paramref name="data"/>
    /// is given, it fills the dialog first and receives its data unless the dialog was cancelled.
    /// </summary>
    public ushort ExecuteDialog(TDialog? pD, object?[]? data = null)
    {
        var c = cmCancel;

        if (ValidView(pD) is not null)
        {
            if (data is not null)
                pD!.SetData(data);
            c = DeskTop!.ExecView(pD);
            if (c != cmCancel && data is not null)
                pD!.GetData(data);
            Destroy(pD);
        }

        return c;
    }

    /// <summary>
    /// Inserts <paramref name="pWin"/> into the desktop if it is valid and the focus can move;
    /// otherwise destroys it and returns null.
    /// </summary>
    public TWindow? InsertWindow(TWindow? pWin)
    {
        if (ValidView(pWin) is not null)
        {
            if (CanMoveFocus())
            {
                DeskTop!.Insert(pWin);
                return pWin;
            }
            Destroy(pWin);
        }
        return null;
    }

    /// <summary>Returns <paramref name="p"/> if it is valid, otherwise destroys it and returns null.</summary>
    public TView? ValidView(TView? p)
    {
        if (p is null)
            return null;
        if (!p.Valid(cmValid))
        {
            Destroy(p);
            return null;
        }
        return p;
    }

    /// <summary>Gives the console back to the system. Does nothing by default.</summary>
    public virtual void Suspend()
    {
    }

    /// <summary>Takes the console back. Does nothing by default.</summary>
    public virtual void Resume()
    {
    }
}
