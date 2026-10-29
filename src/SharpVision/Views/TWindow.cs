// Ported from source/tvision/twindow.cpp, include/tvision/views.h (TWindowInit, class TWindow).

using System;

using SharpVision.Events;
using SharpVision.Objects;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Views;

/// <summary>A movable, resizable, closable group with a <see cref="TFrame"/>.</summary>
/// <remarks>
/// Palette layout: 1 = frame passive, 2 = frame active, 3 = frame icon, 4 = scroll bar page
/// area, 5 = scroll bar controls, 6 = scroller normal text, 7 = scroller selected text,
/// 8 = reserved.
/// </remarks>
public class TWindow : TGroup
{
    private static readonly TPoint _minWinSize = new(16, 6);

    private static readonly TPalette[] _palettes =
    [
        new([0x08, 0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F]), // cpBlueWindow
        new([0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17]), // cpCyanWindow
        new([0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F]), // cpGrayWindow
    ];

    /// <summary>Which of the move, grow, close and zoom actions are allowed (<c>wfXxx</c>).</summary>
    public byte Flags;

    /// <summary>The bounds to restore when the window is unzoomed.</summary>
    public TRect ZoomRect;

    /// <summary>The window number shown in the frame (1-9 can be selected with Alt+number), or <c>wnNoNumber</c>.</summary>
    public short Number;

    /// <summary>Which window palette to use (<c>wpXxx</c>).</summary>
    public short Palette;

    /// <summary>The frame, or null.</summary>
    public TFrame? Frame;

    /// <summary>The title, or null.</summary>
    public string? Title;

    /// <summary>Creates a window with the default frame.</summary>
    public TWindow(TRect bounds, string? aTitle, int aNumber)
        : this(bounds, aTitle, aNumber, InitFrame)
    {
    }

    /// <summary>
    /// Creates a window whose frame is made by <paramref name="createFrame"/>
    /// (upstream's <c>TWindowInit</c>); null for no frame.
    /// </summary>
    public TWindow(TRect bounds, string? aTitle, int aNumber, Func<TRect, TFrame?>? createFrame)
        : base(bounds)
    {
        Flags = (byte)(wfMove | wfGrow | wfClose | wfZoom);
        ZoomRect = GetBounds();
        Number = (short)aNumber;
        Palette = (short)wpBlueWindow;
        Title = aTitle;

        State |= sfShadow;
        Options |= ofSelectable | ofTopSelect;
        GrowMode = (byte)(gfGrowAll | gfGrowRel);

        if (createFrame is not null && (Frame = createFrame(GetExtent())) is not null)
            Insert(Frame);
    }

    /// <summary>Closes and destroys the window if it is valid for <c>cmClose</c>.</summary>
    public virtual void Close()
    {
        if (Valid(cmClose))
        {
            Frame = null; // so we don't try to use the frame after it's been deleted
            Destroy(this);
        }
    }

    /// <inheritdoc />
    public override void ShutDown()
    {
        Frame = null;
        base.ShutDown();
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palettes[Palette];

    /// <summary>Returns the title to show in a frame with room for <paramref name="maxSize"/> columns.</summary>
    public virtual string? GetTitle(int maxSize) => Title;

    /// <summary>Handles the window commands (resize, close, zoom), Tab focus moves and window selection by number.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evCommand)
        {
            switch (ev.Message.Command)
            {
                case cmResize:
                    if ((Flags & (wfMove | wfGrow)) != 0)
                    {
                        var limits = Owner!.GetExtent();
                        SizeLimits(out var min, out var max);
                        DragView(ref ev, (byte)(DragMode | (Flags & (wfMove | wfGrow))), limits, min, max);
                        ClearEvent(ref ev);
                    }
                    break;
                case cmClose:
                    if ((Flags & wfClose) != 0 &&
                        (ev.Message.InfoPtr is null || ev.Message.InfoPtr == this))
                    {
                        ClearEvent(ref ev);
                        if ((State & sfModal) == 0)
                            Close();
                        else
                        {
                            ev.What = evCommand;
                            ev.Message.Command = cmCancel;
                            PutEvent(ref ev);
                            ClearEvent(ref ev);
                        }
                    }
                    break;
                case cmZoom:
                    if ((Flags & wfZoom) != 0 &&
                        (ev.Message.InfoPtr is null || ev.Message.InfoPtr == this))
                    {
                        Zoom();
                        ClearEvent(ref ev);
                    }
                    break;
            }
        }
        else if (ev.What == evKeyDown)
        {
            switch (ev.KeyDown.KeyCode)
            {
                case kbTab:
                    FocusNext(false);
                    ClearEvent(ref ev);
                    break;
                case kbShiftTab:
                    FocusNext(true);
                    ClearEvent(ref ev);
                    break;
            }
        }
        else if (ev.What == evBroadcast &&
                 ev.Message.Command == cmSelectWindowNum &&
                 ev.Message.InfoInt == Number &&
                 (Options & ofSelectable) != 0)
        {
            Select();
            ClearEvent(ref ev);
        }
    }

    /// <summary>Creates the default frame.</summary>
    public static TFrame? InitFrame(TRect r) => new TFrame(r);

    /// <summary>Activates the window and its frame and enables the window commands when it is selected.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        var windowCommands = new TCommandSet();

        base.SetState(aState, enable);
        if ((aState & sfSelected) != 0)
        {
            SetState(sfActive, enable);
            Frame?.SetState(sfActive, enable);
            windowCommands += cmNext;
            windowCommands += cmPrev;
            if ((Flags & (wfGrow | wfMove)) != 0)
                windowCommands += cmResize;
            if ((Flags & wfClose) != 0)
                windowCommands += cmClose;
            if ((Flags & wfZoom) != 0)
                windowCommands += cmZoom;
            if (enable)
                EnableCommands(windowCommands);
            else
                DisableCommands(windowCommands);
        }
    }

    // TBD: StandardScrollBar(ushort aOptions) once TScrollBar is ported.

    /// <summary>Limits the window to at least 16x6.</summary>
    public override void SizeLimits(out TPoint min, out TPoint max)
    {
        base.SizeLimits(out min, out max);
        min = _minWinSize;
    }

    /// <summary>Toggles between the maximum size and <see cref="ZoomRect"/>.</summary>
    public virtual void Zoom()
    {
        SizeLimits(out _, out var maxSize);
        if (Size != maxSize)
        {
            ZoomRect = GetBounds();
            var r = new TRect(0, 0, maxSize.X, maxSize.Y);
            Locate(ref r);
        }
        else
            Locate(ref ZoomRect);
    }
}
