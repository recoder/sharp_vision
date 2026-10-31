using System;

using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.HistList;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Views.Commands;

namespace SharpVision.Dialogs;

/// <summary>The list inside a <see cref="THistoryWindow"/>: the strings recorded for one history id.</summary>
/// Ported from source/tvision/thstview.cpp, include/tvision/dialogs.h (class THistoryViewer).
/// <remarks>Palette layout: 1 = active, 2 = inactive, 3 = focused, 4 = selected, 5 = divider.</remarks>
public class THistoryViewer : TListViewer
{
    private static readonly TPalette _palette = new([0x06, 0x06, 0x07, 0x06, 0x06]);

    /// <summary>The history id whose strings are listed.</summary>
    protected ushort HistoryId;

    /// <summary>Creates the list, focusing the second-oldest string, with the scroll bars' ranges set.</summary>
    public THistoryViewer(TRect bounds, TScrollBar aHScrollBar, TScrollBar aVScrollBar, ushort aHistoryId)
        : base(bounds, 1, aHScrollBar, aVScrollBar)
    {
        HistoryId = aHistoryId;
        SetRange(HistoryCount((byte)aHistoryId));
        if (Range > 1)
            FocusItem(1);
        HScrollBar!.SetRange(0, HistoryWidth() - Size.X + 3);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>The <paramref name="item"/>th string, at most <paramref name="maxLen"/> long.</summary>
    public override string GetText(int item, int maxLen)
    {
        var s = HistoryStr((byte)HistoryId, item) ?? "";
        return s.Length > maxLen ? s[..maxLen] : s;
    }

    /// <summary>Ends the modal window with <c>cmOK</c> on double-click or Enter, <c>cmCancel</c> on Esc.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        if ((ev.What == evMouseDown && (ev.Mouse.EventFlags & meDoubleClick) != 0) ||
            (ev.What == evKeyDown && ev.KeyDown.KeyCode == kbEnter))
        {
            EndModal(cmOK);
            ClearEvent(ref ev);
        }
        else if ((ev.What == evKeyDown && ev.KeyDown.KeyCode == kbEsc) ||
                 (ev.What == evCommand && ev.Message.Command == cmCancel))
        {
            EndModal(cmCancel);
            ClearEvent(ref ev);
        }
        else
            base.HandleEvent(ref ev);
    }

    /// <summary>The width in columns of the widest string.</summary>
    public int HistoryWidth()
    {
        var width = 0;
        int count = HistoryCount((byte)HistoryId);
        for (var i = 0; i < count; i++)
            width = Math.Max(width, Strwidth(HistoryStr((byte)HistoryId, i)));
        return width;
    }
}
