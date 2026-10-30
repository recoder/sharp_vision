namespace SharpVision.Events;

/// <summary>
/// Key codes for the <c>KeyCode</c> field of <c>evKeyDown</c> events and for menu hotkeys,
/// and the keyboard state masks for its <c>ControlKeyState</c> field.
/// </summary>
/// <remarks>
/// Not every combination can be reported on every platform: system shortcuts are never
/// reported, and terminals usually send Ctrl+H, Ctrl+I, Ctrl+J and Ctrl+M as Backspace,
/// Tab and Enter. For other combinations of Shift, Ctrl and Alt, see <see cref="TKey"/>.
/// </remarks>
/// Ported from include/tvision/tkeys.h (kbXxx constants).
public static class KeyCodes
{
    // Control keys
    /// <summary>Key code.</summary>
    public const ushort kbCtrlA = 0x0001;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlB = 0x0002;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlC = 0x0003;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlD = 0x0004;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlE = 0x0005;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF = 0x0006;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlG = 0x0007;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlH = 0x0008;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlI = 0x0009;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlJ = 0x000a;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlK = 0x000b;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlL = 0x000c;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlM = 0x000d;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlN = 0x000e;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlO = 0x000f;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlP = 0x0010;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlQ = 0x0011;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlR = 0x0012;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlS = 0x0013;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlT = 0x0014;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlU = 0x0015;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlV = 0x0016;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlW = 0x0017;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlX = 0x0018;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlY = 0x0019;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlZ = 0x001a;

