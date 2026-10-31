using System;

using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Dialogs;

/// <summary>The drop-down window a <see cref="THistory"/> opens: a closable frame around a <see cref="THistoryViewer"/>.</summary>
/// Ported from source/tvision/thistwin.cpp, include/tvision/dialogs.h (classes THistoryWindow, THistInit).
/// <remarks>Palette layout: 1 = frame passive, 2 = frame active, 3 = frame icon, 4 = scroll bar page,
/// 5 = scroll bar controls, 6 = viewer normal, 7 = viewer selected.</remarks>
public class THistoryWindow : TWindow
{
    private static readonly TPalette _palette = new([0x13, 0x13, 0x15, 0x18, 0x17, 0x13, 0x14]);

    /// <summary>The list of strings, or null.</summary>
    protected TListViewer? Viewer;

    /// <summary>Creates the window with the default viewer.</summary>
    public THistoryWindow(TRect bounds, ushort historyId) : this(bounds, historyId, InitViewer)
    {
    }

    /// <summary>
    /// Creates the window, its list made by <paramref name="createListViewer"/> (upstream's
    /// <c>THistInit</c>) from the window's extent; null for none.
    /// </summary>
    public THistoryWindow(TRect bounds, ushort historyId, Func<TRect, TWindow, ushort, TListViewer?>? createListViewer)
        : base(bounds, null, wnNoNumber, InitFrame)
    {
        Flags = (byte)wfClose;
        if (createListViewer is not null && (Viewer = createListViewer(GetExtent(), this, historyId)) is not null)
            Insert(Viewer);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>The focused string.</summary>
    public virtual string GetSelection() => Viewer!.GetText(Viewer.Focused, 255);

    /// <summary>Closes with <c>cmCancel</c> when clicked outside.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evMouseDown && !MouseInView(ev.Mouse.Where))
        {
            EndModal(cmCancel);
            ClearEvent(ref ev);
        }
    }

    /// <summary>Creates the default viewer inside the frame, with both scroll bars.</summary>
    public static TListViewer? InitViewer(TRect r, TWindow win, ushort historyId)
    {
        r.Grow(-1, -1);
        return new THistoryViewer(r,
            win.StandardScrollBar(sbHorizontal | sbHandleKeyboard),
            win.StandardScrollBar(sbVertical | sbHandleKeyboard),
            historyId);
    }
}
