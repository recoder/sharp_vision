// Ported from source/tvision/tcheckbo.cpp, source/tvision/tvtext1.cpp (button),
// include/tvision/dialogs.h (class TCheckBoxes).

using SharpVision.Objects;

namespace SharpVision.Dialogs;

/// <summary>A cluster of independent on/off boxes; bit <c>n</c> of the value is item <c>n</c>.</summary>
public class TCheckBoxes : TCluster
{
    private const string Button = " [ ] ";

    /// <summary>Creates check boxes labeled by <paramref name="aStrings"/>.</summary>
    public TCheckBoxes(TRect bounds, TSItem? aStrings)
        : base(bounds, aStrings)
    {
    }

    /// <inheritdoc />
    public override void Draw() => DrawMultiBox(Button, " X");

    /// <inheritdoc />
    public override bool Mark(int item) => (Value & (1u << item)) != 0;

    /// <inheritdoc />
    public override void Press(int item) => Value ^= 1u << item;
}
