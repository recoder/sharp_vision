using System;
using System.IO;
using System.Text;
using SharpVision.Drawing;
using SharpVision.Editors;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Events.EventCodes;
using static SharpVision.Objects.PrintConstants;
using static SharpVision.Views.ViewFlags;

/// <summary>A window that prints every event the application receives.</summary>
/// Ported from examples/tvdemo/evntview.h, evntview.cpp (class TEventViewer).
internal sealed class TEventViewer : TWindow
{
    private static readonly string[] _titles =
    [
        "Event Viewer",
        "Event Viewer (Stopped)",
    ];

    private bool _stopped;
    private long _eventCount;
    private TTerminal? _interior;
    private TScrollBar? _scrollBar;

    public TEventViewer(TRect bounds, ushort aBufSize) : base(bounds, null, wnNoNumber)
    {
        EventMask |= evBroadcast;
        Init(aBufSize);
    }

    private void Init(ushort aBufSize)
    {
        _stopped = false;
        _eventCount = 0;
        Title = _titles[0];
        _scrollBar = StandardScrollBar(sbVertical | sbHandleKeyboard);
        var r = GetExtent();
        r.Grow(-1, -1);
        _interior = new TTerminal(r, null, _scrollBar, aBufSize);
        Insert(_interior);
    }

    public override void ShutDown()
    {
        _interior = null;
        _scrollBar = null;
        base.ShutDown();
    }

    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evBroadcast && ev.Message.Command == TVCmds.cmFndEventView)
            ClearEvent(ref ev);
    }

    public void Toggle()
    {
        _stopped = !_stopped;
        Title = _titles[_stopped ? 1 : 0];
        Frame?.DrawView();
    }

    public void Print(in TEvent ev)
    {
        if (ev.What != evNothing && !_stopped && _interior is not null)
        {
            Lock();
            var os = new StringWriter();
            os.Write($"Received event #{++_eventCount}\n");
            PrintEvent(os, ev);
            _interior.DoSputn(os.ToString());
            Unlock();
        }
    }

    private static void PrintConstants(TextWriter os, ushort value, Action<TextWriter, ushort> doPrint)
    {
        os.Write($"0x{value:x4}");
        var name = new StringWriter();
        doPrint(name, value);
        var buf = name.ToString();
        if (buf.Length > 0 && buf[0] != '0')
            os.Write($" ({buf})");
    }

    private static void PrintEvent(TextWriter os, in TEvent ev)
    {
        os.Write("TEvent {\n  .what = ");
        PrintConstants(os, ev.What, PrintEventCode);
        os.Write(",\n");
        if ((ev.What & evMouse) != 0)
        {
            os.Write("  .mouse = MouseEventType {\n" +
                "    .where = TPoint {\n" +
                $"      .x = {ev.Mouse.Where.X}\n" +
                $"      .y = {ev.Mouse.Where.Y}\n" +
                "    },\n" +
                "    .eventFlags = ");
            PrintConstants(os, ev.Mouse.EventFlags, PrintMouseEventFlags);
            os.Write(",\n    .controlKeyState = ");
            PrintConstants(os, ev.Mouse.ControlKeyState, PrintControlKeyState);
            os.Write(",\n    .buttons = ");
            PrintConstants(os, ev.Mouse.Buttons, PrintMouseButtonState);
            os.Write(",\n    .wheel = ");
            PrintConstants(os, ev.Mouse.Wheel, PrintMouseWheelState);
            os.Write("\n  }\n");
        }
        if ((ev.What & evKeyboard) != 0)
        {
            var charCode = ev.KeyDown.CharScan.CharCode;
            os.Write("  .keyDown = KeyDownEvent {\n    .keyCode = ");
            PrintConstants(os, ev.KeyDown.KeyCode, PrintKeyCode);
            os.Write($",\n    .charScan = CharScanType {{\n      .charCode = {charCode}");
            if (charCode != 0)
                os.Write($" ('{TText.FromCodePage(charCode)}')");
            os.Write($",\n      .scanCode = {ev.KeyDown.CharScan.ScanCode}\n    }},\n    .controlKeyState = ");
            PrintConstants(os, ev.KeyDown.ControlKeyState, PrintControlKeyState);
            // The text is printed as its UTF-8 bytes, like upstream's char array.
            var text = Encoding.UTF8.GetBytes(ev.KeyDown.Text ?? "");
            os.Write(",\n    .text = {");
            for (var i = 0; i < text.Length; ++i)
                os.Write(i == 0 ? $"0x{text[i]:x}" : $", 0x{text[i]:x}");
            os.Write($"}},\n    .textLength = {text.Length}\n  }}\n");
        }
        if ((ev.What & evCommand) != 0)
        {
            // Upstream prints the infoPtr address; here, the object or the integer in InfoLong.
            os.Write("  .message = MessageEvent {\n" +
                $"    .command = {ev.Message.Command},\n" +
                $"    .infoPtr = {ev.Message.InfoPtr?.ToString() ?? ev.Message.InfoLong.ToString(System.Globalization.CultureInfo.InvariantCulture)}\n" +
                "  }\n");
        }
        os.Write("}\n");
    }
}
