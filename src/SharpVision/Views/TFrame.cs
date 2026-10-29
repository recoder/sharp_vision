// Ported from source/tvision/tframe.cpp, source/tvision/framelin.cpp,
// source/tvision/tvtext1.cpp (frame characters and icons), include/tvision/views.h (class TFrame).

using System;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Views;

/// <summary>The border of a <see cref="TWindow"/>, with its title, number and icons.</summary>
/// <remarks>
/// Palette layout: 1 = passive frame, 2 = passive title, 3 = active frame, 4 = active title,
/// 5 = icons. The owner must be a <see cref="TWindow"/>.
/// </remarks>
public class TFrame : TView
{
    private static readonly TPalette _palette = new([0x01, 0x01, 0x02, 0x02, 0x03]);

    // Masks for the left, middle and right cells of: top line (0), single-line middle (3),
    // single-line bottom (6), and the same three for the active double line (9, 12, 15).
    private static readonly byte[] _initFrame =
    [
        0x06, 0x0A, 0x0C, 0x05, 0x00, 0x05, 0x03, 0x0A, 0x09,
        0x16, 0x1A, 0x1C, 0x15, 0x00, 0x15, 0x13, 0x1A, 0x19,
    ];

    // Indexed by a mask of line directions (1 = up, 2 = right, 4 = down, 8 = left, 0x10 = double).
    // UPSTREAM: TDisplay::updateIntlChars changes index 30 for non-437 code pages; magiblot's
    // getCodePage always returns 437, so it never does.
    private const string FrameChars = "   └ │┌├ ┘─┴┐┤┬┼   ╚ ║╔╟ ╝═╧╗╢╤ ";

    private const string CloseIcon = "[~■~]";
    private const string ZoomIcon = "[~↑~]";
    private const string UnZoomIcon = "[~↕~]";
    private const string DragIcon = "~─┘~";
    private const string DragLeftIcon = "~└─~";

    /// <summary>Creates a frame that grows with its window.</summary>
    public TFrame(TRect bounds)
        : base(bounds)
    {
        GrowMode = (byte)(gfGrowHiX | gfGrowHiY);
        EventMask |= evBroadcast | evMouseUp;
    }

    private TWindow Window => (TWindow)Owner!;

