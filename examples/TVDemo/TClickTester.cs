using SharpVision.Dialogs;
using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;

/// <summary>A text that changes color on each double click, to try the double click speed.</summary>
/// Ported from examples/tvdemo/mousedlg.h, mousedlg.cpp (class TClickTester).
internal sealed class TClickTester(TRect r, string aText) : TStaticText(r, aText)
{
    private static readonly TPalette _palette = new([0x07, 0x08]);
    private bool _clicked;

    public override TPalette GetPalette() => _palette;

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == evMouseDown)
        {
            if ((ev.Mouse.EventFlags & meDoubleClick) != 0)
            {
                _clicked = !_clicked;
                DrawView();
            }
            ClearEvent(ref ev);
        }
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var c = _clicked ? GetColor(2) : GetColor(1);

        buf.MoveChar(0, ' ', c, Size.X);
        buf.MoveStr(0, Text, c);
        WriteLine(0, 0, Size.X, 1, buf);
    }
}
