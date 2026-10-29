using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Platform;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Platform;

public sealed class TermIOTests
{
    private sealed class ScriptedInput(string bytes) : IInputGetter
    {
        private readonly Queue<int> _bytes = new(Encoding.UTF8.GetBytes(bytes).Select(b => (int)b));
        private readonly Stack<int> _ungotten = new();

        public int Remaining => _bytes.Count + _ungotten.Count;

        public int Get() => _ungotten.Count > 0 ? _ungotten.Pop() : _bytes.Count > 0 ? _bytes.Dequeue() : -1;

        public void Unget(int k) => _ungotten.Push(k);
    }

    private static (ParseResult Result, TEvent Event, InputState State) Parse(string input, InputState? state = null)
    {
        state ??= new InputState();
        var ev = new TEvent();
        var result = TermIO.ParseEvent(new GetChBuf(new ScriptedInput(input)), ref ev, state);
        return (result, ev, state);
    }

    [Theory]
    [InlineData("\x1B[A", kbUp, 0)]
    [InlineData("\x1B[1;5D", kbCtrlLeft, kbLeftCtrl)]
    [InlineData("\x1B[1;2C", kbRight, kbShift)]
    [InlineData("\x1BOP", kbF1, 0)]
    [InlineData("\x1BOA", kbUp, 0)]
    [InlineData("\x1B[15~", kbF5, 0)]
    [InlineData("\x1B[24~", kbF12, 0)]
    [InlineData("\x1B[3~", kbDel, 0)]
    [InlineData("\x1B[3;5~", kbCtrlDel, kbLeftCtrl)]
    [InlineData("\x1B[H", kbHome, 0)]
    [InlineData("\x1B[Z", kbShiftTab, kbShift)]
    [InlineData("\x1B\x1B[A", kbAltUp, kbLeftAlt)]
    [InlineData("\x1B[97;5u", kbCtrlA, kbLeftCtrl)]
    [InlineData("\x1B[27;3;120~", kbAltX, kbLeftAlt)]
    public void Should_ParseKeySequences(string input, ushort keyCode, ushort mods)
    {
        var (result, ev, _) = Parse(input);

        Assert.Equal(ParseResult.Accepted, result);
        Assert.Equal(evKeyDown, ev.What);
        Assert.Equal(keyCode, ev.KeyDown.KeyCode);
        Assert.Equal(mods, ev.KeyDown.ControlKeyState);
    }

    [Fact]
    public void Should_ParseSGRMouse_PressAndRelease()
    {
        var state = new InputState();

        var (result, ev, _) = Parse("\x1B[<0;10;5M", state);
        Assert.Equal(ParseResult.Accepted, result);
        Assert.Equal(evMouse, ev.What);
        Assert.Equal(new TPoint(9, 4), ev.Mouse.Where);
        Assert.Equal(mbLeftButton, ev.Mouse.Buttons);

        (_, ev, _) = Parse("\x1B[<0;10;5m", state);
        Assert.Equal(0, ev.Mouse.Buttons);
    }

    [Fact]
    public void Should_ParseSGRMouseWheel()
    {
        var (_, ev, _) = Parse("\x1B[<65;1;1M");

        Assert.Equal(mwDown, ev.Mouse.Wheel);
    }

    [Fact]
    public void Should_ParseX10Mouse()
    {
        var (result, ev, _) = Parse("\x1B[M" + (char)(32 + 2) + (char)(32 + 3) + (char)(32 + 4));

        Assert.Equal(ParseResult.Accepted, result);
        Assert.Equal(new TPoint(2, 3), ev.Mouse.Where);
        Assert.Equal(mbRightButton, ev.Mouse.Buttons);
    }

    [Fact]
    public void Should_TrackBracketedPaste()
    {
        var state = new InputState();

        Assert.Equal(ParseResult.Ignored, Parse("\x1B[200~", state).Result);
        Assert.True(state.BracketedPaste);
        Assert.Equal(ParseResult.Ignored, Parse("\x1B[201~", state).Result);
        Assert.False(state.BracketedPaste);
    }

    [Fact]
    public void Should_Reject_When_NotAnEscapeSequence()
    {
        var input = new ScriptedInput("\x1B");
        var buf = new GetChBuf(input);
        var ev = new TEvent();

        Assert.Equal(ParseResult.Rejected, TermIO.ParseEvent(buf, ref ev, new InputState()));
        buf.Reject();
        Assert.Equal(1, input.Remaining);
    }

    [Fact]
    public void Should_IgnoreOscResponses()
    {
        Assert.Equal(ParseResult.Ignored, Parse("\x1B]52;c;aGVsbG8=\x07").Result);
        Assert.Equal(ParseResult.Ignored, Parse("\x1BP+r1\x1B\\").Result);
    }
}
