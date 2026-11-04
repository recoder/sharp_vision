using SharpVision.App;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

/// <summary>A window showing a <see cref="TFileViewer"/>.</summary>
/// Ported from examples/tvdemo/fileview.h, fileview.cpp (class TFileWindow).
internal sealed class TFileWindow : TWindow
{
    // UPSTREAM: post-incremented from 0, so the first window gets wnNoNumber.
    private static short _winNumber;

    public TFileWindow(string fileName) : base(TProgram.DeskTop!.GetExtent(), fileName, _winNumber++)
    {
        Options |= ofTileable;
        var r = GetExtent();
        r.Grow(-1, -1);
        Insert(new TFileViewer(r,
            StandardScrollBar(sbHorizontal | sbHandleKeyboard),
            StandardScrollBar(sbVertical | sbHandleKeyboard),
            fileName));
    }
}
