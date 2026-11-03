using System;
using System.Text;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Outline.OutlineFlags;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Outline;

/// <summary>
/// A scroller that shows a tree as an outline, one node per line, with graph characters for the levels.
/// Subclasses supply the tree; <see cref="TOutline"/> uses <see cref="TNode"/>s.
/// </summary>
/// Ported from source/tvision/toutline.cpp, include/tvision/outline.h (class TOutlineViewer).
public abstract class TOutlineViewer : TScroller
{
    // UPSTREAM: sizeof(cpOutlineViewer) counts the terminating NUL, so the palette has a fifth entry 0.
    private static readonly TPalette _palette = new([0x06, 0x07, 0x03, 0x08, 0x00]);

    /// <summary>The display position of the focused node.</summary>
    public int Foc;

    /// <summary>Creates an empty outline viewer with its optional scroll bars.</summary>
    protected TOutlineViewer(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar)
        : base(bounds, aHScrollBar, aVScrollBar)
    {
        GrowMode = gfGrowHiX + gfGrowHiY;
        Foc = 0;
    }

    /// <summary>Expands or collapses <paramref name="node"/>.</summary>
    public abstract void Adjust(TNode node, bool expand);

    /// <summary>The next sibling of <paramref name="node"/>, or null.</summary>
    public abstract TNode? GetNext(TNode node);

    /// <summary>The child <paramref name="i"/> of <paramref name="node"/>.</summary>
    public abstract TNode? GetChild(TNode node, int i);

    /// <summary>The number of children of <paramref name="node"/>.</summary>
    public abstract int GetNumChildren(TNode node);

    /// <summary>The first top-level node, or null.</summary>
    public abstract TNode? GetRoot();

    /// <summary>The text of <paramref name="node"/>.</summary>
    public abstract string GetText(TNode node);

    /// <summary>Whether <paramref name="node"/> has children.</summary>
    public abstract bool HasChildren(TNode node);

    /// <summary>Whether the children of <paramref name="node"/> are shown.</summary>
    public abstract bool IsExpanded(TNode node);

    // Called internally to ensure the focus is within range and displayed.
    private void AdjustFocus(int newFocus)
    {
        if (newFocus < 0)
            newFocus = 0;
        else if (newFocus >= Limit.Y)
            newFocus = Limit.Y - 1;
        if (Foc != newFocus)
            Focused(newFocus);
        if (newFocus < Delta.Y)
            ScrollTo(Delta.X, newFocus);
        else if (newFocus - Size.Y >= Delta.Y)
            ScrollTo(Delta.X, newFocus - Size.Y + 1);
    }

    /// <summary>Draws the visible nodes, with the focused and selected ones highlighted.</summary>
    public override void Draw()
    {
        var nrmColor = GetColor(0x0401);
        var dBuf = new TDrawBuffer();
        var auxPos = -1;
        FirstThat((_, cur, level, position, lines, flags) =>
        {
            if (position >= Delta.Y)
            {
                if (position >= Delta.Y + Size.Y)
                    return true;

                TAttrPair color;
                if (position == Foc && (State & sfFocused) != 0)
                    color = GetColor(0x0202);
                else if (IsSelected(position))
                    color = GetColor(0x0303);
                else
                    color = GetColor(0x0401);
                dBuf.MoveChar(0, ' ', color[0], Size.X);
                var graph = GetGraph(level, lines, flags);
                var x = Strwidth(graph) - Delta.X;
                if (x > 0)
                    dBuf.MoveStr(0, graph, color[0], ushort.MaxValue, Delta.X);
                var c = (flags & ovExpanded) != 0 ? color[0] : color[1];
                dBuf.MoveStr(Math.Max(0, x), GetText(cur), c, ushort.MaxValue, Math.Max(0, -x));
                WriteLine(0, position - Delta.Y, Size.X, 1, dBuf);
                auxPos = position;
            }
            return false;
        });
        dBuf.MoveChar(0, ' ', nrmColor[0], Size.X);
        WriteLine(0, auxPos + 1, Size.X, Size.Y - (auxPos - Delta.Y), dBuf);
    }

    /// <summary>Expands <paramref name="node"/> and all the nodes below it.</summary>
    public void ExpandAll(TNode node)
    {
        if (HasChildren(node))
        {
            Adjust(node, true);
            var n = GetNumChildren(node) - 1;
            for (var i = 0; i <= n; i++)
                ExpandAll(GetChild(node, i)!);
        }
    }

