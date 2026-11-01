using System;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Menus;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Editors.EditorCommands;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Editors;

/// <summary>
/// A multi-line text editor over a gap buffer: the text before the cursor is at the start of
/// <see cref="Buffer"/>, the text after it at the end, with the gap in between. Supports selection,
/// one level of undo, auto indent, find and replace (through <see cref="EditorDialog"/>), WordStar keys
/// and an editor used as the clipboard.
/// </summary>
/// <remarks>
/// Palette layout: 1 = normal text, 2 = selected text. The buffer holds UTF-16 code units, so positions
/// count code units (upstream: UTF-8 bytes), and single-byte mode (<c>cmEncoding</c>) steps by code unit.
/// </remarks>
/// Ported from source/tvision/teditor1.cpp, source/tvision/teditor2.cpp, source/tvision/edits.cpp,
/// source/tvision/editstat.cpp, include/tvision/editors.h (class TEditor).
public class TEditor : TView
{
    /// <summary>How line breaks are written.</summary>
    public enum LineEndingType
    {
        /// <summary>CR LF.</summary>
        eolCrLf,
        /// <summary>LF.</summary>
        eolLf,
        /// <summary>CR.</summary>
        eolCr,
    }

    /// <summary>How the buffer is split into characters.</summary>
    public enum EncodingType
    {
        /// <summary>Whole characters (surrogate pairs are one character).</summary>
        encDefault,
        /// <summary>Single code units.</summary>
        encSingleByte,
    }

    // The largest character, in code units.
    private const int MaxCharSize = 2;

    private const LineEndingType DefaultLineEndingType = LineEndingType.eolCrLf;

    private static readonly TPalette _palette = new([0x06, 0x07]);

    private static readonly ushort[] _firstKeys =
    [
        41,
        kbCtrlA, cmSelectAll,
        kbCtrlC, cmPageDown,
        kbCtrlD, cmCharRight,
        kbCtrlE, cmLineUp,
        kbCtrlF, cmWordRight,
        kbCtrlG, cmDelChar,
        kbCtrlH, cmBackSpace,
        kbCtrlK, 0xFF02,
        kbCtrlL, cmSearchAgain,
        kbCtrlM, cmNewLine,
        kbCtrlO, cmIndentMode,
        kbCtrlP, cmEncoding,
        kbCtrlQ, 0xFF01,
        kbCtrlR, cmPageUp,
        kbCtrlS, cmCharLeft,
        kbCtrlT, cmDelWord,
        kbCtrlU, cmUndo,
        kbCtrlV, cmInsMode,
        kbCtrlX, cmLineDown,
        kbCtrlY, cmDelLine,
        kbLeft, cmCharLeft,
        kbRight, cmCharRight,
        kbAltBack, cmDelWordLeft,
        kbCtrlBack, cmDelWordLeft,
        kbCtrlDel, cmDelWord,
        kbCtrlLeft, cmWordLeft,
        kbCtrlRight, cmWordRight,
        kbHome, cmLineStart,
        kbEnd, cmLineEnd,
        kbUp, cmLineUp,
        kbDown, cmLineDown,
        kbPgUp, cmPageUp,
        kbPgDn, cmPageDown,
        kbCtrlHome, cmTextStart,
        kbCtrlEnd, cmTextEnd,
        kbIns, cmInsMode,
        kbDel, cmDelChar,
        kbShiftIns, cmPaste,
        kbShiftDel, cmCut,
        kbCtrlIns, cmCopy,
        // UPSTREAM: unreachable, since kbCtrlDel already maps to cmDelWord.
        kbCtrlDel, cmClear,
    ];

    private static readonly ushort[] _quickKeys =
    [
        8,
        'A', cmReplace,
        'C', cmTextEnd,
        'D', cmLineEnd,
        'F', cmFind,
        'H', cmDelStart,
        'R', cmTextStart,
        'S', cmLineStart,
        'Y', cmDelEnd,
    ];

    private static readonly ushort[] _blockKeys =
    [
        5,
        'B', cmStartSelect,
        'C', cmPaste,
        'H', cmHideSelect,
        'K', cmCopy,
        'Y', cmCut,
    ];

    private static readonly ushort[][] _keyMap = [_firstKeys, _quickKeys, _blockKeys];

    /// <summary>The horizontal scroll bar, or null.</summary>
    public TScrollBar? HScrollBar;
    /// <summary>The vertical scroll bar, or null.</summary>
    public TScrollBar? VScrollBar;
    /// <summary>The position indicator, or null.</summary>
    public TIndicator? Indicator;
    /// <summary>The gap buffer.</summary>
    public char[] Buffer = [];
    /// <summary>The size of <see cref="Buffer"/>.</summary>
    public int BufSize;
    /// <summary>The length of the text.</summary>
    public int BufLen;
    /// <summary>The length of the gap.</summary>
    public int GapLen;
    /// <summary>The start of the selection.</summary>
    public int SelStart;
    /// <summary>The end of the selection.</summary>
    public int SelEnd;
    /// <summary>The cursor offset, which is also where the gap starts.</summary>
    public int CurPtr;
    /// <summary>The cursor's line and column.</summary>
    public TPoint CurPos;
    /// <summary>The first visible column and line.</summary>
    public TPoint Delta;
    /// <summary>The scroll limits: the line length limit and the number of lines.</summary>
    public TPoint Limit;
    /// <summary>The line of <see cref="DrawPtr"/>.</summary>
    public int DrawLine;
    /// <summary>The start of a line close to the visible area, to start drawing from.</summary>
    public int DrawPtr;
    /// <summary>The length of the deleted text kept at the end of the gap for undo.</summary>
    public int DelCount;
    /// <summary>The length of the text inserted before the cursor since the last move, for undo.</summary>
    public int InsCount;
    /// <summary>Whether the buffer was allocated.</summary>
    public bool IsValid;
    /// <summary>Whether edits can be undone.</summary>
    public bool CanUndo;
    /// <summary>Whether the text changed.</summary>
    public bool Modified;
    /// <summary>Whether cursor movement extends the selection (after <c>cmStartSelect</c>).</summary>
    public bool Selecting;
    /// <summary>Whether typing overwrites.</summary>
    public bool Overwrite;
    /// <summary>Whether new lines copy the indentation of the current one.</summary>
    public bool AutoIndent;

