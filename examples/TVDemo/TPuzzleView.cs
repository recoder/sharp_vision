using System;
using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.ViewFlags;

/// <summary>The 15-puzzle: slide the tiles into order with the arrow keys or the mouse.</summary>
/// Ported from examples/tvdemo/puzzle.h, puzzle.cpp (class TPuzzleView).
internal sealed class TPuzzleView : TView
{
    private static readonly TPalette _palette = new([0x06, 0x07]);
    private static readonly char[] _boardStart =
    [
        'A', 'B', 'C', 'D',
        'E', 'F', 'G', 'H',
        'I', 'J', 'K', 'L',
        'M', 'N', 'O', ' ',
    ];
    private static readonly byte[] _map =
    [
        0, 1, 0, 1,
        1, 0, 1, 0,
        0, 1, 0, 1,
        1, 0, 1,
    ];
    private const string Solution = "ABCDEFGHIJKLMNO ";

    private readonly char[,] _board = new char[6, 6];
    private int _moves;
    private bool _solved;

    public TPuzzleView(TRect r) : base(r)
    {
        Options |= ofSelectable;
        for (var i = 0; i < 6; i++)
            for (var j = 0; j < 6; j++)
                _board[i, j] = ' ';

        for (var i = 0; i <= 3; i++)
            for (var j = 0; j <= 3; j++)
                _board[i, j] = _boardStart[i * 4 + j];

        Scramble();
    }

    public override void Draw()
    {
        var color = new TColorAttr[2];
        var buf = new TDrawBuffer();

        color[0] = color[1] = GetColor(1);
        var colorBack = color[0];
        if (!_solved)
            color[1] = GetColor(2);

        for (var i = 0; i <= 3; i++)
        {
            buf.MoveChar(0, ' ', colorBack, 18);
            if (i == 1)
                buf.MoveStr(13, "Move", colorBack);
            if (i == 2)
                buf.MoveStr(14, _moves.ToString(System.Globalization.CultureInfo.InvariantCulture), colorBack);
            for (var j = 0; j <= 3; j++)
            {
                var tmp = $" {_board[i, j]} ";
                if (_board[i, j] == ' ')
                    buf.MoveStr(j * 3, tmp, color[0]);
                else
                    buf.MoveStr(j * 3, tmp, color[_map[_board[i, j] - 'A']]);
            }
            WriteLine(0, i, 18, 1, buf);
        }
    }

    public override TPalette GetPalette() => _palette;

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        if (_solved && (ev.What & (evKeyboard | evMouse)) != 0)
        {
            Scramble();
            ClearEvent(ref ev);
        }

        if (ev.What == evMouseDown)
        {
            MoveTile(ev.Mouse.Where);
            ClearEvent(ref ev);
            WinCheck();
        }
        else if (ev.What == evKeyDown)
        {
            MoveKey(ev.KeyDown.KeyCode);
            ClearEvent(ref ev);
            WinCheck();
        }
    }

    public void MoveKey(int key)
    {
        int i;
        for (i = 0; i <= 15; i++)
            if (_board[i / 4, i % 4] == ' ')
                break;

        var x = i % 4;
        var y = i / 4;

        switch (key)
        {
            case kbDown:
                if (y > 0)
                {
                    _board[y, x] = _board[y - 1, x];
                    _board[y - 1, x] = ' ';
                    if (_moves < 1000)
                        _moves++;
                }
                break;
            case kbUp:
                if (y < 3)
                {
                    _board[y, x] = _board[y + 1, x];
                    _board[y + 1, x] = ' ';
                    if (_moves < 1000)
                        _moves++;
                }
                break;
            case kbRight:
                if (x > 0)
                {
                    _board[y, x] = _board[y, x - 1];
                    _board[y, x - 1] = ' ';
                    if (_moves < 1000)
                        _moves++;
                }
                break;
            case kbLeft:
                if (x < 3)
                {
                    _board[y, x] = _board[y, x + 1];
                    _board[y, x + 1] = ' ';
                    if (_moves < 1000)
                        _moves++;
                }
                break;
        }
        DrawView();
    }

    public void MoveTile(TPoint p)
    {
        p = MakeLocal(p);

        int i;
        for (i = 0; i <= 15; i++)
            if (_board[i / 4, i % 4] == ' ')
                break;
        var x = p.X / 3;
        var y = p.Y;

        switch (y * 4 + x - i)
        {
            case -4: // Piece moves down
                MoveKey(kbDown);
                break;
            case -1: // Piece moves right
                MoveKey(kbRight);
                break;
            case 1: // Piece moves left
                MoveKey(kbLeft);
                break;
            case 4: // Piece moves up
                MoveKey(kbUp);
                break;
        }
        DrawView();
    }

    public void Scramble()
    {
        _moves = 0;
        _solved = false;
        do
        {
            switch (Random.Shared.Next(4))
            {
                case 0:
                    MoveKey(kbUp);
                    break;
                case 1:
                    MoveKey(kbDown);
                    break;
                case 2:
                    MoveKey(kbRight);
                    break;
                case 3:
                    MoveKey(kbLeft);
                    break;
            }
        } while (_moves++ <= 500);

        _moves = 0;
        DrawView();
    }

    public void WinCheck()
    {
        int i;
        for (i = 0; i <= 15; i++)
            if (_board[i / 4, i % 4] != Solution[i])
                break;

        if (i == 16)
            _solved = true;
        DrawView();
    }
}
