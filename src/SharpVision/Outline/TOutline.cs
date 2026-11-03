using SharpVision.Objects;
using SharpVision.Views;

namespace SharpVision.Outline;

/// <summary>An outline viewer over a tree of <see cref="TNode"/>s.</summary>
/// Ported from source/tvision/toutline.cpp, include/tvision/outline.h (class TOutline).
public class TOutline : TOutlineViewer
{
    /// <summary>The first top-level node, or null.</summary>
    public TNode? Root;

    /// <summary>Creates the outline of <paramref name="aRoot"/> and its siblings.</summary>
    public TOutline(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar, TNode? aRoot)
        : base(bounds, aHScrollBar, aVScrollBar)
    {
        Root = aRoot;
        Update();
    }

    /// <inheritdoc />
    public override void Adjust(TNode node, bool expand) => node.Expanded = expand;

    /// <inheritdoc />
    public override TNode? GetRoot() => Root;

    /// <inheritdoc />
    public override int GetNumChildren(TNode node)
    {
        var i = 0;
        for (var p = node.ChildList; p is not null; p = p.Next)
            i++;
        return i;
    }

    /// <inheritdoc />
    public override TNode? GetNext(TNode node) => node.Next;

    /// <inheritdoc />
    public override TNode? GetChild(TNode node, int i)
    {
        var p = node.ChildList;
        while (i != 0 && p is not null)
        {
            i--;
            p = p.Next;
        }
        return p;
    }

    /// <inheritdoc />
    public override string GetText(TNode node) => node.Text;

    /// <inheritdoc />
    public override bool IsExpanded(TNode node) => node.Expanded;

    /// <inheritdoc />
    public override bool HasChildren(TNode node) => node.ChildList is not null;
}