    /// <summary>Shows the editor dialogs. By default, every dialog is cancelled.</summary>
    public static TEditorDialog EditorDialog = DefEditorDialog;
    /// <summary>The <c>efXxx</c> options of the last find or replace.</summary>
    public static ushort EditorFlags = efBackupFiles | efPromptOnReplace;
    /// <summary>The last text searched for.</summary>
    public static string FindStr = "";
    /// <summary>The last replacement.</summary>
    public static string ReplaceStr = "";
    /// <summary>The editor used as the clipboard, or null.</summary>
    public static TEditor? Clipboard;

    /// <summary>The nesting level of <see cref="Lock"/>; updates wait until it drops to 0.</summary>
    public byte LockCount;
    /// <summary>The pending <c>ufXxx</c> updates.</summary>
    public byte UpdateFlags;
    /// <summary>The key map for the next key: 0 normally, 1 after Ctrl-Q, 2 after Ctrl-K.</summary>
    public int KeyState;
    /// <summary>How line breaks are written, detected from the text.</summary>
    public LineEndingType LineEnding;
    /// <summary>How the buffer is split into characters.</summary>
    public EncodingType Encoding;

    /// <summary>
    /// Creates an editor with a buffer of <paramref name="aBufSize"/> code units, linked to the scroll bars and
    /// the indicator (each optional).
    /// </summary>
    public TEditor(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar, TIndicator? aIndicator, int aBufSize)
        : base(bounds)
    {
        HScrollBar = aHScrollBar;
        VScrollBar = aVScrollBar;
        Indicator = aIndicator;
        BufSize = aBufSize;
        CanUndo = true;
        AutoIndent = true;
        LineEnding = DefaultLineEndingType;
        GrowMode = gfGrowHiX | gfGrowHiY;
        Options |= ofSelectable;
        EventMask = evMouseDown | evKeyDown | evCommand | evBroadcast;
        ShowCursor();
        InitBuffer();
        // Allocation failure throws in .NET, so upstream's out-of-memory branch is gone.
        IsValid = true;
        SetBufLen(0);
    }

    /// <summary>The default <see cref="EditorDialog"/>: returns <c>cmCancel</c>.</summary>
    public static ushort DefEditorDialog(int dialog, params object?[] args) => cmCancel;

    /// <summary>Frees the buffer.</summary>
    public override void ShutDown()
    {
        DoneBuffer();
        base.ShutDown();
    }

    /// <summary>The code unit at text offset <paramref name="p"/>.</summary>
    public char BufChar(int p) => Buffer[BufPtr(p)];

    /// <summary>The buffer index of text offset <paramref name="p"/>, skipping the gap.</summary>
    public int BufPtr(int p) => p < CurPtr ? p : p + GapLen;

    /// <summary>Resizes, keeping the scroll position within the limits.</summary>
    public override void ChangeBounds(TRect bounds)
    {
        SetBounds(bounds);
        Delta.X = Math.Max(0, Math.Min(Delta.X, Limit.X - Size.X));
        Delta.Y = Math.Max(0, Math.Min(Delta.Y, Limit.Y - Size.Y));
        Update(ufView);
    }

    /// <summary>Copies the text from offset <paramref name="p"/> into <paramref name="dest"/>; returns the count copied.</summary>
    public int GetText(int p, Span<char> dest)
    {
        if (p < BufLen)
        {
            var count = Math.Min(dest.Length, BufLen - p);
            for (var i = 0; i < count; ++i)
                dest[i] = BufChar(p + i);
            return count;
        }
        return 0;
    }

    /// <summary>Moves <paramref name="p"/> past one character and <paramref name="pos"/> past its columns; false at the end.</summary>
    public bool NextCharAndPos(ref int p, ref int pos)
    {
        if (p < BufLen)
        {
            if (Encoding == EncodingType.encSingleByte)
            {
                ++p;
                ++pos;
            }
            else
            {
                Span<char> buf = stackalloc char[MaxCharSize];
                var count = GetText(p, buf);
                if (buf[0] == '\t')
                {
                    ++p;
                    pos = (pos | 7) + 1;
                }
                else
                {
                    int i = 0, w = 0;
                    TText.Next(buf[..count], ref i, ref w);
                    p += i;
                    pos += w;
                }
            }
            return true;
        }
        return false;
    }

    /// <summary>The column of <paramref name="target"/> counted from the line start <paramref name="p"/>.</summary>
    public int CharPos(int p, int target)
    {
        var pos = 0;
        while (p < target)
            if (!NextCharAndPos(ref p, ref pos))
                break;
        return pos;
    }

    /// <summary>The offset of column <paramref name="target"/> in the line starting at <paramref name="p"/>, stopping at the line end.</summary>
    public int CharPtr(int p, int target)
    {
        var pos = 0;
        var prevP = p;
        while (p < BufLen && pos < target)
        {
            var c = BufChar(p);
            if (c == '\r' || c == '\n')
                break;
            prevP = p;
            if (!NextCharAndPos(ref p, ref pos))
                break;
        }
        if (pos > target)
            p = prevP;
        return p;
    }

    /// <summary>Copies the selection into the clipboard editor.</summary>
    public bool ClipCopy()
    {
        var res = false;
        if (Clipboard != this)
        {
            // shortcut: without a clipboard editor nothing is copied (upstream uses the system clipboard,
            // TClipboard); port TClipboard to support it.
            if (Clipboard is not null)
                res = Clipboard.InsertFrom(this);
            Selecting = false;
            Update(ufUpdate);
        }
        return res;
    }

    /// <summary>Copies the selection into the clipboard, then deletes it.</summary>
    public void ClipCut()
    {
        if (ClipCopy())
            DeleteSelect();
    }

    /// <summary>Inserts the clipboard's selection.</summary>
    public void ClipPaste()
    {
        // shortcut: without a clipboard editor nothing is pasted (upstream requests the system clipboard).
        if (Clipboard != this && Clipboard is not null)
            InsertFrom(Clipboard);
    }

    /// <summary>Turns a key press into an editor command through the key maps, handling the Ctrl-Q and Ctrl-K prefixes.</summary>
    public virtual void ConvertEvent(ref TEvent ev)
    {
        if (ev.What == evKeyDown)
        {
            if ((ev.KeyDown.ControlKeyState & kbShift) != 0 &&
                ev.KeyDown.CharScan.ScanCode >= 0x47 &&
                ev.KeyDown.CharScan.ScanCode <= 0x51)
                ev.KeyDown.CharScan.CharCode = 0;

            var key = ev.KeyDown.KeyCode;
            if (KeyState != 0)
            {
                if ((key & 0xFF) >= 0x01 && (key & 0xFF) <= 0x1A)
                    key += 0x40;
                if ((key & 0xFF) >= 0x61 && (key & 0xFF) <= 0x7A)
                    key -= 0x20;
            }
            key = ScanKeyMap(_keyMap[KeyState], key);
            KeyState = 0;
            if (key != 0)
            {
                if ((key & 0xFF00) == 0xFF00)
                {
                    KeyState = key & 0xFF;
                    ClearEvent(ref ev);
                }
                else
                {
                    ev.What = evCommand;
                    ev.Message.Command = key;
                }
            }
        }
    }

