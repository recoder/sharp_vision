using System.Collections.Generic;

namespace SharpVision.Dialogs;

/// <summary>The data slot of a <see cref="TListBox"/>: its items and the focused item.</summary>
/// Ported from include/tvision/dialogs.h (struct TListBoxRec).
public struct TListBoxRec
{
    /// <summary>The items, or null for an empty list.</summary>
    public List<string>? Items;

    /// <summary>The focused item.</summary>
    public ushort Selection;
}
