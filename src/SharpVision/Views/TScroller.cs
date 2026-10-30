// Ported from source/tvision/tscrolle.cpp, include/tvision/views.h (class TScroller).

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Views;

/// <summary>
/// A view whose contents are larger than its bounds: <see cref="Delta"/> is the scroll offset,
/// kept in step with optional scroll bars. Subclasses draw the contents shifted by it.
/// </summary>
/// <remarks>Palette layout: 1 = normal text, 2 = selected text.</remarks>
public class TScroller : TView
{
    private static readonly TPalette _palette = new([0x06, 0x07]);

    /// <summary>The scroll offset: the content coordinates shown at the view's top left.</summary>
    public TPoint Delta;

    /// <summary>While non-zero, <see cref="ScrollDraw"/> defers the redraw to <see cref="CheckDraw"/>.</summary>
    protected byte DrawLock;

    /// <summary>Set when a redraw was deferred by <see cref="DrawLock"/>.</summary>
    protected bool DrawFlag;

    /// <summary>The horizontal scroll bar, or null.</summary>
    protected TScrollBar? HScrollBar;

    /// <summary>The vertical scroll bar, or null.</summary>
    protected TScrollBar? VScrollBar;

    /// <summary>The size of the contents.</summary>
    protected TPoint Limit;

    /// <summary>Creates a scroller with optional scroll bars.</summary>
    public TScroller(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar)
        : base(bounds)
    {
        HScrollBar = aHScrollBar;
        VScrollBar = aVScrollBar;
        Options |= ofSelectable;
        EventMask |= evBroadcast;
    }

    /// <inheritdoc />
    public override void ShutDown()
    {
        HScrollBar = null;
        VScrollBar = null;
        base.ShutDown();
    }

    /// <summary>Changes the bounds, updates the scroll bars and redraws once.</summary>
    public override void ChangeBounds(TRect bounds)
    {
        SetBounds(bounds);
        DrawLock++;
        SetLimit(Limit.X, Limit.Y);
        DrawLock--;
        DrawFlag = false;
        DrawView();
    }

    /// <summary>Redraws if a redraw was deferred and nothing holds the lock.</summary>
    public void CheckDraw()
    {
        if (DrawLock == 0 && DrawFlag)
        {
            DrawFlag = false;
            DrawView();
        }
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>Follows changes of its scroll bars.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast &&
            ev.Message.Command == cmScrollBarChanged &&
            (ev.Message.InfoPtr == HScrollBar || ev.Message.InfoPtr == VScrollBar))
            ScrollDraw();
    }

    /// <summary>Takes <see cref="Delta"/> from the scroll bars, moving the cursor with the contents, and redraws.</summary>
    public virtual void ScrollDraw()
    {
        var d = new TPoint(HScrollBar?.Value ?? 0, VScrollBar?.Value ?? 0);
        if (d.X != Delta.X || d.Y != Delta.Y)
        {
            SetCursor(Cursor.X + Delta.X - d.X, Cursor.Y + Delta.Y - d.Y);
            Delta = d;
            if (DrawLock != 0)
                DrawFlag = true;
            else
                DrawView();
        }
    }

    /// <summary>Scrolls to (<paramref name="x"/>, <paramref name="y"/>) through the scroll bars, redrawing once.</summary>
    public void ScrollTo(int x, int y)
    {
        DrawLock++;
        HScrollBar?.SetValue(x);
        VScrollBar?.SetValue(y);
        DrawLock--;
        CheckDraw();
    }

    /// <summary>Sets the size of the contents and the scroll bars' ranges and page steps.</summary>
    public void SetLimit(int x, int y)
    {
        Limit.X = x;
        Limit.Y = y;
        DrawLock++;
        HScrollBar?.SetParams(HScrollBar.Value, 0, x - Size.X, Size.X - 1, HScrollBar.ArStep);
        VScrollBar?.SetParams(VScrollBar.Value, 0, y - Size.Y, Size.Y - 1, VScrollBar.ArStep);
        DrawLock--;
        CheckDraw();
    }

    private void ShowSBar(TScrollBar? sBar)
    {
        if (sBar is not null)
        {
            if (GetState((ushort)(sfActive | sfSelected)))
                sBar.Show();
            else
                sBar.Hide();
        }
    }

    /// <summary>Shows the scroll bars while the scroller is active or selected.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if ((aState & (sfActive | sfSelected)) != 0)
        {
            ShowSBar(HScrollBar);
            ShowSBar(VScrollBar);
        }
    }
}