    private static ushort ScanKeyMap(ushort[] keyMap, ushort keyCode)
    {
        var codeLow = (byte)(keyCode & 0xFF);
        var codeHi = (byte)(keyCode >> 8);
        for (var n = 1; n < 1 + 2 * keyMap[0]; n += 2)
        {
            var mapLow = (byte)(keyMap[n] & 0xFF);
            var mapHi = (byte)(keyMap[n] >> 8);
            if (mapLow == codeLow && (mapHi == 0 || mapHi == codeHi))
                return keyMap[n + 1];
        }
        return 0;
    }

    /// <summary>Whether the cursor's line is visible.</summary>
    public bool CursorVisible() => CurPos.Y >= Delta.Y && CurPos.Y < Delta.Y + Size.Y;

    /// <summary>
    /// Deletes from <paramref name="startPtr"/> to <paramref name="endPtr"/> (around the cursor), or the
    /// selection if there is one and <paramref name="delSelect"/> is set.
    /// </summary>
    public void DeleteRange(int startPtr, int endPtr, bool delSelect)
    {
        if (HasSelection() && delSelect)
            DeleteSelect();
        else
        {
            SetSelect(CurPtr, endPtr, true);
            DeleteSelect();
            SetSelect(startPtr, CurPtr, false);
            DeleteSelect();
        }
    }

    /// <summary>Deletes the selection.</summary>
    public void DeleteSelect() => InsertText([], false);

    /// <summary>Frees the buffer.</summary>
    public virtual void DoneBuffer() => Buffer = [];

    /// <summary>Finds <see cref="FindStr"/> and, with <c>efDoReplace</c>, replaces it, repeating with <c>efReplaceAll</c>.</summary>
    public void DoSearchReplace()
    {
        ushort i;
        do
        {
            i = cmCancel;
            if (!Search(FindStr, EditorFlags))
            {
                if ((EditorFlags & (efReplaceAll | efDoReplace)) != (efReplaceAll | efDoReplace))
                    EditorDialog(edSearchFailed);
            }
            else if ((EditorFlags & efDoReplace) != 0)
            {
                i = cmYes;
                if ((EditorFlags & efPromptOnReplace) != 0)
                {
                    var c = MakeGlobal(Cursor);
                    i = EditorDialog(edReplacePrompt, c);
                }
                if (i == cmYes)
                {
                    Lock();
                    InsertText(ReplaceStr, false);
                    TrackCursor(false);
                    Unlock();
                }
            }
        } while (i != cmCancel && (EditorFlags & efReplaceAll) != 0);
    }

    /// <summary>Applies the pending updates: cursor, redraw, scroll bars, indicator and commands.</summary>
    public void DoUpdate()
    {
        if (UpdateFlags != 0)
        {
            SetCursor(CurPos.X - Delta.X, CurPos.Y - Delta.Y);
            if ((UpdateFlags & ufView) != 0)
                DrawView();
            else if ((UpdateFlags & ufLine) != 0)
                DrawLines(CurPos.Y - Delta.Y, 1, LineStart(CurPtr));
            HScrollBar?.SetParams(Delta.X, 0, Limit.X - Size.X, Size.X / 2, 1);
            VScrollBar?.SetParams(Delta.Y, 0, Limit.Y - Size.Y, Size.Y - 1, 1);
            Indicator?.SetValue(CurPos, Modified);
            if ((State & sfActive) != 0)
                UpdateCommands();
            UpdateFlags = 0;
        }
    }

    /// <summary>Draws the visible lines.</summary>
    public override void Draw()
    {
        if (DrawLine != Delta.Y)
        {
            DrawPtr = LineMove(DrawPtr, Delta.Y - DrawLine);
            DrawLine = Delta.Y;
        }
        DrawLines(0, Size.Y, DrawPtr);
    }

    /// <summary>Draws <paramref name="count"/> lines from view row <paramref name="y"/>, starting with the line at <paramref name="linePtr"/>.</summary>
    public void DrawLines(int y, int count, int linePtr)
    {
        var b = new TDrawBuffer();
        var color = GetColor(0x0201);
        while (count-- > 0)
        {
            FormatLine(b, linePtr, Delta.X, Size.X, color);
            WriteBuf(0, y, Size.X, 1, b);
            linePtr = NextLine(linePtr);
            y++;
        }
    }

    /// <summary>Asks for the text to find, then searches.</summary>
    public void Find()
    {
        var findRec = new TFindDialogRec(FindStr, EditorFlags);
        if (EditorDialog(edFind, findRec) != cmCancel)
        {
            FindStr = findRec.Find;
            EditorFlags = (ushort)(findRec.Options & ~efDoReplace);
            DoSearchReplace();
        }
    }

