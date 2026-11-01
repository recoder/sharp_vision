using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Views.ViewFlags;

namespace SharpVision.Editors;

/// <summary>Shows an editor's cursor position as <c>line:column</c>, and a mark when the text is modified.</summary>
/// <remarks>Palette layout: 1 = frame passive, 2 = frame active.</remarks>
/// Ported from source/tvision/tindictr.cpp, source/tvision/tvtext1.cpp (frames),
/// include/tvision/editors.h (class TIndicator).
public class TIndicator : TView
{
    private static readonly TPalette _palette = new([0x02, 0x03]);
    private const char DragFrame = '═';
    private const char NormalFrame = '─';

    /// <summary>The cursor position shown, zero-based.</summary>
    protected TPoint Location;

    /// <summary>Whether the modified mark is shown.</summary>
    protected bool Modified;

    /// <summary>Creates the indicator; it sticks to the bottom of its owner.</summary>
    public TIndicator(TRect bounds)
        : base(bounds)
    {
        GrowMode = gfGrowLoY | gfGrowHiY;
    }

    /// <summary>Draws the frame line with the position, and '☼' when modified.</summary>
    public override void Draw()
    {
        // UPSTREAM: the double line is drawn when not dragging.
        TColorAttr color;
        char frame;
        if ((State & sfDragging) == 0)
        {
            color = GetColor(1)[0];
            frame = DragFrame;
        }
        else
        {
            color = GetColor(2)[0];
            frame = NormalFrame;
        }

        var b = new TDrawBuffer();
        b.MoveChar(0, frame, color, Size.X);
        if (Modified)
            b.PutChar(0, '☼');
        var s = $" {Location.Y + 1}:{Location.X + 1} ";
        b.MoveStr(8 - s.IndexOf(':'), s, color);
        WriteBuf(0, 0, Size.X, 1, b);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>Redraws when dragging starts or stops.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if (aState == sfDragging)
            DrawView();
    }

    /// <summary>Shows <paramref name="aLocation"/> and the modified state, redrawing if either changed.</summary>
    public void SetValue(TPoint aLocation, bool aModified)
    {
        if (Location != aLocation || Modified != aModified)
        {
            Location = aLocation;
            Modified = aModified;
            DrawView();
        }
    }
}
