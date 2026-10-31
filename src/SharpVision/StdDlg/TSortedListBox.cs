using System;
using System.Collections;

using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.StdDlg;

/// <summary>
/// A list box over sorted items with incremental search: typing jumps to the first item that starts
/// with the typed text (ignoring case), Backspace shortens the search, and '.' jumps to the dot.
/// </summary>
/// Ported from source/tvision/stddlg.cpp, source/tvision/tsortcol.cpp (search),
/// include/tvision/stddlg.h (class TSortedListBox).
/// <remarks>
/// Upstream's <c>TSortedCollection</c> becomes the item list plus <see cref="Compare"/>, which must
/// match the order of the items.
/// </remarks>
public class TSortedListBox : TListBox
{
    /// <summary>The shift state when the search started.</summary>
    protected byte ShiftState;

    private int _searchPos = -1;

    /// <summary>Creates an empty sorted list box with the cursor shown at the search position.</summary>
    public TSortedListBox(TRect bounds, ushort aNumCols, TScrollBar? aScrollBar)
        : base(bounds, aNumCols, aScrollBar)
    {
        ShowCursor();
        SetCursor(1, 0);
    }

    private static bool Equal(string s1, string s2, int count) =>
        string.Equals(s1[..Math.Min(count, s1.Length)], s2[..Math.Min(count, s2.Length)], StringComparison.OrdinalIgnoreCase);

    /// <summary>Handles the list keys and mouse, then the incremental search keys.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        var oldValue = Focused;
        base.HandleEvent(ref ev);
        if (oldValue != Focused || (ev.What == evBroadcast && ev.Message.Command == cmReleasedFocus))
            _searchPos = -1;
        if (ev.What == evKeyDown && ev.KeyDown.CharScan.CharCode != 0)
        {
            var value = Focused;
            var curString = value < Range ? GetText(value, 255) : "";
            var oldPos = _searchPos;
            if (ev.KeyDown.KeyCode == kbBack)
            {
                if (_searchPos == -1)
                    return;
                _searchPos--;
                if (_searchPos == -1)
                    ShiftState = (byte)ev.KeyDown.ControlKeyState;
                curString = curString[..Math.Min(_searchPos + 1, curString.Length)];
            }
            else if (ev.KeyDown.CharScan.CharCode == '.')
                _searchPos = curString.IndexOf('.', StringComparison.Ordinal);
            else
            {
                _searchPos++;
                if (_searchPos == 0)
                    ShiftState = (byte)ev.KeyDown.ControlKeyState;
                curString = curString[..Math.Min(_searchPos, curString.Length)].PadRight(_searchPos) +
                    (char)ev.KeyDown.CharScan.CharCode;
            }

            value = Search(GetKey(curString));
            if (value < Range)
            {
                var newString = GetText(value, 255);
                if (Equal(curString, newString, _searchPos + 1))
                {
                    if (value != oldValue)
                    {
                        FocusItem(value);
                        SetCursor(Cursor.X + _searchPos + 1, Cursor.Y);
                    }
                    else
                        SetCursor(Cursor.X + (_searchPos - oldPos), Cursor.Y);
                }
                else
                    _searchPos = oldPos;
            }
            else
                _searchPos = oldPos;

            if (_searchPos != oldPos || char.IsAsciiLetter((char)ev.KeyDown.CharScan.CharCode))
                ClearEvent(ref ev);
        }
    }

    /// <summary>The search key for the typed text. The text itself by default.</summary>
    protected virtual object GetKey(string s) => s;

    /// <summary>Orders an item against a search key, like the collection's <c>compare</c>. Ordinal on strings by default (<c>TStringCollection</c>).</summary>
    protected virtual int Compare(object item, object key) => string.CompareOrdinal((string)item, (string)key);

    /// <summary>Replaces the items, which must be sorted by <see cref="Compare"/>, and resets the search.</summary>
    public override void NewList(IList? aList)
    {
        base.NewList(aList);
        _searchPos = -1;
    }

    // The index of the first item not less than the key (TNSSortedCollection::search).
    private int Search(object key)
    {
        if (Items is null)
            return 0;
        int l = 0, h = Items.Count - 1;
        while (l <= h)
        {
            var i = (l + h) >> 1;
            var c = Compare(Items[i]!, key);
            if (c < 0)
                l = i + 1;
            else
            {
                h = i - 1;
                if (c == 0)
                    l = i;
            }
        }
        return l;
    }
}