    /// <summary>The text offset under the screen position <paramref name="m"/>, clamped to the view.</summary>
    public int GetMousePtr(TPoint m)
    {
        var mouse = MakeLocal(m);
        mouse.X = Math.Max(0, Math.Min(mouse.X, Size.X - 1));
        mouse.Y = Math.Max(0, Math.Min(mouse.Y, Size.Y - 1));
        return CharPtr(LineMove(DrawPtr, mouse.Y + Delta.Y - DrawLine), mouse.X + Delta.X);
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    /// <summary>Follows the scroll bar <paramref name="p"/> into <paramref name="d"/> if the event comes from it.</summary>
    public void CheckScrollBar(in TEvent ev, TScrollBar? p, ref int d)
    {
        if (ev.Message.InfoPtr == p && p!.Value != d)
        {
            d = p.Value;
            Update(ufView);
        }
    }

    /// <summary>Handles typing, editor commands, mouse selection, the context menu and scroll bar changes.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        var centerCursor = !CursorVisible();
        byte selectMode = 0;

        if (Selecting ||
            ((ev.What & evMouse) != 0 && (ev.Mouse.ControlKeyState & kbShift) != 0) ||
            ((ev.What & evKeyboard) != 0 && (ev.KeyDown.ControlKeyState & kbShift) != 0))
            selectMode = smExtend;

        ConvertEvent(ref ev);

        switch (ev.What)
        {
            case evMouseDown:
                if ((ev.Mouse.Buttons & mbRightButton) != 0)
                {
                    var menu = InitContextMenu(ev.Mouse.Where);
                    TMenuPopup.PopupMenu(ev.Mouse.Where, menu, Owner);
                    break;
                }

                if ((ev.Mouse.Buttons & mbMiddleButton) != 0)
                {
                    var lastMouse = MakeLocal(ev.Mouse.Where);
                    while (MouseEvent(ref ev, evMouse))
                    {
                        var mouse = MakeLocal(ev.Mouse.Where);
                        var d = Delta + (lastMouse - mouse);
                        ScrollTo(d.X, d.Y);
                        lastMouse = mouse;
                    }
                    break;
                }

                if ((ev.Mouse.EventFlags & meDoubleClick) != 0)
                    selectMode |= smDouble;
                else if ((ev.Mouse.EventFlags & meTripleClick) != 0)
                    selectMode |= smTriple;

                do
                {
                    Lock();
                    if (ev.What == evMouseAuto)
                    {
                        var mouse = MakeLocal(ev.Mouse.Where);
                        var d = Delta;
                        if (mouse.X < 0)
                            d.X--;
                        if (mouse.X >= Size.X)
                            d.X++;
                        if (mouse.Y < 0)
                            d.Y--;
                        if (mouse.Y >= Size.Y)
                            d.Y++;
                        ScrollTo(d.X, d.Y);
                    }
                    else if (ev.What == evMouseWheel)
                    {
                        // UPSTREAM: dereferences the scroll bars without checking them.
                        var copy = ev;
                        VScrollBar?.HandleEvent(ref copy);
                        HScrollBar?.HandleEvent(ref copy);
                    }
                    SetCurPtr(GetMousePtr(ev.Mouse.Where), selectMode);
                    selectMode |= smExtend;
                    Unlock();
                } while (MouseEvent(ref ev, evMouseMove | evMouseAuto | evMouseWheel));
                break;

            case evKeyDown:
                var keyText = ev.KeyDown.GetText();
                var charCode = ev.KeyDown.CharScan.CharCode;
                if ((Encoding != EncodingType.encSingleByte && keyText.Length > 0) ||
                    charCode == 9 ||
                    (charCode >= 32 && charCode < 255))
                {
                    Lock();
                    if ((ev.KeyDown.ControlKeyState & kbPaste) != 0)
                    {
                        Span<char> buf = stackalloc char[512];
                        while (TextEvent(ref ev, buf, out var length))
                            InsertText(buf[..length], false);
                    }
                    else
                    {
                        if (Overwrite && !HasSelection())
                            if (CurPtr != LineEnd(CurPtr))
                                SelEnd = NextChar(CurPtr);

                        if (Encoding != EncodingType.encSingleByte && keyText.Length > 0)
                            InsertText(keyText, false);
                        else
                            InsertText(charCode < 0x80 ? ((char)charCode).ToString() : TText.FromCodePage(charCode), false);
                    }

                    TrackCursor(centerCursor);
                    Unlock();
                }
                else
                    return;
                break;

            case evCommand:
                switch (ev.Message.Command)
                {
                    case cmFind:
                        Find();
                        break;
                    case cmReplace:
                        Replace();
                        break;
                    case cmSearchAgain:
                        DoSearchReplace();
                        break;
                    case cmEncoding:
                        ToggleEncoding();
                        break;
                    default:
                        Lock();
                        switch (ev.Message.Command)
                        {
                            case cmCut:
                                ClipCut();
                                break;
                            case cmCopy:
                                ClipCopy();
                                break;
                            case cmPaste:
                                ClipPaste();
                                break;
                            case cmUndo:
                                Undo();
                                break;
                            case cmClear:
                                DeleteSelect();
                                break;
                            case cmCharLeft:
                                SetCurPtr(PrevChar(CurPtr), selectMode);
                                break;
                            case cmCharRight:
                                SetCurPtr(NextChar(CurPtr), selectMode);
                                break;
                            case cmWordLeft:
                                SetCurPtr(PrevWord(CurPtr), selectMode);
                                break;
                            case cmWordRight:
                                SetCurPtr(NextWord(CurPtr), selectMode);
                                break;
                            case cmLineStart:
                                SetCurPtr(AutoIndent ? IndentedLineStart(CurPtr) : LineStart(CurPtr), selectMode);
                                break;
                            case cmLineEnd:
                                SetCurPtr(LineEnd(CurPtr), selectMode);
                                break;
                            case cmLineUp:
                                SetCurPtr(LineMove(CurPtr, -1), selectMode);
                                break;
                            case cmLineDown:
                                SetCurPtr(LineMove(CurPtr, 1), selectMode);
                                break;
                            case cmPageUp:
                                SetCurPtr(LineMove(CurPtr, -(Size.Y - 1)), selectMode);
                                break;
                            case cmPageDown:
                                SetCurPtr(LineMove(CurPtr, Size.Y - 1), selectMode);
                                break;
                            case cmTextStart:
                                SetCurPtr(0, selectMode);
                                break;
                            case cmTextEnd:
                                SetCurPtr(BufLen, selectMode);
                                break;
                            case cmNewLine:
                                NewLine();
                                break;
                            case cmBackSpace:
                                DeleteRange(PrevChar(CurPtr), CurPtr, true);
                                break;
                            case cmDelChar:
                                DeleteRange(CurPtr, NextChar(CurPtr), true);
                                break;
                            case cmDelWord:
                                DeleteRange(CurPtr, NextWord(CurPtr), false);
                                break;
                            case cmDelWordLeft:
                                DeleteRange(PrevWord(CurPtr), CurPtr, false);
                                break;
                            case cmDelStart:
                                DeleteRange(LineStart(CurPtr), CurPtr, false);
                                break;
                            case cmDelEnd:
                                DeleteRange(CurPtr, LineEnd(CurPtr), false);
                                break;
                            case cmDelLine:
                                DeleteRange(LineStart(CurPtr), NextLine(CurPtr), false);
                                break;
                            case cmInsMode:
                                ToggleInsMode();
                                break;
                            case cmStartSelect:
                                StartSelect();
                                break;
                            case cmHideSelect:
                                HideSelect();
                                break;
                            case cmIndentMode:
                                AutoIndent = !AutoIndent;
                                break;
                            case cmSelectAll:
                                SetCurPtr(0, selectMode);
                                selectMode |= smExtend;
                                SetCurPtr(BufLen, selectMode);
                                break;
                            default:
                                Unlock();
                                return;
                        }
                        TrackCursor(centerCursor);
                        Unlock();
                        break;
                }
                break;

            case evBroadcast:
                switch (ev.Message.Command)
                {
                    case cmScrollBarChanged:
                        if (ev.Message.InfoPtr == HScrollBar || ev.Message.InfoPtr == VScrollBar)
                        {
                            CheckScrollBar(ev, HScrollBar, ref Delta.X);
                            CheckScrollBar(ev, VScrollBar, ref Delta.Y);
                        }
                        else
                            return;
                        break;
                    default:
                        return;
                }
                break;
        }
        ClearEvent(ref ev);
    }

