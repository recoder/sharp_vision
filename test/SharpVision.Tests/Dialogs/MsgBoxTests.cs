using System;
using System.Collections.Generic;
using System.Linq;

using SharpVision.App;
using SharpVision.Dialogs;
using SharpVision.Drawing;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Platform;
using Xunit;

using static SharpVision.Dialogs.MsgBox;
using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.Dialogs;

public sealed class MsgBoxTests : CommandStateTest, IDisposable
{
    private readonly ushort _width = TScreen.ScreenWidth, _height = TScreen.ScreenHeight;
    private readonly TScreenCell[]? _buffer = TScreen.ScreenBuffer;
    private readonly ScriptedProgram _app;

    public MsgBoxTests()
    {
        TScreen.ScreenWidth = 80;
        TScreen.ScreenHeight = 25;
        TScreen.ScreenBuffer = new TScreenCell[80 * 25];
        _app = new ScriptedProgram();
    }

    public new void Dispose()
    {
        TObject.Destroy(_app);
        TScreen.ScreenWidth = _width;
        TScreen.ScreenHeight = _height;
        TScreen.ScreenBuffer = _buffer;
        base.Dispose();
    }

    private static TEvent Key(ushort keyCode, string? text = null) =>
        new() { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = keyCode, Text = text } };

    [Fact]
    public void Should_ShowTitleMessageAndCenteredButtons()
    {
        // The No button: three 10-wide buttons 2 apart (34 columns) start at 20 + (40 - 34) / 2 = 23.
        _app.Events.Enqueue(new TEvent { What = evMouseDown, Mouse = new MouseEventType { Where = new TPoint(38, 13), Buttons = mbLeftButton } });
        _app.Events.Enqueue(new TEvent { What = evMouseUp, Mouse = new MouseEventType { Where = new TPoint(38, 13) } });

        var result = MessageBox("Save changes?", mfConfirmation | mfYesNoCancel);

        Assert.Equal(cmNo, result);
        // UPSTREAM: the 40x9 box is centered using the desktop's size (80x23) but inserted into
        // the application, so it spans rows 7-15 rather than being centered on the desktop.
        var s = _app.Snapshot;
        Assert.Contains("Confirm", s[7]);
        Assert.Equal("Save changes?", s[9].Substring(23, 13));
        Assert.InRange(s[13].IndexOf("Yes", StringComparison.Ordinal), 23, 32);
        Assert.InRange(s[13].IndexOf("No", StringComparison.Ordinal), 35, 44);
        Assert.InRange(s[13].IndexOf("Cancel", StringComparison.Ordinal), 47, 56);
    }

    [Fact]
    public void Should_GrowBox_When_MessageIsLong()
    {
        var msg = new string('x', 33 * 4);

        MessageBox(msg, mfError | mfOKButton);

        // 132 columns over 33-column lines: height 132 / 33 + 7 = 11, so the box starts on row (23 - 11) / 2.
        Assert.Contains("Error", _app.Snapshot[6]);
        Assert.Contains("OK", _app.Snapshot[14]);
    }

    [Fact]
    public void Should_ReturnTypedText_When_InputBoxAccepted()
    {
        var s = "ab";
        _app.Events.Enqueue(Key(kbEnd));
        _app.Events.Enqueue(Key('c', "c"));
        _app.Events.Enqueue(new TEvent { What = evCommand, Message = new MessageEvent { Command = cmOK } });

        var result = InputBox("Title", "~N~ame", ref s, 10);

        Assert.Equal(cmOK, result);
        Assert.Equal("abc", s);
    }

    [Fact]
    public void Should_KeepText_When_InputBoxCancelled()
    {
        var s = "ab";
        _app.Events.Enqueue(Key('z', "z"));

        Assert.Equal(cmCancel, InputBox("Title", "Name", ref s, 10));
        Assert.Equal("ab", s);
    }
}
