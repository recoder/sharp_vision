using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;

using static SharpVision.Events.EventCodes;
using static SharpVision.StdDlg.Dir;
using static SharpVision.StdDlg.StdDlgCommands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.StdDlg;

/// <summary>
/// The file name line of a <see cref="TFileDialog"/>. While not selected, it shows the entry focused in the
/// file list: the file name, or the directory followed by <c>\</c> and the dialog's wildcard.
/// </summary>
/// Ported from source/tvision/stddlg.cpp, include/tvision/stddlg.h (class TFileInputLine).
public class TFileInputLine : TInputLine
{
    /// <summary>Creates the line, which also listens to broadcasts.</summary>
    public TFileInputLine(TRect bounds, int aMaxLen) : base(bounds, aMaxLen)
    {
        EventMask |= evBroadcast;
    }

    /// <summary>Edits the text, and follows <c>cmFileFocused</c> while not selected.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast && ev.Message.Command == cmFileFocused && (State & sfSelected) == 0)
        {
            var rec = (TSearchRec)ev.Message.InfoPtr!;
            Data = rec.Name;
            if ((rec.Attr & faDirec) != 0)
                Data += "\\" + ((TFileDialog)Owner!).WildCard;
            SelectAll(false);
            DrawView();
        }
    }
}
