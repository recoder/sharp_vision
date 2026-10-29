using SharpVision.Dialogs;
using SharpVision.Objects;
using SharpVision.Tests.Views;
using Xunit;

namespace SharpVision.Tests.Dialogs;

public sealed class TStaticTextTests : CommandStateTest
{
    private static TestScreen Draw(string text, int w, int h)
    {
        var (screen, desk) = DialogDesk.Create(w + 4, h + 4);
        desk.Insert(new TStaticText(new TRect(0, 0, w, h), text));
        return screen;
    }

    [Fact]
    public void Should_WrapAtSpaces()
    {
        var screen = Draw("How are you today?", 9, 3);

        Assert.Equal("How are  ", screen.Row(0)[..9]);
        Assert.Equal("you      ", screen.Row(1)[..9]);
        Assert.Equal("today?   ", screen.Row(2)[..9]);
    }

    [Fact]
    public void Should_BreakLongWords()
    {
        var screen = Draw("abcdefghijk", 5, 3);

        Assert.Equal("abcde", screen.Row(0)[..5]);
        Assert.Equal("fghij", screen.Row(1)[..5]);
        Assert.Equal("k    ", screen.Row(2)[..5]);
    }

    [Fact]
    public void Should_CenterLine_When_StartsWithCtrlC_UntilNewline()
    {
        var screen = Draw("\x03" + "ab\ncd", 6, 2);

        Assert.Equal("  ab  ", screen.Row(0)[..6]);
        Assert.Equal("cd    ", screen.Row(1)[..6]);
    }

    [Fact]
    public void Should_UseStaticTextColor()
    {
        var screen = Draw("x", 3, 1);

        // Palette 1 -> 0x06; outside a dialog that is the application entry itself.
        Assert.Equal(0x06, (byte)screen.AttrAt(0, 0));
    }

    [Fact]
    public void Should_TruncateGetText_To255()
    {
        var t = new TStaticText(new TRect(0, 0, 1, 1), new string('a', 300));

        Assert.Equal(255, t.GetText().Length);
    }
}
