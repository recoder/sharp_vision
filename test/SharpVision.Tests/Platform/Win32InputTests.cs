using SharpVision.Events;
using SharpVision.Platform;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.Platform.Interop.Kernel32;

namespace SharpVision.Tests.Platform;

public sealed class Win32InputTests
{
    private static (bool Ok, TEvent Ev) Key(ushort scan, char ch, uint mods = 0, ushort vk = 0, InputState? state = null)
    {
        var ev = default(TEvent);
        var rec = new KEY_EVENT_RECORD
        {
            bKeyDown = 1,
            wVirtualKeyCode = vk,
            wVirtualScanCode = scan,
            UnicodeChar = ch,
            dwControlKeyState = mods,
        };
        var ok = Win32Input.GetWin32Key(rec, ref ev, state ?? new InputState());
        return (ok, ev);
    }

    [Fact]
    public void Should_ReportCharAndScanCode_When_LetterTyped()
    {
        var (ok, ev) = Key(0x1E, 'a');

        Assert.True(ok);
        Assert.Equal(evKeyDown, ev.What);
        Assert.Equal(0x1E61, ev.KeyDown.KeyCode);
        Assert.Equal("a", ev.KeyDown.Text);
    }

    [Fact]
    public void Should_MapAltLetter_ToAltCode_WithoutText()
    {
        var (ok, ev) = Key(0x2D, 'x', kbLeftAlt);

        Assert.True(ok);
        Assert.Equal(kbAltX, ev.KeyDown.KeyCode);
        Assert.Null(ev.KeyDown.Text);
    }

    [Theory]
    [InlineData(0x3B, 0u, kbF1)]
    [InlineData(0x3B, (uint)kbShift, kbShiftF1)]
    [InlineData(0x2E, (uint)kbLeftCtrl, kbCtrlC)]
    public void Should_ConvertScanCodes(ushort scan, uint mods, ushort expected)
    {
        var ch = expected == kbCtrlC ? '\x03' : '\0';
        var (ok, ev) = Key(scan, ch, mods);

        Assert.True(ok);
        Assert.Equal(expected, ev.KeyDown.KeyCode);
        Assert.Null(ev.KeyDown.Text);
    }

    [Fact]
    public void Should_DropModifiers_When_AltGrProducesText()
    {
        var (ok, ev) = Key(0x10, '@', kbLeftCtrl | kbRightAlt);

        Assert.True(ok);
        Assert.Equal("@", ev.KeyDown.Text);
        Assert.Equal(0, ev.KeyDown.ControlKeyState & (kbCtrlShift | kbAltShift));
    }

    [Fact]
    public void Should_Discard_When_AltGrWithoutText()
    {
        Assert.False(Key(0x10, '\0', kbLeftCtrl | kbRightAlt).Ok);
    }

    [Fact]
    public void Should_Discard_When_StandaloneShift()
    {
        Assert.False(Key(0x2A, '\0', kbShift).Ok);
    }

    [Fact]
    public void Should_CombineSurrogatePair()
    {
        var state = new InputState();

        Assert.False(Key(0, '\uD83D', state: state).Ok);
        var (ok, ev) = Key(0, '\uDE00', state: state);

        Assert.True(ok);
        Assert.Equal("😀", ev.KeyDown.Text);
        // Not representable in the code page.
        Assert.Equal(kbNoKey, ev.KeyDown.KeyCode);
    }

    [Fact]
    public void Should_ClearScanCode_When_PastedThroughAlt()
    {
        var (ok, ev) = Key(0x38, 'é', vk: VK_MENU);

        Assert.True(ok);
        Assert.Equal(0, ev.KeyDown.CharScan.ScanCode);
        Assert.Equal("é", ev.KeyDown.Text);
    }

    [Theory]
    [InlineData(MOUSE_WHEELED, 0x00780000u, mwUp)]
    [InlineData(MOUSE_WHEELED, 0xFF880000u, mwDown)]
    [InlineData(MOUSE_HWHEELED, 0x00780000u, mwRight)]
    [InlineData(MOUSE_HWHEELED, 0xFF880000u, mwLeft)]
    public void Should_ReportWheelDirection(uint flags, uint buttons, byte wheel)
    {
        var ev = default(TEvent);
        var state = new InputState();
        var rec = new MOUSE_EVENT_RECORD { dwMousePosition = new COORD(3, 4), dwButtonState = buttons, dwEventFlags = flags };

        Win32Input.GetWin32Mouse(rec, ref ev, state);

        Assert.Equal(evMouse, ev.What);
        Assert.Equal(3, ev.Mouse.Where.X);
        Assert.Equal(4, ev.Mouse.Where.Y);
        Assert.Equal(wheel, ev.Mouse.Wheel);
    }

    [Fact]
    public void Should_TrackButtons()
    {
        var ev = default(TEvent);
        var state = new InputState();
        var rec = new MOUSE_EVENT_RECORD { dwButtonState = mbLeftButton };

        Win32Input.GetWin32Mouse(rec, ref ev, state);

        Assert.Equal(mbLeftButton, ev.Mouse.Buttons);
        Assert.Equal(mbLeftButton, state.Buttons);
    }
}
