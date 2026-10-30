// Ported from source/tvision/tmulchkb.cpp, include/tvision/dialogs.h (class TMultiCheckBoxes).

using System;

using SharpVision.Objects;

namespace SharpVision.Dialogs;

/// <summary>
/// Check boxes with more than two states each: every item owns a bit field of the value, and
/// pressing an item cycles it through <c>selRange</c> states, drawn with the characters of <c>states</c>.
/// </summary>
public class TMultiCheckBoxes : TCluster
{
    private readonly byte _selRange;
    private readonly ushort _flags;
    private readonly string _states;

    /// <summary>
    /// Creates multi-state check boxes. <paramref name="aFlags"/> is one of the <c>cfXxx</c> constants;
    /// <paramref name="aStates"/> holds one marker character per state.
    /// </summary>
    public TMultiCheckBoxes(TRect bounds, TSItem? aStrings, byte aSelRange, ushort aFlags, string aStates)
        : base(bounds, aStrings)
    {
        _selRange = aSelRange;
        _flags = aFlags;
        _states = aStates;
    }

    /// <inheritdoc />
    public override void Draw() => DrawMultiBox(" [ ] ", _states);

    /// <summary>One data slot holding the value as a <see cref="uint"/>.</summary>
    public override int DataSize() => 1;

    /// <inheritdoc />
    public override byte MultiMark(int item)
    {
        var flo = _flags & 0xff;
        var fhi = (_flags >> 8) * item;
        return (byte)((Value & ((uint)flo << fhi)) >> fhi);
    }

    /// <summary>Writes the value into <paramref name="rec"/> as a <see cref="uint"/>.</summary>
    public override void GetData(Span<object?> rec)
    {
        rec[0] = Value;
        DrawView();
    }

    /// <summary>Advances <paramref name="item"/> to its next state, wrapping to 0.</summary>
    public override void Press(int item)
    {
        var flo = _flags & 0xff;
        var fhi = (_flags >> 8) * item;
        var curState = (short)((Value & ((uint)flo << fhi)) >> fhi);
        curState++;
        if (curState >= _selRange)
            curState = 0;
        Value = (Value & ~((uint)flo << fhi)) | ((uint)curState << fhi);
    }

    /// <summary>Reads the value from <paramref name="rec"/> (a <see cref="uint"/>).</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
        Value = rec[0] is uint v ? v : 0u;
        DrawView();
    }
}
