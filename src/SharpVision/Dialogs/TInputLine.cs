using System;

using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Validators;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Objects.Util;
using static SharpVision.Validators.ValidatorFlags;
using static SharpVision.Views.Commands;
using static SharpVision.Views.ViewFlags;

namespace SharpVision.Dialogs;

/// <summary>A single-line text editor with horizontal scrolling, selection and an optional validator.</summary>
/// <remarks>Palette layout: 1 = passive, 2 = active, 3 = selected, 4 = arrows.</remarks>
/// Ported from source/tvision/tinputli.cpp, include/tvision/dialogs.h (class TInputLine).
public class TInputLine : TView
{
    /// <summary>The limit counts code units (upstream: bytes, including the terminating null).</summary>
    public const ushort ilMaxBytes = 0;

    /// <summary>The limit counts display columns.</summary>
    public const ushort ilMaxWidth = 1;

    /// <summary>The limit counts characters (grapheme clusters).</summary>
    public const ushort ilMaxChars = 2;

    private const int CONTROL_Y = 25;
    private const char rightArrow = '►';
    private const char leftArrow = '◄';

    private static readonly TPalette _palette = new([0x13, 0x13, 0x14, 0x15]);

    // Home, Left Arrow, Right Arrow, End, Ctrl-Left Arrow, Ctrl-Right Arrow.
    private static readonly byte[] _padKeys = [0x47, 0x4b, 0x4d, 0x4f, 0x73, 0x74];

    /// <summary>The edited text.</summary>
    public string Data = "";

    /// <summary>The maximum length of <see cref="Data"/>, in UTF-16 code units.</summary>
    public int MaxLen;

    /// <summary>The maximum width of <see cref="Data"/>, in columns.</summary>
    public int MaxWidth;

    /// <summary>The maximum number of characters in <see cref="Data"/>.</summary>
    public int MaxChars;

    /// <summary>The cursor position, as a code unit index into <see cref="Data"/>.</summary>
    public int CurPos;

    /// <summary>The first displayed column of <see cref="Data"/>.</summary>
    public int FirstPos;

    /// <summary>The start of the selection (code unit index).</summary>
    public int SelStart;

    /// <summary>The end of the selection (code unit index).</summary>
    public int SelEnd;

    private TValidator? _validator;
    private int _anchor;
    private string _oldData = "";
    private int _oldCurPos;
    private int _oldFirstPos;
    private int _oldSelStart;
    private int _oldSelEnd;

    /// <summary>Creates an input line limited to <paramref name="limit"/> (interpreted per <paramref name="limitMode"/>).</summary>
    /// <remarks>
    /// With <see cref="ilMaxBytes"/> the text holds at most <c>limit - 1</c> UTF-16 code units,
    /// where upstream counts UTF-8 bytes.
    /// </remarks>
    public TInputLine(TRect bounds, int limit, TValidator? aValid = null, ushort limitMode = ilMaxBytes)
        : base(bounds)
    {
        MaxLen = limitMode == ilMaxBytes ? Math.Min(Math.Max(limit - 1, 0), int.MaxValue - 1) : 255;
        MaxWidth = limitMode == ilMaxWidth ? limit : int.MaxValue;
        MaxChars = limitMode == ilMaxChars ? limit : int.MaxValue;
        _validator = aValid;
        State |= sfCursorVis;
        Options |= (ushort)(ofSelectable | ofFirstClick);
    }

    private bool CanScroll(int delta)
    {
        if (delta < 0)
            return FirstPos > 0;
        if (delta > 0)
            return Strwidth(Data) - FirstPos + 2 > Size.X;
        return false;
    }

    /// <summary>The number of data slots: the validator's count, or 1 for the text.</summary>
    public override int DataSize()
    {
        ushort dSize = 0;
        if (_validator is not null)
        {
            object? dummy = null;
            dSize = _validator.Transfer(ref Data, ref dummy, TVTransfer.vtDataSize);
        }
        if (dSize == 0)
            dSize = 1;
        return dSize;
    }

