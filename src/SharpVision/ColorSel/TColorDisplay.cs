using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.ColorSel.ColorSelCommands;
using static SharpVision.Events.EventCodes;

namespace SharpVision.ColorSel;

/// <summary>A sample text drawn in the palette entry being edited, which it updates as the selectors change.</summary>
/// Ported from source/tvision/colorsel.cpp, include/tvision/colorsel.h (class TColorDisplay).
public class TColorDisplay : TView
{
    /// <summary>The palette data holding the edited entry (upstream's <c>TColorAttr *color</c> is <c>Color[ColorIndex]</c>).</summary>
    protected TColorAttr[]? Color;

    /// <summary>The index of the edited entry in <see cref="Color"/>.</summary>
    protected int ColorIndex;

    /// <summary>The sample text, repeated across the view.</summary>
    protected string Text;

    /// <summary>Creates a display showing <paramref name="aText"/>.</summary>
    public TColorDisplay(TRect bounds, string aText) : base(bounds)
    {
        Text = aText;
        EventMask |= evBroadcast;
    }

    /// <summary>Fills the view with the text in the edited color.</summary>
    public override void Draw()
    {
        // UPSTREAM: dereferences the color without a null check, so drawing before SetColor fails.
        TColorAttr c = Color![ColorIndex];
        // BIOS color 0 has a special meaning in TDrawBuffer functions, so display
        // it as an invalid color.
        if (c == 0)
            c = ErrorAttr;
        var len = TText.Width(Text);
        var b = new TDrawBuffer();
        for (var i = 0; i <= Size.X / len; i++)
            b.MoveStr(i * len, Text, c);
        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    /// <summary>Applies foreground and background changes to the edited entry.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast)
            switch (ev.Message.Command)
            {
                case cmColorBackgroundChanged:
                    Color![ColorIndex] = ((byte)Color[ColorIndex] & 0x0F) | ((ev.Message.InfoByte << 4) & 0xF0);
                    DrawView();
                    break;

                case cmColorForegroundChanged:
                    Color![ColorIndex] = ((byte)Color[ColorIndex] & 0xF0) | (ev.Message.InfoByte & 0x0F);
                    DrawView();
                    break;
            }
    }

    /// <summary>Edits entry <paramref name="index"/> of <paramref name="data"/> and tells the selectors its attribute.</summary>
    public virtual void SetColor(TColorAttr[] data, int index)
    {
        Color = data;
        ColorIndex = index;
        Message(Owner, evBroadcast, cmColorSet, (byte)Color[ColorIndex]);
        DrawView();
    }
}
