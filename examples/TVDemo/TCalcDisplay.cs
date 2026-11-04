using System;
using System.Globalization;
using SharpVision.Dialogs;
using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.ViewFlags;

/// <summary>The calculator's display, which also takes the keys and button presses.</summary>
/// Ported from examples/tvdemo/calc.h, calc.cpp (class TCalcDisplay).
internal sealed class TCalcDisplay : TView
{
    public const ushort cmCalcButton = 200;
    private const int DISPLAYLEN = 25; // Length (width) of calculator display

    private enum TCalcState
    {
        csFirst = 1,
        csValid,
        csError,
    }

    private static readonly TPalette _palette = new([0x13]);

    private TCalcState _status;
    private string _number = "";
    private char _sign;
    private char _operate; // since 'operator' is a reserved word.
    private double _operand;

    public TCalcDisplay(TRect r) : base(r)
    {
        Options |= ofSelectable;
        EventMask = evKeyboard | evBroadcast;
        Clear();
    }

    public override TPalette GetPalette() => _palette;

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);

        switch (ev.What)
        {
            case evKeyboard:
                CalcKey(ev.KeyDown.CharScan.CharCode);
                ClearEvent(ref ev);
                break;
            case evBroadcast:
                if (ev.Message.Command == cmCalcButton)
                {
                    // The titles hold the code page characters (Esc is '←', +/- is '±').
                    CalcKey(TText.ToCodePage(((TButton)ev.Message.InfoPtr!).Title));
                    ClearEvent(ref ev);
                }
                break;
        }
    }

    public override void Draw()
    {
        var color = GetColor(1);
        var buf = new TDrawBuffer();

        var i = Size.X - _number.Length - 2;
        buf.MoveChar(0, ' ', color, Size.X);
        buf.MoveChar(i, _sign, color, 1);
        buf.MoveStr(i + 1, _number, color);
        WriteLine(0, 0, Size.X, 1, buf);
    }

    private void Error()
    {
        _status = TCalcState.csError;
        _number = "Error";
        _sign = ' ';
    }

    private void Clear()
    {
        _status = TCalcState.csFirst;
        _number = "0";
        _sign = ' ';
        _operate = '=';
        _operand = 0;
    }

    // Like ostream << double: %g with 6 significant digits.
    private static string FormatG(double r) => r.ToString("G6", CultureInfo.InvariantCulture).Replace('E', 'e');

    private void SetDisplay(double r)
    {
        string str;
        if (r < 0.0)
        {
            _sign = '-';
            str = FormatG(-r);
        }
        else
        {
            str = FormatG(r);
            _sign = ' ';
        }

        var len = str.Length - 1; // Minus one so we can use as an index.

        if (len > DISPLAYLEN)
            Error();
        else
            _number = str;
    }

    private void CheckFirst()
    {
        if (_status == TCalcState.csFirst)
        {
            _status = TCalcState.csValid;
            _number = "0";
            _sign = ' ';
        }
    }

    // Like atof: 0 when there is no number.
    private double GetDisplay() =>
        double.TryParse(_number, NumberStyles.Float, CultureInfo.InvariantCulture, out var r) ? r : 0;

    private void CalcKey(byte key)
    {
        double r;

        key = (byte)char.ToUpperInvariant((char)key);
        if (_status == TCalcState.csError && key != 'C')
            key = (byte)' ';

        switch (key)
        {
            case (byte)'0' or (byte)'1' or (byte)'2' or (byte)'3' or (byte)'4'
                or (byte)'5' or (byte)'6' or (byte)'7' or (byte)'8' or (byte)'9':
                CheckFirst();
                if (_number.Length < 15)
                {
                    // 15 is max visible display length
                    if (_number == "0")
                        _number = "";
                    _number += (char)key;
                }
                break;

            case (byte)'.':
                CheckFirst();
                if (!_number.Contains('.'))
                    _number += '.';
                break;

            case 8:
            case 27:
                CheckFirst();
                if (_number.Length == 1)
                    _number = "0";
                else
                    _number = _number[..^1];
                break;

            case (byte)'_': // underscore (keyboard version of +/-)
            case 241: // +/- extended character.
                _sign = _sign == ' ' ? '-' : ' ';
                break;

            case (byte)'+' or (byte)'-' or (byte)'*' or (byte)'/' or (byte)'=' or (byte)'%' or 13:
                if (_status == TCalcState.csValid)
                {
                    _status = TCalcState.csFirst;
                    r = GetDisplay() * (_sign == '-' ? -1.0 : 1.0);
                    if (key == '%')
                    {
                        if (_operate == '+' || _operate == '-')
                            r = _operand * r / 100;
                        else
                            r /= 100;
                    }
                    switch (_operate)
                    {
                        case '+':
                            SetDisplay(_operand + r);
                            break;
                        case '-':
                            SetDisplay(_operand - r);
                            break;
                        case '*':
                            SetDisplay(_operand * r);
                            break;
                        case '/':
                            if (r == 0)
                                Error();
                            else
                                SetDisplay(_operand / r);
                            break;
                    }
                }
                _operate = (char)key;
                _operand = GetDisplay() * (_sign == '-' ? -1.0 : 1.0);
                break;

            case (byte)'C':
                Clear();
                break;
        }
        DrawView();
    }
}
