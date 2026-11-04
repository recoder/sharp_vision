using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

/// <summary>The window holding a <see cref="TPuzzleView"/>.</summary>
/// Ported from examples/tvdemo/puzzle.h, puzzle.cpp (class TPuzzleWindow).
internal sealed class TPuzzleWindow : TWindow
{
    public TPuzzleWindow() : base(new TRect(1, 1, 21, 7), "Puzzle", wnNoNumber)
    {
        Flags &= unchecked((byte)~(wfZoom | wfGrow));
        GrowMode = 0;

        var r = GetExtent();
        r.Grow(-1, -1);
        Insert(new TPuzzleView(r));
    }
}
