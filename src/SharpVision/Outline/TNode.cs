namespace SharpVision.Outline;

/// <summary>A node of a <see cref="TOutline"/>: its text, its children and its next sibling.</summary>
/// Ported from include/tvision/outline.h (class TNode).
public class TNode
{
    /// <summary>The next sibling, or null.</summary>
    public TNode? Next;
    /// <summary>The text shown for the node.</summary>
    public string Text;
    /// <summary>The first child, or null.</summary>
    public TNode? ChildList;
    /// <summary>Whether the children are shown.</summary>
    public bool Expanded;

    /// <summary>Creates an expanded node with no children or siblings.</summary>
    public TNode(string aText)
        : this(aText, null, null)
    {
    }

    /// <summary>Creates a node with its first child and next sibling.</summary>
    public TNode(string aText, TNode? aChildren, TNode? aNext, bool initialState = true)
    {
        Next = aNext;
        Text = aText;
        ChildList = aChildren;
        Expanded = initialState;
    }
}