    /// <summary>
    /// Builds a graph string: for each of the <paramref name="level"/> levels, a mark if the level's bit is
    /// set in <paramref name="lines"/> followed by fillers to <paramref name="levWidth"/>; then the node end,
    /// <paramref name="endWidth"/> wide, chosen by the <c>ovXxx</c> <paramref name="flags"/>.
    /// <paramref name="chars"/> holds the level filler, level mark, end first (not last / last child),
    /// end filler, end child, retracted and expanded characters.
    /// </summary>
    public string CreateGraph(int level, long lines, ushort flags, int levWidth, int endWidth, string chars)
    {
        const int fillerOrBar = 0, yOrL = 2, straightOrTee = 4, retracted = 6;

        var graph = new StringBuilder();
        var expanded = (flags & ovExpanded) != 0;
        var children = (flags & ovChildren) != 0;
        var last = (flags & ovLast) != 0;

        for (; level > 0; level--, lines >>= 1)
        {
            graph.Append((lines & 1) != 0 ? chars[fillerOrBar + 1] : chars[fillerOrBar]);
            graph.Append(chars[fillerOrBar], levWidth - 1);
        }

        if (--endWidth > 0)
        {
            graph.Append(last ? chars[yOrL + 1] : chars[yOrL]);
            if (--endWidth > 0)
            {
                if (--endWidth > 0)
                    graph.Append(chars[straightOrTee], endWidth);
                graph.Append(children ? chars[straightOrTee + 1] : chars[straightOrTee]);
            }
            graph.Append(expanded ? chars[retracted + 1] : chars[retracted]);
        }
        return graph.ToString();
    }

    /// <summary>Visits the shown nodes in display order until <paramref name="test"/> returns true; returns that node.</summary>
    public TNode? FirstThat(TOutlineVisitor test) => Iterate(test, true);

    /// <summary>Visits all the shown nodes in display order.</summary>
    public TNode? ForEach(TOutlineVisitor action) => Iterate(action, false);

    /// <summary>Called when the node at <paramref name="i"/> gets the focus.</summary>
    public virtual void Focused(int i) => Foc = i;

    private TNode? TraverseTree(TOutlineVisitor action, ref int position, bool checkResult, TNode? cur, int level, long lines, bool lastChild)
    {
        if (cur is null)
            return null;

        var children = HasChildren(cur);

        ushort flags = 0;
        if (lastChild)
            flags |= ovLast;
        if (children && IsExpanded(cur))
            flags |= ovChildren;
        if (!children || IsExpanded(cur))
            flags |= ovExpanded;

        position++;

        var result = action(this, cur, level, position, lines, flags);
        if (checkResult && result)
            return cur;

        TNode? ret;
        if (children && IsExpanded(cur))
        {
            // UPSTREAM: the continued levels are narrowed to int.
            var childLines = (int)lines;
            if (!lastChild)
                childLines |= 1 << level;
            var child = GetChild(cur, 0);
            do
            {
                var next = GetNext(child!);
                ret = TraverseTree(action, ref position, checkResult, child, level + 1, childLines, next is null);
                child = next;
                if (ret is not null)
                    return ret;
            } while (child is not null);
        }
        if (cur == GetRoot())
        {
            var next = cur;
            while (GetNext(next) is { } n)
            {
                next = n;
                ret = TraverseTree(action, ref position, checkResult, next, level, lines, GetNext(next) is null);
                if (ret is not null)
                    return ret;
            }
        }
        return null;
    }

