using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

namespace SharpVision.App;

/// <summary>The desktop's background: a view filled with a pattern character.</summary>
/// <remarks>Palette layout: 1 = background.</remarks>
/// Ported from include/tvision/app.h (class TBackground) and source/tvision/tbkgrnd.cpp.
public class TBackground : TView
{
    private static readonly TPalette _palette = new([0x01]);

    /// <summary>The fill character.</summary>
    public char Pattern;

    /// <summary>Creates a background filled with <paramref name="pattern"/>.</summary>
    public TBackground(TRect bounds, char pattern) : base(bounds)
    {
        Pattern = pattern;
        GrowMode = (byte)(gfGrowHiX | gfGrowHiY);
    }

    /// <summary>Fills the view with <see cref="Pattern"/>.</summary>
    public override void Draw()
    {
        var b = new TDrawBuffer();
        b.MoveChar(0, Pattern, GetColor(0x01), Size.X);
        WriteLine(0, 0, Size.X, Size.Y, b);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;
}
