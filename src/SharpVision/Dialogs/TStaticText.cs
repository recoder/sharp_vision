using System;

using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

namespace SharpVision.Dialogs;

/// <summary>
/// Fixed text, word-wrapped to the view's width. A line starting with <c>'\x03'</c> is
/// centered, and <c>'\n'</c> starts a new line.
/// </summary>
/// <remarks>Palette layout: 1 = text.</remarks>
/// Ported from source/tvision/tstatict.cpp, include/tvision/dialogs.h (class TStaticText).
public class TStaticText : TView
{
    private static readonly TPalette _palette = new([0x06]);

    /// <summary>The text, or null.</summary>
    protected string? Text;

    /// <summary>Creates a static text view showing <paramref name="aText"/>.</summary>
    public TStaticText(TRect bounds, string? aText) : base(bounds)
    {
        Text = aText;
        GrowMode |= (byte)gfFixed;
    }

    /// <summary>Draws the text, wrapping at spaces.</summary>
    public override void Draw()
    {
        TColorAttr color;
        bool center;
        int i, j, l, p, y;
        var b = new TDrawBuffer();
        ReadOnlySpan<char> s;

        color = GetColor(1);
        var buf = GetText();
        s = buf;
        // If possible, read directly from 'text' to prevent truncation.
        if (Text is not null && Text.AsSpan().StartsWith(s))
            s = Text;
        l = s.Length;
        p = 0;
        y = 0;
        center = false;
        while (y < Size.Y)
        {
            b.MoveChar(0, ' ', color, Size.X);
            if (p < l)
            {
                if (s[p] == '\x03')
                {
                    center = true;
                    ++p;
                }
                i = p;
                var last = i + TText.Scroll(s[i..], Size.X, false);
                do
                {
                    j = p;
                    while (p < l && s[p] == ' ')
                        ++p;
                    while (p < l && s[p] != ' ' && s[p] != '\n')
                        p += TText.Next(s[p..]);
                } while (p < l && p < last && s[p] != '\n');
                if (p > last)
                {
                    if (j > i)
                        p = j;
                    else
                        p = last;
                }
                var width = TText.Width(s[i..p]);
                if (center)
                    j = (Size.X - width) / 2;
                else
                    j = 0;
                b.MoveStr(j, s[i..], color, (ushort)width);
                while (p < l && s[p] == ' ')
                    p++;
                if (p < l && s[p] == '\n')
                {
                    center = false;
                    p++;
                }
            }
            WriteLine(0, y++, Size.X, 1, b);
        }
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>Returns the text to draw, at most 255 code units.</summary>
    /// <remarks>Upstream fills a 256-byte buffer; <see cref="Draw"/> still shows the whole <see cref="Text"/> when this is a prefix of it.</remarks>
    public virtual string GetText()
    {
        if (Text is null)
            return "";
        return Text.Length > 255 ? Text[..255] : Text;
    }
}
