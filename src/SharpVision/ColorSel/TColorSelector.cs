using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.ColorSel.ColorSelCommands;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.ColorSel;

/// <summary>A 4-column grid of the 16 foreground or 8 background colors.</summary>
/// Ported from source/tvision/colorsel.cpp, source/tvision/tvtext1.cpp, include/tvision/colorsel.h (class TColorSelector).
public class TColorSelector : TView
{
    /// <summary>Which half of the attribute the selector picks.</summary>
    public enum ColorSel
    {
        /// <summary>The background, colors 0 to 7.</summary>
        csBackground,
        /// <summary>The foreground, colors 0 to 15.</summary>
        csForeground,
    }

    /// <summary>The character each color cell is drawn with (CP437 0xDB).</summary>
    public static char Icon = '█';

    /// <summary>The selected color.</summary>
    protected byte Color;

    /// <summary>Whether this is the foreground or background selector.</summary>
    protected ColorSel SelType;

    /// <summary>Creates a selector of type <paramref name="aSelType"/>.</summary>
    public TColorSelector(TRect bounds, ColorSel aSelType) : base(bounds)
    {
        Options |= ofSelectable | ofFirstClick | ofFramed;
        EventMask |= evBroadcast;
        SelType = aSelType;
        Color = 0;
    }

    /// <summary>Draws the color cells and marks the selected one.</summary>
    public override void Draw()
    {
        var b = new TDrawBuffer();
        b.MoveChar(0, ' ', 0x70, Size.X);
        // UPSTREAM: the loop also draws row Size.Y, which is outside the view and gets clipped.
        for (var i = 0; i <= Size.Y; i++)
        {
            if (i < 4)
            {
                for (var j = 0; j < 4; j++)
                {
                    var c = i * 4 + j;
                    b.MoveChar(j * 3, Icon, c, 3);
                    if (c == Color)
                    {
                        b.PutChar(j * 3 + 1, '◘');
                        if (c == 0)
                            b.PutAttribute(j * 3 + 1, 0x70);
                    }
                }
            }
            WriteLine(0, i, Size.X, 1, b);
        }
    }

    private void ColorChanged()
    {
        var msg = SelType == ColorSel.csForeground ? cmColorForegroundChanged : cmColorBackgroundChanged;
        Message(Owner, evBroadcast, msg, Color);
    }

    /// <summary>Picks a color with the mouse or the arrow keys, and follows <c>cmColorSet</c>.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        const int width = 4;

        base.HandleEvent(ref ev);

        var oldColor = Color;
        var maxCol = SelType == ColorSel.csBackground ? 7 : 15;
        switch (ev.What)
        {
            case evMouseDown:
                do
                {
                    if (MouseInView(ev.Mouse.Where))
                    {
                        var mouse = MakeLocal(ev.Mouse.Where);
                        Color = (byte)(mouse.Y * 4 + mouse.X / 3);
                    }
                    else
                        Color = oldColor;
                    ColorChanged();
                    DrawView();
                } while (MouseEvent(ref ev, evMouseMove));
                break;

            case evKeyDown:
                switch (Util.CtrlToArrow(ev.KeyDown.KeyCode))
                {
                    case kbLeft:
                        if (Color > 0)
                            Color--;
                        else
                            Color = (byte)maxCol;
                        break;

                    case kbRight:
                        if (Color < maxCol)
                            Color++;
                        else
                            Color = 0;
                        break;

                    case kbUp:
                        if (Color > width - 1)
                            Color -= width;
                        else if (Color == 0)
                            Color = (byte)maxCol;
                        else
                            Color += (byte)(maxCol - width);
                        break;

                    case kbDown:
                        if (Color < maxCol - (width - 1))
                            Color += width;
                        else if (Color == maxCol)
                            Color = 0;
                        else
                            Color -= (byte)(maxCol - width);
                        break;

                    default:
                        return;
                }
                break;

            case evBroadcast:
                if (ev.Message.Command == cmColorSet)
                {
                    if (SelType == ColorSel.csBackground)
                        Color = (byte)(ev.Message.InfoByte >> 4);
                    else
                        Color = (byte)(ev.Message.InfoByte & 0x0F);
                    DrawView();
                }
                return;

            default:
                return;
        }
        DrawView();
        ColorChanged();
        ClearEvent(ref ev);
    }
}
