using System.IO;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Objects;

/// <summary>Writes key codes, event codes and event flags by their constant names, for debugging.</summary>
/// Ported from source/tvision/prntcnst.cpp, include/tvision/util.h (printKeyCode, printControlKeyState,
/// printEventCode, printMouseButtonState, printMouseWheelState, printMouseEventFlags).
public static class PrintConstants
{
    private static readonly (ushort Value, string Name)[] _keyCodes =
    [
        (kbCtrlA, "kbCtrlA"), (kbCtrlB, "kbCtrlB"), (kbCtrlC, "kbCtrlC"),
        (kbCtrlD, "kbCtrlD"), (kbCtrlE, "kbCtrlE"), (kbCtrlF, "kbCtrlF"),
        (kbCtrlG, "kbCtrlG"), (kbCtrlH, "kbCtrlH"), (kbCtrlI, "kbCtrlI"),
        (kbCtrlJ, "kbCtrlJ"), (kbCtrlK, "kbCtrlK"), (kbCtrlL, "kbCtrlL"),
        (kbCtrlM, "kbCtrlM"), (kbCtrlN, "kbCtrlN"), (kbCtrlO, "kbCtrlO"),
        (kbCtrlP, "kbCtrlP"), (kbCtrlQ, "kbCtrlQ"), (kbCtrlR, "kbCtrlR"),
        (kbCtrlS, "kbCtrlS"), (kbCtrlT, "kbCtrlT"), (kbCtrlU, "kbCtrlU"),
        (kbCtrlV, "kbCtrlV"), (kbCtrlW, "kbCtrlW"), (kbCtrlX, "kbCtrlX"),
        (kbCtrlY, "kbCtrlY"), (kbCtrlZ, "kbCtrlZ"), (kbEsc, "kbEsc"),
        (kbAltSpace, "kbAltSpace"), (kbCtrlIns, "kbCtrlIns"), (kbShiftIns, "kbShiftIns"),
        (kbCtrlDel, "kbCtrlDel"), (kbShiftDel, "kbShiftDel"), (kbBack, "kbBack"),
        (kbCtrlBack, "kbCtrlBack"), (kbShiftTab, "kbShiftTab"), (kbTab, "kbTab"),
        (kbAltQ, "kbAltQ"), (kbAltW, "kbAltW"), (kbAltE, "kbAltE"),
        (kbAltR, "kbAltR"), (kbAltT, "kbAltT"), (kbAltY, "kbAltY"),
        (kbAltU, "kbAltU"), (kbAltI, "kbAltI"), (kbAltO, "kbAltO"),
        (kbAltP, "kbAltP"), (kbCtrlEnter, "kbCtrlEnter"), (kbEnter, "kbEnter"),
        (kbAltA, "kbAltA"), (kbAltS, "kbAltS"), (kbAltD, "kbAltD"),
        (kbAltF, "kbAltF"), (kbAltG, "kbAltG"), (kbAltH, "kbAltH"),
        (kbAltJ, "kbAltJ"), (kbAltK, "kbAltK"), (kbAltL, "kbAltL"),
        (kbAltZ, "kbAltZ"), (kbAltX, "kbAltX"), (kbAltC, "kbAltC"),
        (kbAltV, "kbAltV"), (kbAltB, "kbAltB"), (kbAltN, "kbAltN"),
        (kbAltM, "kbAltM"), (kbF1, "kbF1"), (kbF2, "kbF2"),
        (kbF3, "kbF3"), (kbF4, "kbF4"), (kbF5, "kbF5"),
        (kbF6, "kbF6"), (kbF7, "kbF7"), (kbF8, "kbF8"),
        (kbF9, "kbF9"), (kbF10, "kbF10"), (kbHome, "kbHome"),
        (kbUp, "kbUp"), (kbPgUp, "kbPgUp"), (kbGrayMinus, "kbGrayMinus"),
        (kbLeft, "kbLeft"), (kbRight, "kbRight"), (kbGrayPlus, "kbGrayPlus"),
        (kbEnd, "kbEnd"), (kbDown, "kbDown"), (kbPgDn, "kbPgDn"),
        (kbIns, "kbIns"), (kbDel, "kbDel"), (kbShiftF1, "kbShiftF1"),
        (kbShiftF2, "kbShiftF2"), (kbShiftF3, "kbShiftF3"), (kbShiftF4, "kbShiftF4"),
        (kbShiftF5, "kbShiftF5"), (kbShiftF6, "kbShiftF6"), (kbShiftF7, "kbShiftF7"),
        (kbShiftF8, "kbShiftF8"), (kbShiftF9, "kbShiftF9"), (kbShiftF10, "kbShiftF10"),
        (kbCtrlF1, "kbCtrlF1"), (kbCtrlF2, "kbCtrlF2"), (kbCtrlF3, "kbCtrlF3"),
        (kbCtrlF4, "kbCtrlF4"), (kbCtrlF5, "kbCtrlF5"), (kbCtrlF6, "kbCtrlF6"),
        (kbCtrlF7, "kbCtrlF7"), (kbCtrlF8, "kbCtrlF8"), (kbCtrlF9, "kbCtrlF9"),
        (kbCtrlF10, "kbCtrlF10"), (kbAltF1, "kbAltF1"), (kbAltF2, "kbAltF2"),
        (kbAltF3, "kbAltF3"), (kbAltF4, "kbAltF4"), (kbAltF5, "kbAltF5"),
        (kbAltF6, "kbAltF6"), (kbAltF7, "kbAltF7"), (kbAltF8, "kbAltF8"),
        (kbAltF9, "kbAltF9"), (kbAltF10, "kbAltF10"), (kbCtrlPrtSc, "kbCtrlPrtSc"),
        (kbCtrlLeft, "kbCtrlLeft"), (kbCtrlRight, "kbCtrlRight"), (kbCtrlEnd, "kbCtrlEnd"),
        (kbCtrlPgDn, "kbCtrlPgDn"), (kbCtrlHome, "kbCtrlHome"), (kbAlt1, "kbAlt1"),
        (kbAlt2, "kbAlt2"), (kbAlt3, "kbAlt3"), (kbAlt4, "kbAlt4"),
        (kbAlt5, "kbAlt5"), (kbAlt6, "kbAlt6"), (kbAlt7, "kbAlt7"),
        (kbAlt8, "kbAlt8"), (kbAlt9, "kbAlt9"), (kbAlt0, "kbAlt0"),
        (kbAltMinus, "kbAltMinus"), (kbAltEqual, "kbAltEqual"), (kbCtrlPgUp, "kbCtrlPgUp"),
        (kbNoKey, "kbNoKey"), (kbAltEsc, "kbAltEsc"), (kbAltBack, "kbAltBack"),
        (kbF11, "kbF11"), (kbF12, "kbF12"), (kbShiftF11, "kbShiftF11"),
        (kbShiftF12, "kbShiftF12"), (kbCtrlF11, "kbCtrlF11"), (kbCtrlF12, "kbCtrlF12"),
        (kbAltF11, "kbAltF11"), (kbAltF12, "kbAltF12"), (kbCtrlUp, "kbCtrlUp"),
        (kbCtrlDown, "kbCtrlDown"), (kbCtrlTab, "kbCtrlTab"), (kbAltHome, "kbAltHome"),
        (kbAltUp, "kbAltUp"), (kbAltPgUp, "kbAltPgUp"), (kbAltLeft, "kbAltLeft"),
        (kbAltRight, "kbAltRight"), (kbAltEnd, "kbAltEnd"), (kbAltDown, "kbAltDown"),
        (kbAltPgDn, "kbAltPgDn"), (kbAltIns, "kbAltIns"), (kbAltDel, "kbAltDel"),
        (kbAltTab, "kbAltTab"), (kbAltEnter, "kbAltEnter"),
    ];

