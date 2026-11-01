using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Editors.EditorCommands;
using static SharpVision.Events.EventCodes;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Editors;

/// <summary>
/// A tileable window holding a <see cref="TFileEditor"/> with scroll bars and an indicator on its frame,
/// titled with the file name.
/// </summary>
/// Ported from source/tvision/teditwnd.cpp, source/tvision/tvtext2.cpp (titles),
/// include/tvision/editors.h (class TEditWindow).
public class TEditWindow : TWindow
{
    /// <summary>The title of the clipboard window.</summary>
    public static string ClipboardTitle = "Clipboard";
    /// <summary>The title of an untitled editor's window.</summary>
    public static string Untitled = "Untitled";

    private static readonly TPoint _minEditWinSize = new(24, 6);

    /// <summary>The editor.</summary>
    public TFileEditor Editor;

    /// <summary>Creates the window numbered <paramref name="aNumber"/> editing <paramref name="fileName"/> (empty for untitled).</summary>
    public TEditWindow(TRect bounds, string fileName, int aNumber)
        : base(bounds, null, aNumber)
    {
        Options |= ofTileable;

        var hScrollBar = new TScrollBar(new TRect(18, Size.Y - 1, Size.X - 2, Size.Y));
        hScrollBar.Hide();
        Insert(hScrollBar);

        var vScrollBar = new TScrollBar(new TRect(Size.X - 1, 1, Size.X, Size.Y - 1));
        vScrollBar.Hide();
        Insert(vScrollBar);

        var indicator = new TIndicator(new TRect(2, Size.Y - 1, 16, Size.Y));
        indicator.Hide();
        Insert(indicator);

        var r = GetExtent();
        r.Grow(-1, -1);
        Editor = new TFileEditor(r, hScrollBar, vScrollBar, indicator, fileName);
        Insert(Editor);
    }

    /// <summary>Hides the clipboard window instead of closing it.</summary>
    public override void Close()
    {
        if (Editor.IsClipboard())
            Hide();
        else
            base.Close();
    }

    /// <summary>The clipboard title, <see cref="Untitled"/>, or the file name.</summary>
    public override string? GetTitle(int maxSize) =>
        Editor.IsClipboard() ? ClipboardTitle : Editor.FileName.Length == 0 ? Untitled : Editor.FileName;

    /// <summary>Redraws the frame on <c>cmUpdateTitle</c>.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast && ev.Message.Command == cmUpdateTitle)
        {
            Frame?.DrawView();
            ClearEvent(ref ev);
        }
    }

    /// <summary>At least 24x6.</summary>
    public override void SizeLimits(out TPoint min, out TPoint max)
    {
        base.SizeLimits(out min, out max);
        min = _minEditWinSize;
    }
}
