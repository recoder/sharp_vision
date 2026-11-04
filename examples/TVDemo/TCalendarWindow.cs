using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

/// <summary>The window holding a <see cref="TCalendarView"/>.</summary>
/// Ported from examples/tvdemo/calendar.h, calendar.cpp (class TCalendarWindow).
internal sealed class TCalendarWindow : TWindow
{
    public TCalendarWindow() : base(new TRect(1, 1, 23, 11), "Calendar", wnNoNumber)
    {
        var r = GetExtent();

        Flags &= unchecked((byte)~(wfZoom | wfGrow));
        GrowMode = 0;

        Palette = (short)wpCyanWindow;

        r.Grow(-1, -1);
        Insert(new TCalendarView(r));
    }
}
