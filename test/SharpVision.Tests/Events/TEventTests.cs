using SharpVision.Events;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Events;

public sealed class TEventTests
{
    [Fact]
    public void Should_HaveNoEvent_When_DefaultConstructed()
    {
        Assert.Equal(evNothing, default(TEvent).What);
    }

    [Fact]
    public void Should_AliasKeyCodeAndCharScan()
    {
        var ev = new TEvent { What = evKeyDown };
        ev.KeyDown.KeyCode = kbEnter;

        Assert.Equal(0x0d, ev.KeyDown.CharScan.CharCode);
        Assert.Equal(0x1c, ev.KeyDown.CharScan.ScanCode);

        ev.KeyDown.CharScan.CharCode = 0;
        Assert.Equal(0x1c00, ev.KeyDown.KeyCode);
    }

    [Fact]
    public void Should_ReturnEmptyText_When_KeyHasNoText()
    {
        var keyDown = new KeyDownEvent { KeyCode = kbF1 };

        Assert.True(keyDown.GetText().IsEmpty);
        keyDown.Text = "é";
        Assert.Equal("é", keyDown.GetText().ToString());
    }

    [Fact]
    public void Should_ReadLowBitsOfInfoLong()
    {
        var message = new MessageEvent { InfoLong = 0x1234FF80 };

        Assert.Equal(0xFF80, message.InfoWord);
        Assert.Equal(unchecked((short)0xFF80), message.InfoInt);
        Assert.Equal(0x80, message.InfoByte);
    }
}
