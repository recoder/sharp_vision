// Ported from include/tvision/dialogs.h (class TSItem).

namespace SharpVision.Dialogs;

/// <summary>A node in a linked list of strings, used to build a <see cref="TCluster"/>.</summary>
public class TSItem
{
    /// <summary>The string.</summary>
    public string Value;

    /// <summary>The next node, or null.</summary>
    public TSItem? Next;

    /// <summary>Creates a node holding <paramref name="aValue"/> followed by <paramref name="aNext"/>.</summary>
    public TSItem(string aValue, TSItem? aNext)
    {
        Value = aValue;
        Next = aNext;
    }
}