    // The category of a code unit for word movement: blank, line break, punctuation or word.
    private static int GetCharCategory(char ch)
    {
        if (ch == ' ' || ch == '\t' || ch == '\0')
            return 0;
        if (ch == '\n' || ch == '\r')
            return 1;
        // ispunct() on a byte: ASCII punctuation only.
        if (ch != '_' && char.IsAscii(ch) && char.IsPunctuation(ch) | char.IsSymbol(ch))
            return 2;
        return 3;
    }

    private static bool IsWordBoundary(char a, char b) => GetCharCategory(a) != GetCharCategory(b);

    private static bool IsWhitespaceOrNewline(char ch) => GetCharCategory(ch) <= 1;

    private static bool IsWordChar(char ch) => GetCharCategory(ch) == 3;

    private LineEndingType DetectLineEndingType()
    {
        // Detect the line ending type based on the first line break.
        for (var p = 0; p < BufLen; ++p)
        {
            if (BufChar(p) == '\r')
            {
                if (p + 1 < BufLen && BufChar(p + 1) == '\n')
                    return LineEndingType.eolCrLf;
                return LineEndingType.eolCr;
            }
            else if (BufChar(p) == '\n')
                return LineEndingType.eolLf;
        }
        return DefaultLineEndingType;
    }

    /// <summary>Whether text is selected.</summary>
    public bool HasSelection() => SelStart != SelEnd;

    /// <summary>Clears the selection and stops selecting.</summary>
    public void HideSelect()
    {
        Selecting = false;
        SetSelect(CurPtr, CurPtr, false);
    }

    /// <summary>Allocates <see cref="Buffer"/> with <see cref="BufSize"/> code units.</summary>
    public virtual void InitBuffer() => Buffer = new char[BufSize];

    /// <summary>The right-click menu shown near the global position <paramref name="p"/>: Cut, Copy, Paste and Undo.</summary>
    public virtual TMenuItem InitContextMenu(TPoint p) =>
        new TMenuItem("Cu~t~", cmCut, kbShiftDel, hcNoContext, "Shift-Del") +
        new TMenuItem("~C~opy", cmCopy, kbCtrlIns, hcNoContext, "Ctrl-Ins") +
        new TMenuItem("~P~aste", cmPaste, kbShiftIns, hcNoContext, "Shift-Ins") +
        new TMenuItem("~U~ndo", cmUndo, kbCtrlU, hcNoContext, "Ctrl-U");

    private string GetLineEnding() => LineEnding switch
    {
        LineEndingType.eolLf => "\n",
        LineEndingType.eolCr => "\r",
        _ => "\r\n",
    };

    private int LengthWithConvertedLineEndings(ReadOnlySpan<char> p)
    {
        var lineEndingLength = GetLineEnding().Length;
        var newLength = 0;
        for (var i = 0; i < p.Length; ++i)
        {
            if (p[i] == '\n' || p[i] == '\r')
            {
                newLength += lineEndingLength;
                if (p[i] == '\r' && i + 1 < p.Length && p[i + 1] == '\n')
                    ++i; // Skip LF after CR.
            }
            else
                ++newLength;
        }
        return newLength;
    }

    // Copies forwards, so src may overlap dest when it lies after it.
    private void CopyAndConvertLineEndings(int dest, ReadOnlySpan<char> src)
    {
        var lineEnding = GetLineEnding();
        for (var i = 0; i < src.Length; ++i)
        {
            var c = src[i];
            if (c == '\n' || c == '\r')
            {
                Buffer[dest++] = lineEnding[0];
                if (lineEnding.Length > 1)
                    Buffer[dest++] = lineEnding[1];
                if (c == '\r' && i < src.Length - 1 && src[i + 1] == '\n')
                    ++i; // Skip LF after CR.
            }
            else
                Buffer[dest++] = c;
        }
    }

    /// <summary>
    /// Replaces the selection with <paramref name="length"/> code units of <paramref name="p"/> from
    /// <paramref name="offset"/>, converting line breaks to <see cref="LineEnding"/>. Keeps the deleted text for
    /// undo if <paramref name="allowUndo"/>; selects the inserted text if <paramref name="selectText"/>.
    /// </summary>
    public bool InsertBuffer(ReadOnlySpan<char> p, int offset, int length, bool allowUndo, bool selectText)
    {
        Selecting = false;
        var selLen = SelEnd - SelStart;
        if (selLen == 0 && length == 0)
            return true;

        var delLen = 0;
        if (allowUndo)
        {
            if (CurPtr == SelStart)
                delLen = selLen;
            else if (selLen > InsCount)
                delLen = selLen - InsCount;
        }

        var text = p.Slice(offset, length);
        var insLen = LengthWithConvertedLineEndings(text);
        var newSize = (long)BufLen + DelCount - selLen + delLen + insLen;

        // A text inside the buffer stays readable: the span keeps the old array if the buffer is reallocated.
        if (newSize > BufLen + DelCount)
        {
            if (newSize > int.MaxValue - 0x1F || !SetBufSize((int)newSize))
            {
                EditorDialog(edOutOfMemory);
                SelEnd = SelStart;
                return false;
            }
        }

        var selLines = CountLines(Buffer.AsSpan(BufPtr(SelStart), selLen));
        if (CurPtr == SelEnd)
        {
            if (allowUndo)
            {
                if (delLen > 0)
                    Array.Copy(Buffer, SelStart, Buffer, CurPtr + GapLen - DelCount - delLen, delLen);
                InsCount -= selLen - delLen;
            }
            CurPtr = SelStart;
            CurPos.Y -= selLines;
        }
        if (Delta.Y > CurPos.Y)
        {
            Delta.Y -= selLines;
            if (Delta.Y < CurPos.Y)
                Delta.Y = CurPos.Y;
        }

        if (length > 0)
            CopyAndConvertLineEndings(CurPtr, text);

        var lines = CountLines(Buffer.AsSpan(CurPtr, insLen));
        CurPtr += insLen;
        BufLen += insLen - selLen;
        GapLen -= insLen - selLen;
        CurPos.Y += lines;
        DrawLine = CurPos.Y;
        DrawPtr = LineStart(CurPtr);
        CurPos.X = CharPos(DrawPtr, CurPtr);
        if (!selectText)
            SelStart = CurPtr;
        SelEnd = CurPtr;
        if (allowUndo)
        {
            DelCount += delLen;
            InsCount += insLen;
        }
        Limit.Y += lines - selLines;
        Delta.Y = Math.Max(0, Math.Min(Delta.Y, Limit.Y - Size.Y));
        if (!IsClipboard())
            Modified = true;
        SetBufSize(BufLen + DelCount);
        if (selLines == 0 && lines == 0)
            Update(ufLine);
        else
            Update(ufView);
        return true;
    }

