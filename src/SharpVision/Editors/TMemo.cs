using System;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Editors;

/// <summary>
/// An editor for use in a dialog: it transfers its text as data and leaves Tab to the dialog.
/// </summary>
/// Ported from source/tvision/tmemo.cpp, include/tvision/editors.h (class TMemo).
/// <remarks>The data slot is the text as a string, standing in for upstream's <c>TMemoData</c>.</remarks>
public class TMemo : TEditor
{
    private static readonly TPalette _palette = new([0x1A, 0x1B]);

    /// <summary>Creates a memo; see <see cref="TEditor(TRect, TScrollBar?, TScrollBar?, TIndicator?, int)"/>.</summary>
    public TMemo(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar, TIndicator? aIndicator, int aBufSize)
        : base(bounds, aHScrollBar, aVScrollBar, aIndicator, aBufSize)
    {
    }

    /// <summary>One slot.</summary>
    public override int DataSize() => 1;

    /// <summary>Writes the text into the slot.</summary>
    public override void GetData(Span<object?> rec)
    {
        var text = new char[BufLen];
        GetText(0, text);
        rec[0] = new string(text);
    }

    /// <summary>Replaces the text with the slot's string, unless it does not fit in the buffer.</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
        var s = rec[0] as string ?? "";
        if (SetBufSize(s.Length))
        {
            s.CopyTo(Buffer.AsSpan(BufSize - s.Length));
            SetBufLen(s.Length);
        }
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>Handles everything but Tab, which moves between the dialog's controls.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        if (ev.What != evKeyDown || ev.KeyDown.KeyCode != kbTab)
            base.HandleEvent(ref ev);
    }
}
