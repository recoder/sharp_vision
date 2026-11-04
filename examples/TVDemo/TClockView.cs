using System;
using System.Globalization;
using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Views;

/// <summary>Shows the time, in the top-right corner.</summary>
/// Ported from examples/tvdemo/gadgets.h, gadgets.cpp (class TClockView).
internal sealed class TClockView : TView
{
    private string _lastTime = "        ";
    private string _curTime = "        ";

    public TClockView(TRect r) : base(r)
    {
    }

    public override void Draw()
    {
        var buf = new TDrawBuffer();
        var c = GetColor(2);

        buf.MoveChar(0, ' ', c, Size.X);
        buf.MoveStr(0, _curTime, c);
        WriteLine(0, 0, Size.X, 1, buf);
    }

    public void Update()
    {
        _curTime = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

        if (_lastTime != _curTime)
        {
            DrawView();
            _lastTime = _curTime;
        }
    }
}