    private static readonly (ushort Value, string Name)[] _controlKeyStateFlags =
    [
        (kbShift, "kbShift"), (kbScrollState, "kbScrollState"), (kbLeftCtrl, "kbLeftCtrl"),
        (kbRightCtrl, "kbRightCtrl"), (kbLeftAlt, "kbLeftAlt"), (kbRightAlt, "kbRightAlt"),
        (kbNumState, "kbNumState"), (kbCapsState, "kbCapsState"), (kbInsState, "kbInsState"),
        (kbEnhanced, "kbEnhanced"), (kbPaste, "kbPaste"),
    ];

    private static readonly (ushort Value, string Name)[] _eventCodes =
    [
        (evNothing, "evNothing"), (evMouseDown, "evMouseDown"), (evMouseUp, "evMouseUp"),
        (evMouseMove, "evMouseMove"), (evMouseAuto, "evMouseAuto"), (evMouseWheel, "evMouseWheel"),
        (evKeyDown, "evKeyDown"), (evCommand, "evCommand"), (evBroadcast, "evBroadcast"),
        (evMouse, "evMouse"), (evKeyboard, "evKeyboard"), (evMessage, "evMessage"),
    ];

    private static readonly (ushort Value, string Name)[] _mouseButtonFlags =
    [
        (mbLeftButton, "mbLeftButton"), (mbRightButton, "mbRightButton"), (mbMiddleButton, "mbMiddleButton"),
    ];

