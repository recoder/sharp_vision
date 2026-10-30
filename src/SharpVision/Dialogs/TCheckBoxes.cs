using SharpVision.Objects;

namespace SharpVision.Dialogs;

/// <summary>A cluster of independent on/off boxes; bit <c>n</c> of the value is item <c>n</c>.</summary>
/// Ported from source/tvision/tcheckbo.cpp, source/tvision/tvtext1.cpp (button),
/// include/tvision/dialogs.h (class TCheckBoxes).
/// <remarks>Creates check boxes labeled by <paramref name="aStrings"/>.</remarks>
public class TCheckBoxes(TRect bounds, TSItem? aStrings) : TCluster(bounds, aStrings)
{
    private const string Button = " [ ] ";

    /// <inheritdoc />
    public override void Draw() => DrawMultiBox(Button, " X");

    /// <inheritdoc />
    public override bool Mark(int item) => (Value & (1u << item)) != 0;

    /// <inheritdoc />
    public override void Press(int item) => Value ^= 1u << item;
}
