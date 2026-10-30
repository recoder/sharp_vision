using System;

using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Validators;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.Dialogs;

public sealed class TInputLineTests : CommandStateTest
{
    private sealed class NoDigits : TValidator
    {
        public int Errors;

        public override bool IsValidInput(ref string s, bool suppressFill) => !s.AsSpan().ContainsAnyInRange('0', '9');

        public override bool IsValid(string s) => s.Length > 0;

        public override void Error() => Errors++;
    }

    private static (Views.TestScreen Screen, TInputLine Line) Setup(int limit = 20, TValidator? validator = null)
    {
        var (screen, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 30, 8), null);
        var line = new TInputLine(new TRect(2, 2, 10, 3), limit, validator);
        d.Insert(line);
        desk.Insert(d);
        return (screen, line);
    }

    private static void Key(TInputLine line, ushort keyCode, string? text = null, ushort shift = 0)
    {
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode, ControlKeyState = shift, Text = text } };
        line.HandleEvent(ref ev);
    }

    private static void Type(TInputLine line, string s)
    {
        foreach (var c in s)
            Key(line, c, c.ToString());
    }

    [Fact]
    public void Should_InsertTypedText_And_ScrollWithArrows()
    {
        var (screen, line) = Setup();

        Type(line, "hello");
        Assert.Equal("hello", line.Data);
        Assert.Equal(" hello  ", screen.Row(2)[2..10]);

        Type(line, " world");
        // 11 chars in 8 columns: scrolled so the cursor stays visible, with a left arrow.
        Assert.Equal(5, line.FirstPos);
        Assert.Equal("◄ world ", screen.Row(2)[2..10]);
    }

    [Fact]
    public void Should_EditWithBackspaceWordKeysAndCtrlY()
    {
        var (_, line) = Setup();
        Type(line, "one two");

        Key(line, kbBack);
        Assert.Equal("one tw", line.Data);
        Key(line, kbCtrlLeft);
        Assert.Equal(4, line.CurPos);
        Key(line, kbCtrlS); // WordStar left.
        Assert.Equal(3, line.CurPos);
        Key(line, kbCtrlY);
        Assert.Equal("", line.Data);
    }

    [Fact]
    public void Should_SelectWithShift_And_ReplaceSelection()
    {
        var (_, line) = Setup();
        Type(line, "abcd");

        Key(line, kbHome, shift: kbLeftShift);
        Assert.Equal((0, 4), (line.SelStart, line.SelEnd));
        Assert.True(TView.CommandEnabled(cmCut));

        Type(line, "x");
        Assert.Equal("x", line.Data);
        Assert.False(TView.CommandEnabled(cmCut));
    }

    [Fact]
    public void Should_StopAtMaxLen()
    {
        var (_, line) = Setup(limit: 4);

        Type(line, "abcdef");

        Assert.Equal("abc", line.Data);
    }

    [Fact]
    public void Should_RestoreState_When_ValidatorRejectsInput()
    {
        var v = new NoDigits();
        var (_, line) = Setup(validator: v);
        Type(line, "ab1c");

        Assert.Equal("abc", line.Data);
        Assert.True(line.Valid(cmOK));
        Assert.Equal(0, v.Errors);

        Key(line, kbCtrlY);
        Assert.False(line.Valid(cmOK));
        Assert.Equal(1, v.Errors);
        Assert.True(line.Valid(cmCancel));
    }

    [Fact]
    public void Should_TransferText_And_SelectAll_When_DataSet()
    {
        var (_, line) = Setup(limit: 4);

        line.SetData(new object?[] { "hello" });
        var rec = new object?[1];
        line.GetData(rec);

        Assert.Equal("hel", rec[0]);
        Assert.Equal((0, 3, 3), (line.SelStart, line.SelEnd, line.CurPos));
        Assert.Equal(1, line.DataSize());
    }

    [Fact]
    public void Should_MapWordStarKeysToArrows()
    {
        Assert.Equal(kbLeft, Util.CtrlToArrow(kbCtrlS));
        Assert.Equal(kbBack, Util.CtrlToArrow(kbCtrlH));
        Assert.Equal(kbCtrlY, Util.CtrlToArrow(kbCtrlY));
    }
}
