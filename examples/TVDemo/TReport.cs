using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;

/// <summary>The line under the ASCII table showing the current character's codes.</summary>
/// Ported from examples/tvdemo/ascii.h, ascii.cpp (class TReport).
internal sealed class TReport(TRect r) : TView(r)
{
    private byte _asciiChar;

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var color = GetColor(6);
        var ch = TText.FromCodePage(_asciiChar == 0 ? (byte)0x20 : _asciiChar);
        var str = $"  Char: {ch} Decimal: {_asciiChar,3} Hex {_asciiChar,2:X}     ";

        buf.MoveStr(0, str, color);
        WriteLine(0, 0, 32, 1, buf);
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast && ev.Message.Command == TTable.cmAsciiTableCmdBase + TTable.cmCharFocused)
        {
            _asciiChar = ev.Message.InfoByte;
            DrawView();
        }
    }
}
