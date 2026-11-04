using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.ViewFlags;

/// <summary>A window with the code page table and the current character's codes.</summary>
/// Ported from examples/tvdemo/ascii.h, ascii.cpp (class TAsciiChart).
internal sealed class TAsciiChart : TWindow
{
    public TAsciiChart() : base(new TRect(0, 0, 34, 12), "ASCII Chart", wnNoNumber)
    {
        Flags &= unchecked((byte)~(wfGrow | wfZoom));
        GrowMode = 0;
        Palette = (short)wpGrayWindow;

        var r = GetExtent();
        r.Grow(-1, -1);
        r.A.Y = r.B.Y - 1;
        TView control = new TReport(r);
        control.Options |= ofFramed;
        control.EventMask |= evBroadcast;
        Insert(control);

        r = GetExtent();
        r.Grow(-1, -1);
        r.B.Y -= 2;
        control = new TTable(r);
        control.Options |= ofFramed;
        control.Options |= ofSelectable;
        control.BlockCursor();
        Insert(control);
        control.Select();
    }

    public override void HandleEvent(ref TEvent ev) => base.HandleEvent(ref ev);
}