    /// <summary>Replaces the selection with the selection of <paramref name="editor"/>.</summary>
    public virtual bool InsertFrom(TEditor editor) =>
        InsertBuffer(editor.Buffer, editor.BufPtr(editor.SelStart), editor.SelEnd - editor.SelStart, CanUndo, IsClipboard());

    /// <summary>Replaces the selection with <paramref name="text"/>, selecting it if <paramref name="selectText"/>.</summary>
    public bool InsertText(ReadOnlySpan<char> text, bool selectText) =>
        InsertBuffer(text, 0, text.Length, CanUndo, selectText);

    /// <summary>Whether this editor is the clipboard.</summary>
    public bool IsClipboard() => Clipboard == this;

    /// <summary>The offset <paramref name="count"/> lines away from <paramref name="p"/>, keeping the column if possible.</summary>
    public int LineMove(int p, int count)
    {
        var i = p;
        p = LineStart(p);
        var pos = CharPos(p, i);
        while (count != 0)
        {
            i = p;
            if (count < 0)
            {
                p = PrevLine(p);
                count++;
            }
            else
            {
                p = NextLine(p);
                count--;
            }
        }
        if (p != i)
            p = CharPtr(p, pos);
        return p;
    }

    /// <summary>Defers updates until the matching <see cref="Unlock"/>.</summary>
    public void Lock() => LockCount++;

    /// <summary>Inserts a line break and, with <see cref="AutoIndent"/>, the current line's indentation.</summary>
    public void NewLine()
    {
        var p = LineStart(CurPtr);
        var i = p;
        while (i < CurPtr && (Buffer[i] == ' ' || Buffer[i] == '\t'))
            i++;
        // Line break will be automatically converted to the right type.
        InsertText("\n", false);
        if (AutoIndent)
            InsertText(Buffer.AsSpan(p, i - p), false);
    }

    /// <summary>The start of the line after the one at <paramref name="p"/>.</summary>
    public int NextLine(int p) => NextChar(LineEnd(p));

    private int NextWordBoundary(int p)
    {
        if (p < BufLen)
        {
            var c = BufChar(p);
            do
            {
                p = NextChar(p);
            } while (p < BufLen && !IsWordBoundary(BufChar(p), c));
        }
        return p;
    }

    /// <summary>The start of the next word after <paramref name="p"/>, skipping blanks and line breaks.</summary>
    public int NextWord(int p)
    {
        p = NextWordBoundary(p);
        while (p < BufLen && IsWhitespaceOrNewline(BufChar(p)))
            p = NextWordBoundary(p);
        return p;
    }

    /// <summary>The start of the line before the one at <paramref name="p"/>.</summary>
    public int PrevLine(int p) => LineStart(PrevChar(p));

    private int PrevWordBoundary(int p)
    {
        if (p > 0)
        {
            p = PrevChar(p);
            var c = BufChar(p);
            while (p > 0)
            {
                var q = PrevChar(p);
                if (IsWordBoundary(BufChar(q), c))
                    break;
                p = q;
            }
        }
        return p;
    }

    /// <summary>The start of the word before <paramref name="p"/>, skipping blanks and line breaks.</summary>
    public int PrevWord(int p)
    {
        p = PrevWordBoundary(p);
        while (p > 0 && IsWhitespaceOrNewline(BufChar(p)))
            p = PrevWordBoundary(p);
        return p;
    }

    /// <summary>The first non-blank of the line at <paramref name="p"/>, or the line start if already there.</summary>
    public int IndentedLineStart(int p)
    {
        var startPtr = LineStart(p);
        var destPtr = startPtr;
        char c;
        while (destPtr < BufLen && ((c = BufChar(destPtr)) == ' ' || c == '\t'))
            ++destPtr;
        return destPtr == p ? startPtr : destPtr;
    }

    /// <summary>Asks for the text to find and its replacement, then replaces.</summary>
    public void Replace()
    {
        var replaceRec = new TReplaceDialogRec(FindStr, ReplaceStr, EditorFlags);
        if (EditorDialog(edReplace, replaceRec) != cmCancel)
        {
            FindStr = replaceRec.Find;
            ReplaceStr = replaceRec.Replace;
            EditorFlags = (ushort)(replaceRec.Options | efDoReplace);
            DoSearchReplace();
        }
    }

    /// <summary>Scrolls to column <paramref name="x"/> and line <paramref name="y"/>, within the limits.</summary>
    public void ScrollTo(int x, int y)
    {
        x = Math.Max(0, Math.Min(x, Limit.X - Size.X));
        y = Math.Max(0, Math.Min(y, Limit.Y - Size.Y));
        if (x != Delta.X || y != Delta.Y)
        {
            Delta.X = x;
            Delta.Y = y;
            Update(ufView);
        }
    }

