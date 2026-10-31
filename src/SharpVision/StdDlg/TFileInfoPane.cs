using System.Globalization;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.StdDlg.Dir;
using static SharpVision.StdDlg.StdDlgCommands;

namespace SharpVision.StdDlg;

/// <summary>
/// The pane at the bottom of a <see cref="TFileDialog"/>: the expanded directory and wildcard, then the
/// name, size and date of the entry focused in the file list.
/// </summary>
/// Ported from source/tvision/stddlg.cpp, source/tvision/tvtext2.cpp (texts),
/// include/tvision/stddlg.h (class TFileInfoPane).
/// <remarks>Palette layout: 1 = normal.</remarks>
public class TFileInfoPane : TView
{
    private static readonly TPalette _palette = new([0x1E]);

    /// <summary>The PM suffix.</summary>
    public static string PmText = "p";

    /// <summary>The AM suffix.</summary>
    public static string AmText = "a";

    /// <summary>The month names, from index 1.</summary>
    public static string[] Months = ["", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    /// <summary>A copy of the focused entry.</summary>
    public TSearchRec FileBlock = new();

    /// <summary>Creates the pane, which listens to broadcasts.</summary>
    public TFileInfoPane(TRect bounds) : base(bounds)
    {
        EventMask |= evBroadcast;
    }

    private static string Num(int n) => n.ToString(CultureInfo.InvariantCulture);

    private static string Two(int n) => n >= 10 ? Num(n) : "0" + Num(n);

    /// <summary>Draws the path line, the entry line, and blank lines below.</summary>
    public override void Draw()
    {
        var b = new TDrawBuffer();
        var owner = (TFileDialog)Owner!;
        var path = FExpand(owner.Directory + owner.WildCard);
        var color = GetColor(0x01);
        b.MoveChar(0, ' ', color, Size.X);
        b.MoveStr(1, path, color);
        WriteLine(0, 0, Size.X, 1, b);
        b.MoveChar(0, ' ', color, Size.X);
        b.MoveStr(1, FileBlock.Name, color);
        if (FileBlock.Name.Length != 0)
        {
            b.MoveStr(Size.X - 38, Num(FileBlock.Size), color);
            // The DOS time: seconds/2, minute, hour in the low word; day, month, year-1980 in the high word.
            var time = FileBlock.Time;
            b.MoveStr(Size.X - 22, Months[(time >> 21) & 0xF], color);
            b.MoveStr(Size.X - 18, Two((time >> 16) & 0x1F), color);
            b.PutChar(Size.X - 16, ',');
            b.MoveStr(Size.X - 15, Num(((time >> 25) & 0x7F) + 1980), color);
            var hour = (time >> 11) & 0x1F;
            var pm = hour >= 12;
            hour %= 12;
            if (hour == 0)
                hour = 12;
            // UPSTREAM: the 12-hour value is written back, so redrawing a PM time shows it as AM.
            FileBlock.Time = (time & ~(0x1F << 11)) | (hour << 11);
            b.MoveStr(Size.X - 9, Two(hour), color);
            b.PutChar(Size.X - 7, ':');
            b.MoveStr(Size.X - 6, Two((time >> 5) & 0x3F), color);
            b.MoveStr(Size.X - 4, pm ? PmText : AmText, color);
        }
        WriteLine(0, 1, Size.X, 1, b);
        b.MoveChar(0, ' ', color, Size.X);
        WriteLine(0, 2, Size.X, Size.Y - 2, b);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>Copies the entry of a <c>cmFileFocused</c> broadcast and redraws.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast && ev.Message.Command == cmFileFocused)
        {
            var r = (TSearchRec)ev.Message.InfoPtr!;
            FileBlock = new TSearchRec { Attr = r.Attr, Time = r.Time, Size = r.Size, Name = r.Name };
            DrawView();
        }
    }
}