    private static readonly (ushort Value, string Name)[] _mouseWheelFlags =
    [
        (mwUp, "mwUp"), (mwDown, "mwDown"), (mwLeft, "mwLeft"),
        (mwRight, "mwRight"),
    ];

    private static readonly (ushort Value, string Name)[] _mouseEventFlags =
    [
        (meMouseMoved, "meMouseMoved"), (meDoubleClick, "meDoubleClick"), (meTripleClick, "meTripleClick"),
    ];

    private static void PrintFlags(TextWriter os, ushort flags, (ushort Value, string Name)[] constants)
    {
        ushort foundFlags = 0;
        foreach (var (value, name) in constants)
        {
            if ((flags & value) != 0)
            {
                if (foundFlags != 0)
                    os.Write(" | ");
                os.Write(name);
                foundFlags |= (ushort)(flags & value);
            }
        }
        if (foundFlags == 0 || foundFlags != flags)
        {
            if (foundFlags != 0)
                os.Write(" | ");
            os.Write($"0x{flags & ~foundFlags:x4}");
        }
    }

    private static void PrintCode(TextWriter os, ushort code, (ushort Value, string Name)[] constants)
    {
        foreach (var (value, name) in constants)
        {
            if (code == value)
            {
                os.Write(name);
                return;
            }
        }
        os.Write($"0x{code:x4}");
    }

    /// <summary>Writes the name of <paramref name="keyCode"/> (<c>kbXxx</c>), or its hex value.</summary>
    public static void PrintKeyCode(TextWriter os, ushort keyCode) => PrintCode(os, keyCode, _keyCodes);

    /// <summary>Writes the <c>kbXxx</c> modifier and state flags set in <paramref name="controlKeyState"/>.</summary>
    public static void PrintControlKeyState(TextWriter os, ushort controlKeyState) =>
        PrintFlags(os, controlKeyState, _controlKeyStateFlags);

    /// <summary>Writes the name of <paramref name="eventCode"/> (<c>evXxx</c>), or its hex value.</summary>
    public static void PrintEventCode(TextWriter os, ushort eventCode) => PrintCode(os, eventCode, _eventCodes);

    /// <summary>Writes the <c>mbXxx</c> flags set in <paramref name="buttonState"/>.</summary>
    public static void PrintMouseButtonState(TextWriter os, ushort buttonState) =>
        PrintFlags(os, buttonState, _mouseButtonFlags);

    /// <summary>Writes the <c>mwXxx</c> flags set in <paramref name="wheelState"/>.</summary>
    public static void PrintMouseWheelState(TextWriter os, ushort wheelState) =>
        PrintFlags(os, wheelState, _mouseWheelFlags);

    /// <summary>Writes the <c>meXxx</c> flags set in <paramref name="eventFlags"/>.</summary>
    public static void PrintMouseEventFlags(TextWriter os, ushort eventFlags) =>
        PrintFlags(os, eventFlags, _mouseEventFlags);
}