    /// <summary>Selects the next occurrence of <paramref name="findStr"/> after the cursor, with the <c>efXxx</c> options <paramref name="opts"/>.</summary>
    public bool Search(string findStr, ushort opts)
    {
        var pos = CurPtr;
        int i;
        do
        {
            var block = Buffer.AsSpan(BufPtr(pos), BufLen - pos);
            if ((opts & efCaseSensitive) != 0)
                i = Scan(block, findStr);
            else
                i = IScan(block, findStr);

            if (i != sfSearchFailed)
            {
                i += pos;
                if ((opts & efWholeWordsOnly) == 0 ||
                    !((i != 0 && IsWordChar(BufChar(i - 1))) ||
                      (i + findStr.Length != BufLen && IsWordChar(BufChar(i + findStr.Length)))))
                {
                    Lock();
                    SetSelect(i, i + findStr.Length, false);
                    TrackCursor(!CursorVisible());
                    Unlock();
                    return true;
                }
                else
                    pos = i + 1;
            }
        } while (i != sfSearchFailed);
        return false;
    }

    /// <summary>Takes the <paramref name="length"/> code units at the end of the buffer as the text, with the cursor at the start.</summary>
    public void SetBufLen(int length)
    {
        BufLen = length;
        GapLen = BufSize - length;
        SelStart = 0;
        SelEnd = 0;
        CurPtr = 0;
        Delta.X = 0;
        Delta.Y = 0;
        CurPos = Delta;
        Limit.X = maxLineLength;
        Limit.Y = CountLines(Buffer.AsSpan(GapLen, BufLen)) + 1;
        DrawLine = 0;
        DrawPtr = 0;
        DelCount = 0;
        InsCount = 0;
        Modified = false;
        LineEnding = DetectLineEndingType();
        Update(ufView);
    }

    /// <summary>Makes room for <paramref name="newSize"/> code units; the base buffer cannot grow.</summary>
    public virtual bool SetBufSize(int newSize) => newSize <= BufSize;

    /// <summary>Enables <paramref name="command"/> if <paramref name="enable"/> and the editor is active, else disables it.</summary>
    public void SetCmdState(ushort command, bool enable)
    {
        var s = new TCommandSet();
        s += command;
        if (enable && (State & sfActive) != 0)
            EnableCommands(s);
        else
            DisableCommands(s);
    }

    /// <summary>Moves the cursor to <paramref name="p"/>, extending or snapping the selection by the <c>smXxx</c> <paramref name="selectMode"/>.</summary>
    public void SetCurPtr(int p, byte selectMode)
    {
        int anchor;
        if ((selectMode & smExtend) == 0)
            anchor = p;
        else if (CurPtr == SelStart)
            anchor = SelEnd;
        else
            anchor = SelStart;

        if (p < anchor)
        {
            if ((selectMode & smDouble) != 0)
            {
                p = PrevWordBoundary(NextWordBoundary(p));
                anchor = NextWordBoundary(PrevWordBoundary(anchor));
            }
            else if ((selectMode & smTriple) != 0)
            {
                p = PrevLine(NextLine(p));
                anchor = NextLine(PrevLine(anchor));
            }
            SetSelect(p, anchor, true);
        }
        else
        {
            if ((selectMode & smDouble) != 0)
            {
                p = NextWordBoundary(p);
                anchor = PrevWordBoundary(NextWordBoundary(anchor));
            }
            else if ((selectMode & smTriple) != 0)
            {
                p = NextLine(p);
                anchor = PrevLine(NextLine(anchor));
            }
            SetSelect(anchor, p, false);
        }
    }

    /// <summary>Selects from <paramref name="newStart"/> to <paramref name="newEnd"/>, moving the cursor (and gap) to the start if <paramref name="curStart"/>, else to the end.</summary>
    public void SetSelect(int newStart, int newEnd, bool curStart)
    {
        var p = curStart ? newStart : newEnd;

        var flags = ufUpdate;

        if (newStart != SelStart || newEnd != SelEnd)
            if (newStart != newEnd || SelStart != SelEnd)
                flags = ufView;

        if (p != CurPtr)
        {
            if (p > CurPtr)
            {
                var l = p - CurPtr;
                Array.Copy(Buffer, CurPtr + GapLen, Buffer, CurPtr, l);
                CurPos.Y += CountLines(Buffer.AsSpan(CurPtr, l));
                CurPtr = p;
            }
            else
            {
                var l = CurPtr - p;
                CurPtr = p;
                CurPos.Y -= CountLines(Buffer.AsSpan(CurPtr, l));
                Array.Copy(Buffer, CurPtr, Buffer, CurPtr + GapLen, l);
            }
            DelCount = 0;
            InsCount = 0;
            SetBufSize(BufLen);
        }
        DrawLine = CurPos.Y;
        DrawPtr = LineStart(p);
        CurPos.X = CharPos(DrawPtr, p);
        SelStart = newStart;
        SelEnd = newEnd;
        Update(flags);
    }

