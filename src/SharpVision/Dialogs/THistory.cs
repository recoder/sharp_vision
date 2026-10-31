using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Dialogs.HistList;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Dialogs;

/// <summary>
/// The arrow next to an input line: records the line's text in the history and, when clicked (or on
/// Down in the line), drops down a <see cref="THistoryWindow"/> to pick an earlier entry.
/// </summary>
/// Ported from source/tvision/thistory.cpp, source/tvision/tvtext1.cpp (icon),
/// include/tvision/dialogs.h (class THistory).
/// <remarks>Palette layout: 1 = arrow, 2 = sides.</remarks>
public class THistory : TView
{
    private const string Icon = "▐~↓~▌";
    private static readonly TPalette _palette = new([0x16, 0x17]);

    /// <summary>The input line whose history this is, or null after shutdown.</summary>
    protected TInputLine? Link;

    /// <summary>The history id.</summary>
    protected ushort HistoryId;

    /// <summary>Creates the arrow for <paramref name="aLink"/>, recording under <paramref name="aHistoryId"/>.</summary>
    public THistory(TRect bounds, TInputLine aLink, ushort aHistoryId)
        : base(bounds)
    {
        Link = aLink;
        HistoryId = aHistoryId;
        Options |= ofPostProcess;
        EventMask |= evBroadcast;
    }

    /// <inheritdoc />
    public override void ShutDown()
    {
        Link = null;
        base.ShutDown();
    }

    /// <inheritdoc />
    public override void Draw()
    {
        var b = new TDrawBuffer();
        b.MoveCStr(0, Icon, GetColor(0x0102));
        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>Opens the history on a click or Down; records the text when the line loses focus or on <c>cmRecordHistory</c>.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evMouseDown ||
            (ev.What == evKeyDown &&
             CtrlToArrow(ev.KeyDown.KeyCode) == kbDown &&
             (Link!.State & sfFocused) != 0))
        {
            if (!Link!.Focus())
            {
                ClearEvent(ref ev);
                return;
            }
            RecordHistory(Link.Data);
            var r = Link.GetBounds();
            r.A.X--;
            r.B.X++;
            r.B.Y += 7;
            r.A.Y--;
            var p = Owner!.GetExtent();
            r.Intersect(p);
            r.B.Y--;
            var historyWindow = InitHistoryWindow(r);
            if (historyWindow is not null)
            {
                var c = Owner.ExecView(historyWindow);
                if (c == cmOK)
                {
                    var rslt = historyWindow.GetSelection();
                    Link.Data = rslt.Length > Link.MaxLen ? rslt[..Link.MaxLen] : rslt;
                    Link.SelectAll(true);
                    Link.DrawView();
                }
                Destroy(historyWindow);
            }
            ClearEvent(ref ev);
        }
        else if (ev.What == evBroadcast)
        {
            if ((ev.Message.Command == cmReleasedFocus && ev.Message.InfoPtr == Link) ||
                ev.Message.Command == cmRecordHistory)
                RecordHistory(Link!.Data);
        }
    }

    /// <summary>Creates the drop-down window, with the line's help context.</summary>
    public virtual THistoryWindow? InitHistoryWindow(TRect bounds) =>
        new(bounds, HistoryId) { HelpCtx = Link!.HelpCtx };

    /// <summary>Adds <paramref name="s"/> to the history.</summary>
    public virtual void RecordHistory(string s) => HistoryAdd((byte)HistoryId, s);
}
