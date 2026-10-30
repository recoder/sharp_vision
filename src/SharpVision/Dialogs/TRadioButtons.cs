using System;

using SharpVision.Objects;

namespace SharpVision.Dialogs;

/// <summary>A cluster of mutually exclusive buttons; the value is the index of the chosen item.</summary>
/// Ported from source/tvision/tradiobu.cpp, source/tvision/tvtext1.cpp (button),
/// <remarks>Creates radio buttons labeled by <paramref name="aStrings"/>.</remarks>
// include/tvision/dialogs.h (class TRadioButtons).
public class TRadioButtons(TRect bounds, TSItem? aStrings) : TCluster(bounds, aStrings)
{
    private const string Button = " ( ) ";

    /// <inheritdoc />
    public override void Draw() => DrawMultiBox(Button, " •");

    /// <inheritdoc />
    public override bool Mark(int item) => item == (int)Value;

    /// <inheritdoc />
    public override void Press(int item) => Value = (uint)item;

    /// <inheritdoc />
    public override void MovedTo(int item) => Value = (uint)item;

    /// <inheritdoc />
    public override void SetData(ReadOnlySpan<object?> rec)
    {
        base.SetData(rec);
        Sel = (int)Value;
    }
}
