using System;
using System.IO;
using SharpVision.Objects;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Objects;

public sealed class PrintConstantsTests
{
    private static string Print(Action<TextWriter, ushort> print, ushort value)
    {
        var os = new StringWriter();
        print(os, value);
        return os.ToString();
    }

    [Fact]
    public void Should_PrintCodeName_Or_HexValue()
    {
        Assert.Equal("kbAltX", Print(PrintConstants.PrintKeyCode, kbAltX));
        Assert.Equal("0x1234", Print(PrintConstants.PrintKeyCode, 0x1234));
        Assert.Equal("evKeyDown", Print(PrintConstants.PrintEventCode, evKeyDown));
    }

    [Fact]
    public void Should_PrintFlagNames_And_UnknownBitsInHex()
    {
        Assert.Equal("kbShift | kbLeftCtrl", Print(PrintConstants.PrintControlKeyState, kbShift | kbLeftCtrl));
        Assert.Equal("mbLeftButton | 0x0080", Print(PrintConstants.PrintMouseButtonState, mbLeftButton | 0x80));
        Assert.Equal("0x0000", Print(PrintConstants.PrintMouseEventFlags, 0));
    }
}
