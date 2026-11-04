using System;
using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.ViewFlags;

/// <summary>A month calendar; the arrows (or +/-, Up/Down) change the month.</summary>
/// Ported from examples/tvdemo/calendar.h, calendar.cpp (class TCalendarView).
internal sealed class TCalendarView : TView
{
    private static readonly string[] _monthNames =
    [
        "",
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    ];

    private static readonly byte[] _daysInMonth = [0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

    private int _month, _year;
    private readonly int _curDay, _curMonth, _curYear;

    public TCalendarView(TRect r) : base(r)
    {
        Options |= ofSelectable;
        EventMask |= evMouseAuto;

        var calendar = DateTime.Now;
        _year = _curYear = calendar.Year;
        _month = _curMonth = calendar.Month;
        _curDay = calendar.Day;

        DrawView();
    }

    private static bool IsLeapYear(int year) => year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);

    private static int DayOfWeek(int day, int month, int year)
    {
        if (month < 3)
        {
            month += 10;
            --year;
        }
        else
            month -= 2;

        var century = year / 100;
        var yr = year % 100;
        var dw = ((26 * month - 2) / 10 + day + yr + yr / 4 + century / 4 - 2 * century) % 7;

        if (dw < 0)
            dw += 7;

        return dw;
    }

    public override void Draw()
    {
        var current = 1 - DayOfWeek(1, _month, _year);
        var days = _daysInMonth[_month] + (IsLeapYear(_year) && _month == 2 ? 1 : 0);
        var buf = new TDrawBuffer();

        var color = GetColor(6);
        var boldColor = GetColor(7);

        buf.MoveChar(0, ' ', color, 22);
        buf.MoveStr(0, $"{_monthNames[_month],9} {_year,4} ▲  ▼ ", color);
        WriteLine(0, 0, 22, 1, buf);

        buf.MoveChar(0, ' ', color, 22);
        buf.MoveStr(0, "Su Mo Tu We Th Fr Sa", color);
        WriteLine(0, 1, 22, 1, buf);

        for (var i = 1; i <= 6; i++)
        {
            buf.MoveChar(0, ' ', color, 22);
            for (var j = 0; j <= 6; j++)
            {
                if (current < 1 || current > days)
                    buf.MoveStr(j * 3, "   ", color);
                else
                {
                    var str = $"{current,2}";
                    if (_year == _curYear && _month == _curMonth && current == _curDay)
                        buf.MoveStr(j * 3, str, boldColor);
                    else
                        buf.MoveStr(j * 3, str, color);
                }
                current++;
            }
            WriteLine(0, i + 1, 22, 1, buf);
        }
    }

    private void NextMonth()
    {
        ++_month;
        if (_month > 12)
        {
            _month = 1;
            ++_year;
        }
        DrawView();
    }

    private void PrevMonth()
    {
        --_month;
        if (_month < 1)
        {
            _month = 12;
            --_year;
        }
        DrawView();
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if ((State & sfSelected) != 0)
        {
            if ((ev.What & (evMouseDown | evMouseAuto)) != 0)
            {
                var point = MakeLocal(ev.Mouse.Where);
                if (point.X == 15 && point.Y == 0)
                    PrevMonth();
                else if (point.X == 18 && point.Y == 0)
                    NextMonth();
            }
            else if (ev.What == evKeyboard)
            {
                if (ev.KeyDown.CharScan.CharCode == '+' || ev.KeyDown.KeyCode == kbDown)
                    NextMonth();
                else if (ev.KeyDown.CharScan.CharCode == '-' || ev.KeyDown.KeyCode == kbUp)
                    PrevMonth();
            }
        }
    }
}