    // Extended key codes
    /// <summary>Key code.</summary>
    public const ushort kbEsc = 0x011b;
    /// <summary>Key code.</summary>
    public const ushort kbAltSpace = 0x0200;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlIns = 0x0400;
    /// <summary>Key code.</summary>
    public const ushort kbShiftIns = 0x0500;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlDel = 0x0600;
    /// <summary>Key code.</summary>
    public const ushort kbShiftDel = 0x0700;
    /// <summary>Key code.</summary>
    public const ushort kbBack = 0x0e08;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlBack = 0x0e7f;
    /// <summary>Key code.</summary>
    public const ushort kbShiftTab = 0x0f00;
    /// <summary>Key code.</summary>
    public const ushort kbTab = 0x0f09;
    /// <summary>Key code.</summary>
    public const ushort kbAltQ = 0x1000;
    /// <summary>Key code.</summary>
    public const ushort kbAltW = 0x1100;
    /// <summary>Key code.</summary>
    public const ushort kbAltE = 0x1200;
    /// <summary>Key code.</summary>
    public const ushort kbAltR = 0x1300;
    /// <summary>Key code.</summary>
    public const ushort kbAltT = 0x1400;
    /// <summary>Key code.</summary>
    public const ushort kbAltY = 0x1500;
    /// <summary>Key code.</summary>
    public const ushort kbAltU = 0x1600;
    /// <summary>Key code.</summary>
    public const ushort kbAltI = 0x1700;
    /// <summary>Key code.</summary>
    public const ushort kbAltO = 0x1800;
    /// <summary>Key code.</summary>
    public const ushort kbAltP = 0x1900;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlEnter = 0x1c0a;
    /// <summary>Key code.</summary>
    public const ushort kbEnter = 0x1c0d;
    /// <summary>Key code.</summary>
    public const ushort kbAltA = 0x1e00;
    /// <summary>Key code.</summary>
    public const ushort kbAltS = 0x1f00;
    /// <summary>Key code.</summary>
    public const ushort kbAltD = 0x2000;
    /// <summary>Key code.</summary>
    public const ushort kbAltF = 0x2100;
    /// <summary>Key code.</summary>
    public const ushort kbAltG = 0x2200;
    /// <summary>Key code.</summary>
    public const ushort kbAltH = 0x2300;
    /// <summary>Key code.</summary>
    public const ushort kbAltJ = 0x2400;
    /// <summary>Key code.</summary>
    public const ushort kbAltK = 0x2500;
    /// <summary>Key code.</summary>
    public const ushort kbAltL = 0x2600;
    /// <summary>Key code.</summary>
    public const ushort kbAltZ = 0x2c00;
    /// <summary>Key code.</summary>
    public const ushort kbAltX = 0x2d00;
    /// <summary>Key code.</summary>
    public const ushort kbAltC = 0x2e00;
    /// <summary>Key code.</summary>
    public const ushort kbAltV = 0x2f00;
    /// <summary>Key code.</summary>
    public const ushort kbAltB = 0x3000;
    /// <summary>Key code.</summary>
    public const ushort kbAltN = 0x3100;
    /// <summary>Key code.</summary>
    public const ushort kbAltM = 0x3200;
    /// <summary>Key code.</summary>
    public const ushort kbF1 = 0x3b00;
    /// <summary>Key code.</summary>
    public const ushort kbF2 = 0x3c00;
    /// <summary>Key code.</summary>
    public const ushort kbF3 = 0x3d00;
    /// <summary>Key code.</summary>
    public const ushort kbF4 = 0x3e00;
    /// <summary>Key code.</summary>
    public const ushort kbF5 = 0x3f00;
    /// <summary>Key code.</summary>
    public const ushort kbF6 = 0x4000;
    /// <summary>Key code.</summary>
    public const ushort kbF7 = 0x4100;
    /// <summary>Key code.</summary>
    public const ushort kbF8 = 0x4200;
    /// <summary>Key code.</summary>
    public const ushort kbF9 = 0x4300;
    /// <summary>Key code.</summary>
    public const ushort kbF10 = 0x4400;
    /// <summary>Key code.</summary>
    public const ushort kbHome = 0x4700;
    /// <summary>Key code.</summary>
    public const ushort kbUp = 0x4800;
    /// <summary>Key code.</summary>
    public const ushort kbPgUp = 0x4900;
    /// <summary>Key code.</summary>
    public const ushort kbGrayMinus = 0x4a2d;
    /// <summary>Key code.</summary>
    public const ushort kbLeft = 0x4b00;
    /// <summary>Key code.</summary>
    public const ushort kbRight = 0x4d00;
    /// <summary>Key code.</summary>
    public const ushort kbGrayPlus = 0x4e2b;
    /// <summary>Key code.</summary>
    public const ushort kbEnd = 0x4f00;
    /// <summary>Key code.</summary>
    public const ushort kbDown = 0x5000;
    /// <summary>Key code.</summary>
    public const ushort kbPgDn = 0x5100;
    /// <summary>Key code.</summary>
    public const ushort kbIns = 0x5200;
    /// <summary>Key code.</summary>
    public const ushort kbDel = 0x5300;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF1 = 0x5400;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF2 = 0x5500;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF3 = 0x5600;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF4 = 0x5700;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF5 = 0x5800;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF6 = 0x5900;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF7 = 0x5a00;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF8 = 0x5b00;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF9 = 0x5c00;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF10 = 0x5d00;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF1 = 0x5e00;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF2 = 0x5f00;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF3 = 0x6000;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF4 = 0x6100;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF5 = 0x6200;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF6 = 0x6300;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF7 = 0x6400;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF8 = 0x6500;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF9 = 0x6600;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF10 = 0x6700;
    /// <summary>Key code.</summary>
    public const ushort kbAltF1 = 0x6800;
    /// <summary>Key code.</summary>
    public const ushort kbAltF2 = 0x6900;
    /// <summary>Key code.</summary>
    public const ushort kbAltF3 = 0x6a00;
    /// <summary>Key code.</summary>
    public const ushort kbAltF4 = 0x6b00;
    /// <summary>Key code.</summary>
    public const ushort kbAltF5 = 0x6c00;
    /// <summary>Key code.</summary>
    public const ushort kbAltF6 = 0x6d00;
    /// <summary>Key code.</summary>
    public const ushort kbAltF7 = 0x6e00;
    /// <summary>Key code.</summary>
    public const ushort kbAltF8 = 0x6f00;
    /// <summary>Key code.</summary>
    public const ushort kbAltF9 = 0x7000;
    /// <summary>Key code.</summary>
    public const ushort kbAltF10 = 0x7100;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlPrtSc = 0x7200;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlLeft = 0x7300;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlRight = 0x7400;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlEnd = 0x7500;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlPgDn = 0x7600;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlHome = 0x7700;
    /// <summary>Key code.</summary>
    public const ushort kbAlt1 = 0x7800;
    /// <summary>Key code.</summary>
    public const ushort kbAlt2 = 0x7900;
    /// <summary>Key code.</summary>
    public const ushort kbAlt3 = 0x7a00;
    /// <summary>Key code.</summary>
    public const ushort kbAlt4 = 0x7b00;
    /// <summary>Key code.</summary>
    public const ushort kbAlt5 = 0x7c00;
    /// <summary>Key code.</summary>
    public const ushort kbAlt6 = 0x7d00;
    /// <summary>Key code.</summary>
    public const ushort kbAlt7 = 0x7e00;
    /// <summary>Key code.</summary>
    public const ushort kbAlt8 = 0x7f00;
    /// <summary>Key code.</summary>
    public const ushort kbAlt9 = 0x8000;
    /// <summary>Key code.</summary>
    public const ushort kbAlt0 = 0x8100;
    /// <summary>Key code.</summary>
    public const ushort kbAltMinus = 0x8200;
    /// <summary>Key code.</summary>
    public const ushort kbAltEqual = 0x8300;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlPgUp = 0x8400;
    /// <summary>Key code.</summary>
    public const ushort kbNoKey = 0x0000;