    /// <summary>Draws the visible part of the text, the scroll arrows and the selection.</summary>
    public override void Draw()
    {
        int l, r;
        var b = new TDrawBuffer();

        TColorAttr color = GetColor((ushort)((State & sfFocused) != 0 ? 2 : 1));

        b.MoveChar(0, ' ', color, Size.X);
        if (Size.X > 1)
            b.MoveStr(1, Data, color, Size.X - 1, FirstPos);
        if (CanScroll(1))
            b.MoveChar(Size.X - 1, rightArrow, GetColor(4), 1);
        if (CanScroll(-1))
            b.MoveChar(0, leftArrow, GetColor(4), 1);
        if ((State & sfSelected) != 0)
        {
            l = DisplayedPos(SelStart) - FirstPos;
            r = DisplayedPos(SelEnd) - FirstPos;
            l = Math.Max(0, l);
            r = Math.Min(Size.X - 2, r);
            if (l < r)
                b.MoveChar(l + 1, '\0', GetColor(3), r - l);
        }
        WriteLine(0, 0, Size.X, Size.Y, b);
        SetCursor(DisplayedPos(CurPos) - FirstPos + 1, 0);
    }

    /// <summary>Writes the text (or the validator's transfer) into <paramref name="rec"/>.</summary>
    public override void GetData(Span<object?> rec)
    {
        if (_validator is null || _validator.Transfer(ref Data, ref rec[0], TVTransfer.vtGetData) == 0)
            rec[0] = Data;
    }

    /// <inheritdoc />
    public override TPalette GetPalette() => _palette;

    private int MouseDelta(in TEvent ev)
    {
        var mouse = MakeLocal(ev.Mouse.Where);
        if (mouse.X <= 0)
            return -1;
        if (mouse.X >= Size.X - 1)
            return 1;
        return 0;
    }

    private int MousePos(in TEvent ev)
    {
        var mouse = MakeLocal(ev.Mouse.Where);
        mouse.X = Math.Max(mouse.X, 1);
        var pos = mouse.X + FirstPos - 1;
        pos = Math.Max(pos, 0);
        return TText.Scroll(Data, pos, false);
    }

    private int DisplayedPos(int pos) => Strwidth(Data.AsSpan(0, pos));

    private void DeleteSelect()
    {
        if (SelStart < SelEnd)
        {
            Data = Data.Remove(SelStart, SelEnd - SelStart);
            CurPos = SelStart;
        }
    }

    private void DeleteCurrent()
    {
        if (CurPos < Data.Length)
        {
            SelStart = CurPos;
            SelEnd = CurPos + TText.Next(Data.AsSpan(CurPos));
            DeleteSelect();
        }
    }

    private void AdjustSelectBlock()
    {
        if (CurPos < _anchor)
        {
            SelStart = CurPos;
            SelEnd = _anchor;
        }
        else
        {
            SelStart = _anchor;
            SelEnd = CurPos;
        }
    }

    private void SaveState()
    {
        if (_validator is not null)
        {
            _oldData = Data;
            _oldCurPos = CurPos;
            _oldFirstPos = FirstPos;
            _oldSelStart = SelStart;
            _oldSelEnd = SelEnd;
        }
    }

    private void RestoreState()
    {
        if (_validator is not null)
        {
            Data = _oldData;
            CurPos = _oldCurPos;
            FirstPos = _oldFirstPos;
            SelStart = _oldSelStart;
            SelEnd = _oldSelEnd;
        }
    }

    private bool CheckValid(bool noAutoFill)
    {
        var result = true;
        if (_validator is not null)
        {
            var oldLen = Data.Length;
            var newData = Data;
            if (!_validator.IsValidInput(ref newData, noAutoFill))
            {
                RestoreState();
                result = false;
            }
            else
            {
                if (newData.Length > MaxLen)
                    newData = newData[..MaxLen];
                Data = newData;
                if (CurPos >= oldLen && newData.Length > oldLen)
                    CurPos = newData.Length;
            }
        }
        return result;
    }

    /// <summary>Handles mouse selection and scrolling, and editing keys.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        bool extendBlock;
        base.HandleEvent(ref ev);

