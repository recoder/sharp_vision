using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

/// <summary>The 32x8 grid of code page characters; the cursor marks the current one.</summary>
/// Ported from examples/tvdemo/ascii.h, ascii.cpp (class TTable).
internal sealed class TTable : TView
{
    public const ushort cmAsciiTableCmdBase = 910;
    public const ushort cmCharFocused = 0;

    public TTable(TRect r) : base(r)
    {
        EventMask |= evKeyboard;
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var color = GetColor(6);

        for (var y = 0; y <= Size.Y - 1; y++)
        {
            buf.MoveChar(0, ' ', color, Size.X);
            for (var x = 0; x <= Size.X - 1; x++)
                buf.MoveStr(x, TText.FromCodePage((byte)(32 * y + x)), color, 1);
            WriteLine(0, y, Size.X, 1, buf);
        }
        ShowCursor();
    }

    // cmCharFocused is a offset value (basically the ascii code of the
    // current selected character) thus should be added, not or'ed, to
    // cmAsciiTableCmdBase.
    public void CharFocused() =>
        Message(Owner, evBroadcast, cmAsciiTableCmdBase + cmCharFocused, Cursor.X + 32 * Cursor.Y);

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (ev.What == evMouseDown)
        {
            do
            {
                if (MouseInView(ev.Mouse.Where))
                {
                    var spot = MakeLocal(ev.Mouse.Where);
                    SetCursor(spot.X, spot.Y);
                    CharFocused();
                }
            } while (MouseEvent(ref ev, evMouseMove));
            ClearEvent(ref ev);
        }
        else if (ev.What == evKeyboard)
        {
            switch (ev.KeyDown.KeyCode)
            {
                case kbHome:
                    SetCursor(0, 0);
                    break;
                case kbEnd:
                    SetCursor(Size.X - 1, Size.Y - 1);
                    break;
                case kbUp:
                    if (Cursor.Y > 0)
                        SetCursor(Cursor.X, Cursor.Y - 1);
                    break;
                case kbDown:
                    if (Cursor.Y < Size.Y - 1)
                        SetCursor(Cursor.X, Cursor.Y + 1);
                    break;
                case kbLeft:
                    if (Cursor.X > 0)
                        SetCursor(Cursor.X - 1, Cursor.Y);
                    break;
                case kbRight:
                    if (Cursor.X < Size.X - 1)
                        SetCursor(Cursor.X + 1, Cursor.Y);
                    break;
                default:
                    SetCursor(ev.KeyDown.CharScan.CharCode % 32, ev.KeyDown.CharScan.CharCode / 32);
                    break;
            }
            CharFocused();
            ClearEvent(ref ev);
        }
    }
}