    /// <summary>Draws the border, title, window number and, when active, the icons.</summary>
    public override void Draw()
    {
        TAttrPair cFrame, cTitle;
        int f, i, l, width;
        var b = new TDrawBuffer();

        if ((State & sfDragging) != 0)
        {
            cFrame = 0x0505;
            cTitle = 0x0005;
            f = 0;
        }
        else if ((State & sfActive) == 0)
        {
            cFrame = 0x0101;
            cTitle = 0x0002;
            f = 0;
        }
        else
        {
            cFrame = 0x0503;
            cTitle = 0x0004;
            f = 9;
        }

        cFrame = GetColor((ushort)cFrame);
        cTitle = GetColor((ushort)cTitle);

        width = Size.X;
        l = width - 10;

        if ((Window.Flags & (wfClose | wfZoom)) != 0)
            l -= 6;
        FrameLine(b, 0, f, cFrame);
        if (Window.Number != wnNoNumber && Window.Number < 10)
        {
            l -= 4;
            if ((Window.Flags & wfZoom) != 0)
                i = 7;
            else
                i = 3;
            b.PutChar(width - i, (char)(Window.Number + '0'));
        }

        if (Owner is not null)
        {
            var title = Window.GetTitle(l);
            if (title is not null)
            {
                l = Math.Min(TText.Width(title), width - 10);
                l = Math.Max(l, 0);
                i = (width - l) >> 1;
                b.PutChar(i - 1, ' ');
                b.MoveStr(i, title, cTitle, l);
                b.PutChar(i + l, ' ');
            }
        }

        if ((State & sfActive) != 0)
        {
            if ((Window.Flags & wfClose) != 0)
                b.MoveCStr(2, CloseIcon, cFrame);
            if ((Window.Flags & wfZoom) != 0)
            {
                Owner!.SizeLimits(out _, out var maxSize);
                if (Owner.Size == maxSize)
                    b.MoveCStr(width - 5, UnZoomIcon, cFrame);
                else
                    b.MoveCStr(width - 5, ZoomIcon, cFrame);
            }
        }

        WriteLine(0, 0, Size.X, 1, b);
        for (i = 1; i <= Size.Y - 2; i++)
        {
            FrameLine(b, i, f + 3, cFrame);
            WriteLine(0, i, Size.X, 1, b);
        }
        FrameLine(b, Size.Y - 1, f + 6, cFrame);
        if ((State & sfActive) != 0)
        {
            if ((Window.Flags & wfGrow) != 0)
            {
                b.MoveCStr(0, DragLeftIcon, cFrame);
                b.MoveCStr(width - 2, DragIcon, cFrame);
            }
        }
        WriteLine(0, Size.Y - 1, Size.X, 1, b);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    // Fills row y of the frame, joining the lines of framed (ofFramed) siblings below this frame.
    private void FrameLine(TDrawBuffer frameBuf, int y, int n, TColorAttr color)
    {
        if (Size.X <= 0)
            return;

        Span<byte> frameMask = stackalloc byte[Size.X];
        int x;

        frameMask[0] = _initFrame[n];
        for (x = 1; x < Size.X - 1; ++x)
            frameMask[x] = _initFrame[n + 1];
        frameMask[Size.X - 1] = _initFrame[n + 2];

        var v = Owner!.Last!.Next!;
        for (; v != this; v = v.Next!)
        {
            if ((v.Options & ofFramed) != 0 && (v.State & sfVisible) != 0)
            {
                ushort mask = 0;
                if (y < v.Origin.Y)
                {
                    if (y == v.Origin.Y - 1)
                        mask = 0x0A06;
                }
                else if (y < v.Origin.Y + v.Size.Y)
                    mask = 0x0005;
                else if (y == v.Origin.Y + v.Size.Y)
                    mask = 0x0A03;

                if (mask != 0)
                {
                    var start = Math.Max(v.Origin.X, 1);
                    var end = Math.Min(v.Origin.X + v.Size.X, Size.X - 1);
                    if (start < end)
                    {
                        var maskLow = (byte)(mask & 0x00FF);
                        var maskHigh = (byte)((mask & 0xFF00) >> 8);
                        frameMask[start - 1] |= maskLow;
                        frameMask[end] |= (byte)(maskLow ^ maskHigh);
                        if (maskLow != 0)
                        {
                            for (x = start; x < end; ++x)
                                frameMask[x] |= maskHigh;
                        }
                    }
                }
            }
        }

        for (x = 0; x < Size.X; ++x)
        {
            frameBuf.PutChar(x, FrameChars[frameMask[x]]);
            frameBuf.PutAttribute(x, color);
        }
    }

    private void DragWindow(ref TEvent ev, ushort mode)
    {
        var limits = Owner!.Owner!.GetExtent();
        Owner.SizeLimits(out var min, out var max);
        Owner.DragView(ref ev, (byte)(Owner.DragMode | mode), limits, min, max);
        ClearEvent(ref ev);
    }

    /// <summary>Handles clicks on the close and zoom icons, and dragging to move or resize the window.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evMouseDown)
        {
            var mouse = MakeLocal(ev.Mouse.Where);
            if (mouse.Y == 0)
            {
                if ((Window.Flags & wfClose) != 0 &&
                    (State & sfActive) != 0 &&
                    mouse.X >= 2 &&
                    mouse.X <= 4)
                {
                    while (MouseEvent(ref ev, evMouse))
                    {
                    }
                    mouse = MakeLocal(ev.Mouse.Where);
                    if (mouse.Y == 0 && mouse.X >= 2 && mouse.X <= 4)
                    {
                        ev.What = evCommand;
                        ev.Message.Command = cmClose;
                        ev.Message.InfoPtr = Owner;
                        PutEvent(ref ev);
                        ClearEvent(ref ev);
                    }
                }
                else if ((Window.Flags & wfZoom) != 0 &&
                         (State & sfActive) != 0 &&
                         ((mouse.X >= Size.X - 5 && mouse.X <= Size.X - 3) ||
                          (ev.Mouse.EventFlags & meDoubleClick) != 0))
                {
                    ev.What = evCommand;
                    ev.Message.Command = cmZoom;
                    ev.Message.InfoPtr = Owner;
                    PutEvent(ref ev);
                    ClearEvent(ref ev);
                }
                else if ((Window.Flags & wfMove) != 0)
                    DragWindow(ref ev, dmDragMove);
            }
            else if ((State & sfActive) != 0 && mouse.Y >= Size.Y - 1 &&
                     (Window.Flags & wfGrow) != 0)
            {
                if (mouse.X >= Size.X - 2)
                    DragWindow(ref ev, dmDragGrow);
                else if (mouse.X <= 1)
                    DragWindow(ref ev, dmDragGrowLeft);
            }
            else if (ev.What == evMouseDown && ev.Mouse.Buttons == mbMiddleButton &&
                     0 < mouse.X && mouse.X < Size.X - 1 && 0 < mouse.Y && mouse.Y < Size.Y - 1 &&
                     (Window.Flags & wfMove) != 0)
                DragWindow(ref ev, dmDragMove);
        }
    }

    /// <summary>Redraws the frame when it becomes active or inactive, or starts or stops dragging.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if ((aState & (sfActive | sfDragging)) != 0)
            DrawView();
    }
}
