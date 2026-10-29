// Ported from test/tvision/tkey.test.cpp.

using SharpVision.Events;
using Xunit;

using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Events;

public sealed class TKeyTests
{
    private const ushort kb0 = 0x0b30;
    private const ushort kbA = 0x1e61;
    private const ushort kbCtrlKpDiv = 0x3500;
    private const ushort kbBiosCtrlN = 0x310e;

    public static TheoryData<ushort, ushort, ushort, ushort> Cases => new()
    {
        { (ushort)(kbNoKey), (ushort)(0), (ushort)(kbNoKey), (ushort)(0) },
        { (ushort)(kbNoKey), (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)(kbNoKey), (ushort)(0) },
        { (ushort)(kbCtrlA), (ushort)(0), (ushort)'A', (ushort)(kbCtrlShift) },
        { (ushort)(kbCtrlA), (ushort)(kbCtrlShift), (ushort)'A', (ushort)(kbCtrlShift) },
        { (ushort)(kbCtrlA), (ushort)(kbShift | kbCtrlShift), (ushort)'A', (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kbCtrlA), (ushort)(kbAltShift), (ushort)'A', (ushort)(kbCtrlShift | kbAltShift) },
        { (ushort)(kbCtrlZ), (ushort)(0), (ushort)'Z', (ushort)(kbCtrlShift) },
        { (ushort)(kbCtrlZ), (ushort)(kbCtrlShift), (ushort)'Z', (ushort)(kbCtrlShift) },
        { (ushort)(kbCtrlZ), (ushort)(kbShift | kbCtrlShift), (ushort)'Z', (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kbCtrlZ), (ushort)(kbAltShift), (ushort)'Z', (ushort)(kbCtrlShift | kbAltShift) },
        { (ushort)(kbCtrlZ + 1), (ushort)(0), (ushort)(kbCtrlZ + 1), (ushort)(0) },
        { (ushort)(kbCtrlZ + 1), (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)(kbCtrlZ + 1), (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kbGrayPlus), (ushort)(0), (ushort)(kbGrayPlus), (ushort)(0) },
        { (ushort)(kbGrayPlus), (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)(kbGrayPlus), (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kbCtrlPrtSc), (ushort)(0), (ushort)(kbCtrlPrtSc), (ushort)(kbCtrlShift) },
        { (ushort)(kbCtrlPrtSc), (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)(kbCtrlPrtSc), (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)'A', (ushort)(0), (ushort)'A', (ushort)(0) },
        { (ushort)'A', (ushort)(kbShift), (ushort)'A', (ushort)(kbShift) },
        { (ushort)'A', (ushort)(kbCtrlShift), (ushort)'A', (ushort)(kbCtrlShift) },
        { (ushort)'A', (ushort)(kbShift | kbCtrlShift), (ushort)'A', (ushort)(kbShift | kbCtrlShift) },
        { (ushort)'A', (ushort)(kbAltShift), (ushort)'A', (ushort)(kbAltShift) },
        { (ushort)'A', (ushort)(kbShift | kbAltShift), (ushort)'A', (ushort)(kbShift | kbAltShift) },
        { (ushort)'A', (ushort)(kbCtrlShift | kbAltShift), (ushort)'A', (ushort)(kbCtrlShift | kbAltShift) },
        { (ushort)'A', (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)'A', (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)'a', (ushort)(0), (ushort)'A', (ushort)(0) },
        { (ushort)'a', (ushort)(kbRightAlt), (ushort)'A', (ushort)(kbAltShift) },
        { (ushort)'a', (ushort)(kbShift), (ushort)'A', (ushort)(kbShift) },
        { (ushort)'a', (ushort)(kbCtrlShift), (ushort)'A', (ushort)(kbCtrlShift) },
        { (ushort)'a', (ushort)(kbShift | kbCtrlShift), (ushort)'A', (ushort)(kbShift | kbCtrlShift) },
        { (ushort)'a', (ushort)(kbAltShift), (ushort)'A', (ushort)(kbAltShift) },
        { (ushort)'a', (ushort)(kbShift | kbAltShift), (ushort)'A', (ushort)(kbShift | kbAltShift) },
        { (ushort)'a', (ushort)(kbCtrlShift | kbAltShift), (ushort)'A', (ushort)(kbCtrlShift | kbAltShift) },
        { (ushort)'a', (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)'A', (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)'0', (ushort)(0), (ushort)'0', (ushort)(0) },
        { (ushort)'0', (ushort)(kbShift), (ushort)'0', (ushort)(kbShift) },
        { (ushort)'0', (ushort)(kbCtrlShift), (ushort)'0', (ushort)(kbCtrlShift) },
        { (ushort)'0', (ushort)(kbShift | kbCtrlShift), (ushort)'0', (ushort)(kbShift | kbCtrlShift) },
        { (ushort)'0', (ushort)(kbAltShift), (ushort)'0', (ushort)(kbAltShift) },
        { (ushort)'0', (ushort)(kbShift | kbAltShift), (ushort)'0', (ushort)(kbShift | kbAltShift) },
        { (ushort)'0', (ushort)(kbCtrlShift | kbAltShift), (ushort)'0', (ushort)(kbCtrlShift | kbAltShift) },
        { (ushort)'0', (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)'0', (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kbA), (ushort)(0), (ushort)'A', (ushort)(0) },
        { (ushort)(kbA), (ushort)(kbShift), (ushort)'A', (ushort)(kbShift) },
        { (ushort)(kbA), (ushort)(kbCtrlShift), (ushort)'A', (ushort)(kbCtrlShift) },
        { (ushort)(kbA), (ushort)(kbShift | kbCtrlShift), (ushort)'A', (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kbA), (ushort)(kbAltShift), (ushort)'A', (ushort)(kbAltShift) },
        { (ushort)(kbA), (ushort)(kbShift | kbAltShift), (ushort)'A', (ushort)(kbShift | kbAltShift) },
        { (ushort)(kbA), (ushort)(kbCtrlShift | kbAltShift), (ushort)'A', (ushort)(kbCtrlShift | kbAltShift) },
        { (ushort)(kbA), (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)'A', (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kb0), (ushort)(0), (ushort)'0', (ushort)(0) },
        { (ushort)(kb0), (ushort)(kbShift), (ushort)'0', (ushort)(kbShift) },
        { (ushort)(kb0), (ushort)(kbCtrlShift), (ushort)'0', (ushort)(kbCtrlShift) },
        { (ushort)(kb0), (ushort)(kbShift | kbCtrlShift), (ushort)'0', (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kb0), (ushort)(kbAltShift), (ushort)'0', (ushort)(kbAltShift) },
        { (ushort)(kb0), (ushort)(kbShift | kbAltShift), (ushort)'0', (ushort)(kbShift | kbAltShift) },
        { (ushort)(kb0), (ushort)(kbCtrlShift | kbAltShift), (ushort)'0', (ushort)(kbCtrlShift | kbAltShift) },
        { (ushort)(kb0), (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)'0', (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kbRight), (ushort)(kbShift), (ushort)(kbRight), (ushort)(kbShift) },
        { (ushort)(kbAltX), (ushort)(0), (ushort)'X', (ushort)(kbAltShift) },
        { (ushort)(kbTab), (ushort)(0), (ushort)(kbTab), (ushort)(0) },
        { (ushort)(kbTab), (ushort)(kbShift), (ushort)(kbTab), (ushort)(kbShift) },
        { (ushort)(kbShiftTab), (ushort)(0), (ushort)(kbTab), (ushort)(kbShift) },
        { (ushort)(kbTab), (ushort)(kbCtrlShift), (ushort)(kbTab), (ushort)(kbCtrlShift) },
        { (ushort)(kbTab), (ushort)(kbShift | kbCtrlShift), (ushort)(kbTab), (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kbShiftTab), (ushort)(kbCtrlShift), (ushort)(kbTab), (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kbShiftTab), (ushort)(kbShift | kbCtrlShift), (ushort)(kbTab), (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kbCtrlTab), (ushort)(kbShift), (ushort)(kbTab), (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kbTab), (ushort)(kbAltShift), (ushort)(kbTab), (ushort)(kbAltShift) },
        { (ushort)(kbTab), (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)(kbTab), (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kbShiftTab), (ushort)(kbAltShift), (ushort)(kbTab), (ushort)(kbShift | kbAltShift) },
        { (ushort)(kbShiftTab), (ushort)(kbCtrlShift | kbAltShift), (ushort)(kbTab), (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kbCtrlTab), (ushort)(kbAltShift), (ushort)(kbTab), (ushort)(kbCtrlShift | kbAltShift) },
        { (ushort)(kbCtrlTab), (ushort)(kbShift | kbAltShift), (ushort)(kbTab), (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kbEnter), (ushort)(0), (ushort)(kbEnter), (ushort)(0) },
        { (ushort)(kbEnter), (ushort)(kbShift), (ushort)(kbEnter), (ushort)(kbShift) },
        { (ushort)(kbEnter), (ushort)(kbCtrlShift), (ushort)(kbEnter), (ushort)(kbCtrlShift) },
        { (ushort)(kbEnter), (ushort)(kbShift | kbCtrlShift), (ushort)(kbEnter), (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kbCtrlEnter), (ushort)(kbShift), (ushort)(kbEnter), (ushort)(kbShift | kbCtrlShift) },
        { (ushort)(kbEnter), (ushort)(kbAltShift), (ushort)(kbEnter), (ushort)(kbAltShift) },
        { (ushort)(kbEnter), (ushort)(kbShift | kbCtrlShift | kbAltShift), (ushort)(kbEnter), (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kbCtrlEnter), (ushort)(kbAltShift), (ushort)(kbEnter), (ushort)(kbCtrlShift | kbAltShift) },
        { (ushort)(kbCtrlEnter), (ushort)(kbShift | kbAltShift), (ushort)(kbEnter), (ushort)(kbShift | kbCtrlShift | kbAltShift) },
        { (ushort)(kbTab), (ushort)(kbLeftCtrl), (ushort)(kbTab), (ushort)(kbCtrlShift) },
        { (ushort)(kbTab), (ushort)(kbRightCtrl), (ushort)(kbTab), (ushort)(kbCtrlShift) },
        { (ushort)(kbTab), (ushort)(kbLeftCtrl), (ushort)(kbTab), (ushort)(kbCtrlShift) },
        { (ushort)(kbCtrlTab), (ushort)(kbLeftCtrl), (ushort)(kbTab), (ushort)(kbCtrlShift) },
        { (ushort)(kbCtrlTab), (ushort)(kbRightCtrl), (ushort)(kbTab), (ushort)(kbCtrlShift) },
        { (ushort)(kbCtrlTab), (ushort)(kbLeftCtrl), (ushort)(kbTab), (ushort)(kbCtrlShift) },
        { (ushort)(kbA), (ushort)(kbLeftCtrl), (ushort)'A', (ushort)(kbCtrlShift) },
        { (ushort)(kbA), (ushort)(kbRightCtrl), (ushort)'A', (ushort)(kbCtrlShift) },
        { (ushort)(kbCtrlKpDiv), (ushort)(0), (ushort)'/', (ushort)(kbCtrlShift) },
        { (ushort)(kbBiosCtrlN), (ushort)(0), (ushort)'N', (ushort)(kbCtrlShift) },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Should_Normalize_When_Constructed(ushort code, ushort mods, ushort expectedCode, ushort expectedMods)
    {
        var key = new TKey(code, mods);

        Assert.Equal((expectedCode, expectedMods), (key.Code, key.Mods));
    }

    [Fact]
    public void Should_BeEqual_When_DescribingSameCombination()
    {
        Assert.Equal(kbCtrlA, new TKey('A', kbCtrlShift));
        Assert.Equal(new TKey(kbCtrlTab, kbShift), new TKey(kbTab, kbShift | kbCtrlShift));
        Assert.Equal(new TKey(kbAltDel, kbCtrlShift), new TKey(kbCtrlDel, kbAltShift));
        Assert.NotEqual(new TKey(kbF1), new TKey(kbF1, kbShift));
    }

    [Fact]
    public void Should_ConvertFromKeyDownEvent()
    {
        var keyDown = new KeyDownEvent { KeyCode = kbA, ControlKeyState = kbLeftCtrl };

        TKey key = keyDown;

        Assert.Equal(new TKey('A', kbCtrlShift), key);
    }
}
