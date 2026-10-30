// Ported from source/tvision/tradiobu.cpp, source/tvision/tvtext1.cpp (button),
// include/tvision/dialogs.h (class TRadioButtons).

using System;

using SharpVision.Objects;

namespace SharpVision.Dialogs;

/// <summary>A cluster of mutually exclusive buttons; the value is the index of the chosen item.</summary>
public class TRadioButtons : TCluster
{
    private const string Button = " ( ) ";

    /// <summary>Creates radio buttons labeled by <paramref name="aStrings"/>.</summary>
    public TRadioButtons(TRect bounds, TSItem? aStrings)
        : base(bounds, aStrings)
    {
    }

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
