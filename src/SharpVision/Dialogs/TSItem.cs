namespace SharpVision.Dialogs;

/// <summary>A node in a linked list of strings, used to build a <see cref="TCluster"/>.</summary>
/// <remarks>Creates a node holding <paramref name="aValue"/> followed by <paramref name="aNext"/>.</remarks>
/// Ported from include/tvision/dialogs.h (class TSItem).
public class TSItem(string aValue, TSItem? aNext)
{
    /// <summary>The string.</summary>
    public string Value = aValue;

    /// <summary>The next node, or null.</summary>
    public TSItem? Next = aNext;
}
