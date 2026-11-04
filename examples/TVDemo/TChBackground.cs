using SharpVision.App;
using SharpVision.Dialogs;
using SharpVision.Drawing;
using SharpVision.Objects;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Views.Commands;

/// <summary>Asks for the desktop's background character.</summary>
/// Ported from examples/tvdemo/backgrnd.h, backgrnd.cpp (class TChBackground).
internal sealed class TChBackground : TDialog
{
    private readonly TBackground? _background;
    private readonly TInputLine _input;

    public TChBackground(TBackground? b) : base(new TRect(0, 0, 29, 9), null)
    {
        _background = b;
        // Ideally, we would show an ASCII table and let the user choose from there.
        var r = GetExtent();
        r.Move((TProgram.DeskTop!.Size.X - r.B.X) / 2, (TProgram.DeskTop.Size.Y - r.B.Y) / 2);
        ChangeBounds(r);
        _input = new TInputLine(new TRect(4, 5, 7, 6), 1, null, TInputLine.ilMaxChars);
        Insert(_input);
        Insert(new TStaticText(new TRect(2, 2, 27, 3), "Enter background pattern:"));
        Insert(new TButton(new TRect(16, 4, 26, 6), "~A~pply", cmOK, bfDefault));
        Insert(new TButton(new TRect(16, 6, 26, 8), "~C~lose", cmCancel, bfNormal));
        _input.Focus();
    }

    public override bool Valid(ushort command)
    {
        if (base.Valid(command))
        {
            if (_background is not null && command == cmOK)
            {
                // The pattern is a code page character; one that has none (0) is ignored.
                var pattern = TText.ToCodePage(_input.Data);
                if (pattern != 0)
                {
                    _background.Pattern = TText.FromCodePage(pattern)[0];
                    _background.DrawView();
                }
                return false; // Keep dialog open.
            }
            return true;
        }
        return false;
    }
}