        int delta, i, curWidth;
        if ((State & sfSelected) != 0)
        {
            switch (ev.What)
            {
                case evMouseDown:
                    if (CanScroll(delta = MouseDelta(ev)))
                    {
                        do
                        {
                            if (CanScroll(delta))
                            {
                                FirstPos += delta;
                                DrawView();
                            }
                        } while (MouseEvent(ref ev, evMouseAuto));
                    }
                    else if ((ev.Mouse.EventFlags & meDoubleClick) != 0)
                        SelectAll(true);
                    else
                    {
                        _anchor = MousePos(ev);
                        do
                        {
                            if (ev.What == evMouseAuto)
                            {
                                delta = MouseDelta(ev);
                                if (CanScroll(delta))
                                    FirstPos += delta;
                            }
                            CurPos = MousePos(ev);
                            AdjustSelectBlock();
                            DrawView();
                        } while (MouseEvent(ref ev, evMouseMove | evMouseAuto));
                    }
                    ClearEvent(ref ev);
                    break;
                case evKeyDown:
                    SaveState();
                    ev.KeyDown.KeyCode = CtrlToArrow(ev.KeyDown.KeyCode);
                    if (Array.IndexOf(_padKeys, ev.KeyDown.CharScan.ScanCode) >= 0 &&
                        (ev.KeyDown.ControlKeyState & kbShift) != 0)
                    {
                        ev.KeyDown.CharScan.CharCode = 0;
                        if (CurPos == SelEnd)
                            _anchor = SelStart;
                        else if (SelStart == SelEnd)
                            _anchor = CurPos;
                        else
                            _anchor = SelEnd;
                        extendBlock = true;
                    }
                    else
                        extendBlock = false;

                    switch (ev.KeyDown.KeyCode)
                    {
                        case kbLeft:
                            CurPos -= TText.Prev(Data, CurPos);
                            break;
                        case kbRight:
                            CurPos += TText.Next(Data.AsSpan(CurPos));
                            break;
                        case kbCtrlLeft:
                            CurPos = PrevWord(Data, CurPos);
                            break;
                        case kbCtrlRight:
                            CurPos = NextWord(Data, CurPos);
                            break;
                        case kbHome:
                            CurPos = 0;
                            break;
                        case kbEnd:
                            CurPos = Data.Length;
                            break;
                        case kbBack:
                            if (SelStart == SelEnd)
                            {
                                SelStart = CurPos - TText.Prev(Data, CurPos);
                                SelEnd = CurPos;
                            }
                            DeleteSelect();
                            CheckValid(true);
                            break;
                        case kbCtrlBack:
                        case kbAltBack:
                            if (SelStart == SelEnd)
                            {
                                SelStart = PrevWord(Data, CurPos);
                                SelEnd = CurPos;
                            }
                            DeleteSelect();
                            CheckValid(true);
                            break;
                        case kbDel:
                            if (SelStart == SelEnd)
                                DeleteCurrent();
                            else
                                DeleteSelect();
                            CheckValid(true);
                            break;
                        case kbCtrlDel:
                            if (SelStart == SelEnd)
                            {
                                SelStart = CurPos;
                                SelEnd = NextWord(Data, CurPos);
                            }
                            DeleteSelect();
                            CheckValid(true);
                            break;
                        case kbIns:
                            SetState(sfCursorIns, (State & sfCursorIns) == 0);
                            break;
                        default:
                            // The event text may contain null characters; upstream stops at the first one.
                            var keyText = ev.KeyDown.Text ?? "";
                            var nul = keyText.IndexOf('\0');
                            if (nul >= 0)
                                keyText = keyText[..nul];
                            if (keyText.Length > 0)
                            {
                                DeleteSelect();
                                if ((State & sfCursorIns) != 0)
                                    DeleteCurrent();

                                if (CheckValid(true))
                                {
                                    if (keyText[0] is '\t' or '\r' or '\n')
                                        keyText = " " + keyText[1..]; // Replace tabs and newlines into spaces.
                                    var dataMts = TText.Measure(Data);
                                    var keyMts = TText.Measure(keyText);
                                    if (Data.Length + keyText.Length <= MaxLen &&
                                        (uint)(dataMts.Width + keyMts.Width) <= (uint)MaxWidth &&
                                        (uint)(dataMts.GraphemeCount + keyMts.GraphemeCount) <= (uint)MaxChars)
                                    {
                                        if (FirstPos > CurPos)
                                            FirstPos = CurPos;
                                        Data = Data.Insert(CurPos, keyText);
                                        CurPos += keyText.Length;
                                    }
                                    CheckValid(false);
                                }
                            }
                            else if (ev.KeyDown.CharScan.CharCode == CONTROL_Y)
                            {
                                Data = "";
                                CurPos = 0;
                            }
                            else
                                return;
                            break;
                    }
                    if (extendBlock)
                        AdjustSelectBlock();
                    else
                        SelStart = SelEnd = 0;
                    curWidth = DisplayedPos(CurPos);
                    if (FirstPos > curWidth)
                        FirstPos = curWidth;
                    i = curWidth - Size.X + 2;
                    if (FirstPos < i)
                        FirstPos = i;
                    DrawView();
                    ClearEvent(ref ev);
                    break;
                    // shortcut: cmCut/cmCopy/cmPaste are not handled because TClipboard and paste
                    // events are not ported yet; port the evCommand branch together with them.
            }
            if (CanUpdateCommands())
                UpdateCommands();
        }
    }

    /// <summary>Selects all the text (or nothing) and optionally scrolls the cursor into view.</summary>
    public void SelectAll(bool enable, bool scroll = true)
    {
        SelStart = 0;
        if (enable)
            CurPos = SelEnd = Data.Length;
        else
            CurPos = SelEnd = 0;
        if (scroll)
            FirstPos = Math.Max(0, DisplayedPos(CurPos) - Size.X + 2);
        DrawView();
        if (CanUpdateCommands())
            UpdateCommands();
    }

    /// <summary>Sets the text from <paramref name="rec"/> (or through the validator) and selects it.</summary>
    public override void SetData(ReadOnlySpan<object?> rec)
    {
        var slot = rec[0];
        if (_validator is null || _validator.Transfer(ref Data, ref slot, TVTransfer.vtSetData) == 0)
        {
            var s = slot as string ?? "";
            Data = s.Length > MaxLen ? s[..MaxLen] : s;
        }
        SelectAll(true);
    }

    /// <summary>Selects all the text when the line becomes selected, and keeps the clipboard commands up to date.</summary>
    public override void SetState(ushort aState, bool enable)
    {
        var updateBefore = CanUpdateCommands();
        base.SetState(aState, enable);
        var updateAfter = CanUpdateCommands();

        if (aState == sfSelected || (aState == sfActive && (State & sfSelected) != 0))
            SelectAll(enable, false);
        if (updateBefore != updateAfter)
            UpdateCommands();
    }

    /// <summary>Replaces the validator.</summary>
    public void SetValidator(TValidator? aValid) => _validator = aValid;

    private bool CanUpdateCommands() => (~State & (sfActive | sfSelected)) == 0;

    private void SetCmdState(ushort command, bool enable)
    {
        var s = new TCommandSet();
        s.EnableCmd(command);
        if (enable && CanUpdateCommands())
            EnableCommands(s);
        else
            DisableCommands(s);
    }

    private void UpdateCommands()
    {
        SetCmdState(cmCut, SelStart < SelEnd);
        SetCmdState(cmCopy, SelStart < SelEnd);
        SetCmdState(cmPaste, true);
    }

    /// <summary>Checks the text with the validator, unless closing with <c>cmCancel</c>.</summary>
    public override bool Valid(ushort command)
    {
        if (_validator is not null)
        {
            if (command == cmValid)
                return _validator.Status == vsOk;
            if (command != cmCancel && !_validator.Validate(Data))
            {
                Select();
                return false;
            }
        }
        return true;
    }

    private static int PrevWord(string s, int pos)
    {
        for (var i = pos - 1; i >= 1; --i)
        {
            if (s[i] != ' ' && s[i - 1] == ' ')
                return i;
        }
        return 0;
    }

    private static int NextWord(string s, int pos)
    {
        for (var i = pos; i < s.Length - 1; ++i)
        {
            if (s[i] == ' ' && s[i + 1] != ' ')
                return i + 1;
        }
        return s.Length;
    }
}