    private TNode? Iterate(TOutlineVisitor action, bool checkResult)
    {
        var position = -1;
        var root = GetRoot();
        if (root is not null)
            return TraverseTree(action, ref position, checkResult, root, 0, 0, GetNext(root) is null);
        return null;
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>The graph shown before a node's text; by default 3 columns per level with box-drawing lines.</summary>
    public virtual string GetGraph(int level, long lines, ushort flags)
    {
        const int levelWidth = 3, endWidth = levelWidth;
        return CreateGraph(level, lines, flags, levelWidth, endWidth, " │├└──+─");
    }

    /// <summary>The node shown at display position <paramref name="i"/>, or null.</summary>
    public virtual TNode? GetNode(int i) => FirstThat((_, _, _, position, _, _) => position == i);

    /// <summary>Whether the node at <paramref name="i"/> is selected; by default, whether it is focused.</summary>
    public virtual bool IsSelected(int i) => Foc == i;

    /// <summary>
    /// Mouse: focuses the clicked node and expands or collapses it when its graph is clicked. Keys: arrows, PgUp/PgDn, Home/End and Ctrl-PgUp/PgDn move the focus; Enter selects;
    /// <c>+</c>, <c>-</c> and <c>*</c> expand, collapse and expand all.
    /// </summary>
    public override void HandleEvent(ref TEvent ev)
    {
        const int mouseAutoToSkip = 3;

        var mouse = default(TPoint);
        TNode? cur;
        var newFocus = 0;

        base.HandleEvent(ref ev);
        switch (ev.What)
        {
            case evMouseDown:
                var count = 0;
                byte dragged = 0;
                do
                {
                    if (dragged < 2)
                        dragged++;
                    mouse = MakeLocal(ev.Mouse.Where);
                    if (MouseInView(ev.Mouse.Where))
                    {
                        var i = Delta.Y + mouse.Y;
                        newFocus = i < Limit.Y ? i : Foc;
                    }
                    else
                    {
                        if (ev.What == evMouseAuto)
                            count++;
                        if (count == mouseAutoToSkip)
                        {
                            count = 0;
                            if (mouse.Y < 0)
                                newFocus--;
                            if (mouse.Y >= Size.Y)
                                newFocus++;
                        }
                    }
                    if (Foc != newFocus)
                    {
                        AdjustFocus(newFocus);
                        DrawView();
                    }
                } while (MouseEvent(ref ev, evMouseMove + evMouseAuto));

                // UPSTREAM: 'ev' is now the mouse-up event, which never carries meDoubleClick, so a
                // double click does not select.
                if ((ev.Mouse.EventFlags & meDoubleClick) != 0)
                    Selected(Foc);
                else
                {
                    var focLevel = 0;
                    long focLines = 0;
                    ushort focFlags = 0;
                    if (dragged < 2 && (cur = FirstThat((_, _, level, position, lines, flags) =>
                        {
                            if (position != Foc)
                                return false;
                            (focLevel, focLines, focFlags) = (level, lines, flags);
                            return true;
                        })) is not null)
                    {
                        var graph = GetGraph(focLevel, focLines, focFlags);
                        if (mouse.X < Strwidth(graph))
                        {
                            Adjust(cur, !IsExpanded(cur));
                            Update();
                            DrawView();
                        }
                    }
                }
                break;

            case evKeyboard:
                newFocus = Foc;
                switch (CtrlToArrow(ev.KeyDown.KeyCode))
                {
                    case kbUp:
                    case kbLeft:
                        newFocus--;
                        break;
                    case kbDown:
                    case kbRight:
                        newFocus++;
                        break;
                    case kbPgDn:
                        newFocus += Size.Y - 1;
                        break;
                    case kbPgUp:
                        newFocus -= Size.Y - 1;
                        break;
                    case kbHome:
                        newFocus = Delta.Y;
                        break;
                    case kbEnd:
                        newFocus = Delta.Y + Size.Y - 1;
                        break;
                    case kbCtrlPgUp:
                        newFocus = 0;
                        break;
                    case kbCtrlPgDn:
                        newFocus = Limit.Y - 1;
                        break;
                    case kbCtrlEnter:
                    case kbEnter:
                        Selected(newFocus);
                        break;
                    default:
                        var code = ev.KeyDown.CharScan.CharCode;
                        switch (code)
                        {
                            case (byte)'-':
                            case (byte)'+':
                                if ((cur = GetNode(newFocus)) is not null)
                                    Adjust(cur, code == '+');
                                break;
                            case (byte)'*':
                                if ((cur = GetNode(newFocus)) is not null)
                                    ExpandAll(cur);
                                break;
                            default:
                                return;
                        }
                        Update();
                        break;
                }
                ClearEvent(ref ev);
                AdjustFocus(newFocus);
                DrawView();
                break;
        }
    }

    /// <summary>Called when the user selects the node at <paramref name="i"/> (Enter or double click); does nothing by default.</summary>
    public virtual void Selected(int i)
    {
    }

    /// <summary>Redraws when the focus changes.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if ((aState & sfFocused) != 0)
            DrawView();
    }

    /// <summary>Recomputes the limits from the shown nodes and keeps the focus in range. Call it after the tree changes.</summary>
    public void Update()
    {
        var updateCount = 0;
        var updateMaxX = 0;
        FirstThat((_, p, level, _, lines, flags) =>
        {
            updateCount++;
            var len = Strwidth(GetText(p)) + Strwidth(GetGraph(level, lines, flags));
            if (updateMaxX < len)
                updateMaxX = len;
            return false;
        });
        SetLimit(updateMaxX, updateCount);
        AdjustFocus(Foc);
    }
}
