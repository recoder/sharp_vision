using SharpVision.Editors;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Tests.Dialogs;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Editors;

public sealed class TMemoTests : CommandStateTest
{
    private static TMemo NewMemo(int bufSize = 16) => new(new TRect(0, 0, 10, 2), null, null, null, bufSize);

    private static object? Data(TMemo m)
    {
        var rec = new object?[1];
        m.GetData(rec);
        return rec[0];
    }

    [Fact]
    public void Should_RoundTripText_Across_TheGap()
    {
        var m = NewMemo();
        m.SetData(new object?[] { "ab\r\ncd" });
        m.SetCurPtr(2, 0);

        Assert.Equal(1, m.DataSize());
        Assert.Equal("ab\r\ncd", Data(m));
    }

    [Fact]
    public void Should_KeepText_When_DataDoesNotFit()
    {
        var m = NewMemo(4);
        m.SetData(new object?[] { "abc" });
        m.SetData(new object?[] { "too long" });

        Assert.Equal("abc", Data(m));
    }

    [Fact]
    public void Should_LeaveTab_ToTheDialog()
    {
        var m = NewMemo();
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = kbTab } };

        m.HandleEvent(ref ev);

        Assert.Equal((evKeyDown, ""), (ev.What, Data(m)));
    }

    [Fact]
    public void Should_DrawWith_MemoPalette()
    {
        var (screen, desk) = DialogDesk.Create(10, 2);
        var m = NewMemo();
        desk.Insert(m);
        m.SetData(new object?[] { "x" });
        m.DrawView();

        Assert.Equal(("x", 0x1A), (screen.Row(0)[..1], (int)screen.AttrAt(0, 0)));
    }
}
