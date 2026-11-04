using SharpVision.Dialogs;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.DialogFlags;
using static SharpVision.Views.ViewFlags;

/// <summary>A four-function calculator.</summary>
/// Ported from examples/tvdemo/calc.h, calc.cpp (class TCalculator).
internal sealed class TCalculator : TDialog
{
    private static readonly string[] _keyChar =
    [
        "C", "←", "%", "±", // '←' is escape (0x1B), '±' is the +/- char (0xF1).
        "7", "8", "9", "/",
        "4", "5", "6", "*",
        "1", "2", "3", "-",
        "0", ".", "=", "+",
    ];

    public TCalculator() : base(new TRect(5, 3, 29, 18), "Calculator")
    {
        Options |= ofFirstClick;

        for (var i = 0; i <= 19; i++)
        {
            var x = i % 4 * 5 + 2;
            var y = i / 4 * 2 + 4;
            var r = new TRect(x, y, x + 5, y + 2);

            TView tv = new TButton(r, _keyChar[i], TCalcDisplay.cmCalcButton, bfNormal | bfBroadcast);
            tv.Options &= unchecked((ushort)~ofSelectable);
            Insert(tv);
        }
        Insert(new TCalcDisplay(new TRect(3, 2, 21, 3)));
    }
}