    /// <summary>Shows the scroll bars and indicator while active; applies deferred updates once exposed.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        switch (aState)
        {
            case sfActive:
                HScrollBar?.SetState(sfVisible, enable);
                VScrollBar?.SetState(sfVisible, enable);
                Indicator?.SetState(sfVisible, enable);
                UpdateCommands();
                break;

            case sfExposed:
                if (enable)
                    Unlock();
                break;
        }
    }

    /// <summary>Starts selecting with the cursor keys.</summary>
    public void StartSelect()
    {
        HideSelect();
        Selecting = true;
    }

    /// <summary>Switches between whole characters and single code units.</summary>
    public void ToggleEncoding()
    {
        Encoding = Encoding == EncodingType.encDefault ? EncodingType.encSingleByte : EncodingType.encDefault;
        UpdateFlags |= ufView;
        SetSelect(SelStart, SelEnd, CurPtr < SelEnd);
    }

    /// <summary>Switches between insert and overwrite mode, changing the cursor shape.</summary>
    public void ToggleInsMode()
    {
        Overwrite = !Overwrite;
        SetState(sfCursorIns, !GetState(sfCursorIns));
    }

    /// <summary>Scrolls so the cursor is visible, centering its line if <paramref name="center"/>.</summary>
    public void TrackCursor(bool center)
    {
        if (center)
            ScrollTo(CurPos.X - Size.X + 1, CurPos.Y - Size.Y / 2);
        else
            ScrollTo(Math.Max(CurPos.X - Size.X + 1, Math.Min(Delta.X, CurPos.X)),
                     Math.Max(CurPos.Y - Size.Y + 1, Math.Min(Delta.Y, CurPos.Y)));
    }

    /// <summary>Undoes the edits since the cursor last moved, selecting the restored text.</summary>
    public void Undo()
    {
        if (DelCount != 0 || InsCount != 0)
        {
            SelStart = CurPtr - InsCount;
            SelEnd = CurPtr;
            var length = DelCount;
            DelCount = 0;
            InsCount = 0;
            InsertBuffer(Buffer, CurPtr + GapLen - length, length, false, true);
        }
    }

    /// <summary>Ends a <see cref="Lock"/>; the last one applies the pending updates.</summary>
    public void Unlock()
    {
        if (LockCount > 0)
        {
            LockCount--;
            if (LockCount == 0)
                DoUpdate();
        }
    }

    /// <summary>Adds the <c>ufXxx</c> <paramref name="aFlags"/> and applies them unless locked.</summary>
    public void Update(byte aFlags)
    {
        UpdateFlags |= aFlags;
        if (LockCount == 0)
            DoUpdate();
    }

    /// <summary>Enables the undo, clipboard and search commands that apply.</summary>
    public virtual void UpdateCommands()
    {
        SetCmdState(cmUndo, DelCount != 0 || InsCount != 0);
        if (!IsClipboard())
        {
            SetCmdState(cmCut, HasSelection());
            SetCmdState(cmCopy, HasSelection());
            SetCmdState(cmPaste, Clipboard is null || Clipboard.HasSelection());
        }
        SetCmdState(cmClear, HasSelection());
        SetCmdState(cmFind, true);
        SetCmdState(cmReplace, true);
        SetCmdState(cmSearchAgain, true);
    }

    /// <summary>Whether the buffer was allocated.</summary>
    public override bool Valid(ushort command) => IsValid;

    private static TColorAttr GetColorAt(int p, TAttrPair colors, TEditor editor) =>
        editor.SelStart <= p && p < editor.SelEnd ? colors[1] : colors[0];

    /// <summary>
    /// Fills <paramref name="b"/> with <paramref name="width"/> columns of the line at <paramref name="linePtr"/>
    /// from column <paramref name="hScroll"/>, selected text in the second color of <paramref name="colors"/>.
    /// </summary>
    public void FormatLine(TDrawBuffer b, int linePtr, int hScroll, int width, TAttrPair colors)
    {
        hScroll = Math.Max(hScroll, 0);
        width = Math.Max(width, 0);

        var p = linePtr;
        var pos = 0;
        var x = 0;
        Span<char> buf = stackalloc char[MaxCharSize];
        while (p < BufLen)
        {
            var nextP = p;
            var nextPos = pos;
            NextCharAndPos(ref nextP, ref nextPos);

            // Only break when exceeding 'width', so that combining characters in
            // the last visible column can be drawn properly.
            if (x > width || (x == width && pos < nextPos))
                break;

            var charLen = nextP - p;
            GetText(p, buf[..charLen]);

            if (buf[0] == '\r' || buf[0] == '\n')
                break;

            // Only draw text beyond position 'hScroll'.
            if (nextPos > hScroll)
            {
                var color = GetColorAt(p, colors, this);
                // If pos < hScroll, we are dealing with an incomplete tabulator or
                // double-width character which we have to represent as spaces.
                var charWidth = nextPos - Math.Max(pos, hScroll);
                if (buf[0] == '\t' || pos < hScroll)
                    b.MoveChar(x, ' ', color, charWidth);
                else
                    b.MoveStr(x, buf[..charLen], color);

                x += charWidth;
            }

            p = nextP;
            pos = nextPos;
        }

        if (x < width)
        {
            var colorAfter = GetColorAt(p, colors, this);
            b.MoveChar(x, ' ', colorAfter, width - x);
        }
    }

    /// <summary>The end of the line at <paramref name="p"/>: its line break, or the end of the text.</summary>
    public int LineEnd(int p)
    {
        for (var i = p; i < BufLen; ++i)
        {
            var c = BufChar(i);
            if (c == '\r' || c == '\n')
                return i;
        }
        return BufLen;
    }

    /// <summary>The start of the line at <paramref name="p"/>.</summary>
    public int LineStart(int p)
    {
        var i = p;
        while (i-- > 0)
        {
            var c = BufChar(i);
            if (c == '\r')
            {
                if (i + 1 != CurPtr && i + 1 != BufLen && BufChar(i + 1) == '\n')
                    return i + 2;
                return i + 1;
            }
            else if (c == '\n')
                return i + 1;
        }
        return 0;
    }

    /// <summary>The offset after the character at <paramref name="p"/>; CR LF counts as one.</summary>
    public int NextChar(int p)
    {
        if (p + 1 < BufLen)
        {
            if (BufChar(p) == '\r' && BufChar(p + 1) == '\n')
                return p + 2;
            if (Encoding == EncodingType.encSingleByte)
                return p + 1;
            Span<char> buf = stackalloc char[MaxCharSize];
            var count = GetText(p, buf);
            return p + TText.Next(buf[..count]);
        }
        return BufLen;
    }

    /// <summary>The offset of the character before <paramref name="p"/>; CR LF counts as one.</summary>
    public int PrevChar(int p)
    {
        if (p > 1)
        {
            if (BufChar(p - 2) == '\r' && BufChar(p - 1) == '\n')
                return p - 2;
            if (Encoding == EncodingType.encSingleByte)
                return p - 1;
            Span<char> buf = stackalloc char[MaxCharSize];
            var count = Math.Min(MaxCharSize, p);
            GetText(p - count, buf[..count]);
            return p - TText.Prev(buf[..count], count);
        }
        return 0;
    }

    /// <summary>The number of line breaks in <paramref name="buf"/>; CR LF counts as one.</summary>
    public static int CountLines(ReadOnlySpan<char> buf)
    {
        var lines = 0;
        for (var i = 0; i < buf.Length; ++i)
        {
            if (buf[i] == '\r')
            {
                ++lines;
                if (i + 1 < buf.Length && buf[i + 1] == '\n')
                    ++i;
            }
            else if (buf[i] == '\n')
                ++lines;
        }
        return lines;
    }

    /// <summary>The offset of <paramref name="str"/> in <paramref name="block"/>, or <c>sfSearchFailed</c>.</summary>
    public static int Scan(ReadOnlySpan<char> block, string str)
    {
        if (str.Length == 0)
            return sfSearchFailed;
        var i = block.IndexOf(str, StringComparison.Ordinal);
        return i < 0 ? sfSearchFailed : i;
    }

    /// <summary>Like <see cref="Scan"/>, ignoring case.</summary>
    public static int IScan(ReadOnlySpan<char> block, string str)
    {
        if (str.Length == 0)
            return sfSearchFailed;
        var i = block.IndexOf(str, StringComparison.OrdinalIgnoreCase);
        return i < 0 ? sfSearchFailed : i;
    }
}
