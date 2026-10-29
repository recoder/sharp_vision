using SharpVision.Events;
using Xunit;

using static SharpVision.Events.KeyCodes;
using static SharpVision.Objects.Util;

namespace SharpVision.Tests.Objects;

public sealed class UtilTests
{
    [Theory]
    [InlineData("~F~ile", 4)]
    [InlineData("E~x~it", 4)]
    [InlineData("中~文~", 4)]
    [InlineData("", 0)]
    public void Should_IgnoreTildes_When_MeasuringHotkeyText(string text, int expected)
    {
        Assert.Equal(expected, Cstrlen(text));
    }

    [Theory]
    [InlineData("~F~ile", "F", 'F')]
    [InlineData("E~x~it", "x", 'X')]
    [InlineData("Open ~file", "file", 'F')]
    [InlineData("None", "", '\0')]
    public void Should_FindHotkey(string text, string hotKeyStr, char hotKey)
    {
        Assert.Equal(hotKeyStr, HotKeyStr(text).ToString());
        Assert.Equal(hotKey, HotKey(text));
    }

    [Theory]
    [InlineData('x', kbAltX)]
    [InlineData('A', kbAltA)]
    [InlineData('1', kbAlt1)]
    [InlineData('=', kbAltEqual)]
    [InlineData('≡', kbAltSpace)]
    [InlineData('!', 0)]
    public void Should_MapAltCodes(char c, ushort keyCode)
    {
        Assert.Equal(keyCode, GetAltCode(c));
        if (keyCode != 0)
            Assert.Equal(char.ToUpperInvariant(c), GetAltChar(keyCode));
    }

    [Fact]
    public void Should_PreferText_When_AltKeyHasIt()
    {
        var keyDown = new KeyDownEvent { KeyCode = kbAltX, ControlKeyState = kbLeftAlt, Text = "ф" };

        Assert.Equal("ф", GetAltCharStr(keyDown));
    }

    [Fact]
    public void Should_MapCtrlCodes()
    {
        Assert.Equal('C', GetCtrlChar(kbCtrlC));
        Assert.Equal(kbAltC | kbCtrlC, GetCtrlCode('c'));
    }
}