    // Additional extended key codes (not reported in 16-bit mode)
    /// <summary>Key code.</summary>
    public const ushort kbAltEsc = 0x0100;
    /// <summary>Key code.</summary>
    public const ushort kbAltBack = 0x0e00;
    /// <summary>Key code.</summary>
    public const ushort kbF11 = 0x8500;
    /// <summary>Key code.</summary>
    public const ushort kbF12 = 0x8600;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF11 = 0x8700;
    /// <summary>Key code.</summary>
    public const ushort kbShiftF12 = 0x8800;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF11 = 0x8900;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlF12 = 0x8a00;
    /// <summary>Key code.</summary>
    public const ushort kbAltF11 = 0x8b00;
    /// <summary>Key code.</summary>
    public const ushort kbAltF12 = 0x8c00;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlUp = 0x8d00;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlDown = 0x9100;
    /// <summary>Key code.</summary>
    public const ushort kbCtrlTab = 0x9400;
    /// <summary>Key code.</summary>
    public const ushort kbAltHome = 0x9700;
    /// <summary>Key code.</summary>
    public const ushort kbAltUp = 0x9800;
    /// <summary>Key code.</summary>
    public const ushort kbAltPgUp = 0x9900;
    /// <summary>Key code.</summary>
    public const ushort kbAltLeft = 0x9b00;
    /// <summary>Key code.</summary>
    public const ushort kbAltRight = 0x9d00;
    /// <summary>Key code.</summary>
    public const ushort kbAltEnd = 0x9f00;
    /// <summary>Key code.</summary>
    public const ushort kbAltDown = 0xa000;
    /// <summary>Key code.</summary>
    public const ushort kbAltPgDn = 0xa100;
    /// <summary>Key code.</summary>
    public const ushort kbAltIns = 0xa200;
    /// <summary>Key code.</summary>
    public const ushort kbAltDel = 0xa300;
    /// <summary>Key code.</summary>
    public const ushort kbAltTab = 0xa500;
    /// <summary>Key code.</summary>
    public const ushort kbAltEnter = 0xa600;

    // Keyboard state and shift masks (the 32-bit values, which follow the Win32 console API).
    // Left and right Ctrl and Alt are only distinguished on Windows. kbScrollState,
    // kbNumState, kbCapsState and kbEnhanced are only reported on Windows.

    /// <summary>Left Shift pressed (not distinguished from right Shift).</summary>
    public const ushort kbLeftShift = 0x0010;

    /// <summary>Right Shift pressed (not distinguished from left Shift).</summary>
    public const ushort kbRightShift = 0x0010;

    /// <summary>Either Shift pressed.</summary>
    public const ushort kbShift = kbLeftShift | kbRightShift;

    /// <summary>Left Ctrl pressed.</summary>
    public const ushort kbLeftCtrl = 0x0008;

    /// <summary>Right Ctrl pressed.</summary>
    public const ushort kbRightCtrl = 0x0004;

    /// <summary>Either Ctrl pressed.</summary>
    public const ushort kbCtrlShift = kbLeftCtrl | kbRightCtrl;

    /// <summary>Left Alt pressed.</summary>
    public const ushort kbLeftAlt = 0x0002;

    /// <summary>Right Alt pressed.</summary>
    public const ushort kbRightAlt = 0x0001;

    /// <summary>Either Alt pressed.</summary>
    public const ushort kbAltShift = kbLeftAlt | kbRightAlt;

    /// <summary>Scroll Lock on.</summary>
    public const ushort kbScrollState = 0x0040;

    /// <summary>Num Lock on.</summary>
    public const ushort kbNumState = 0x0020;

    /// <summary>Caps Lock on.</summary>
    public const ushort kbCapsState = 0x0080;

    /// <summary>Enhanced (extended) key.</summary>
    public const ushort kbEnhanced = 0x0100;

    /// <summary>Insert mode on.</summary>
    public const ushort kbInsState = 0x0200;

    /// <summary>The key event is part of pasted text.</summary>
    public const ushort kbPaste = 0x0400;
}
