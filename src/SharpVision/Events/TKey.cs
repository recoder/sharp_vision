// Ported from include/tvision/tkeys.h and source/tvision/tkey.cpp (class TKey).

using System;

using static SharpVision.Events.KeyCodes;

namespace SharpVision.Events;

/// <summary>
/// A key combination: a key code plus a mask of Shift, Ctrl and Alt modifiers. Used to
/// define menu hotkeys and to examine <c>evKeyDown</c> events.
/// </summary>
/// <remarks>
/// Left and right modifier keys are not distinguished. Since some key codes already imply a
/// modifier, a combination can be written in several ways; <see cref="TKey"/> normalizes them
/// so they compare equal. For example:
/// <code>
/// kbCtrlA == new TKey('A', kbCtrlShift)
/// new TKey(kbCtrlTab, kbShift) == new TKey(kbTab, kbShift | kbCtrlShift)
/// new TKey(kbAltDel, kbCtrlShift) == new TKey(kbCtrlDel, kbAltShift)
/// </code>
/// </remarks>
public readonly struct TKey : IEquatable<TKey>
{
    private readonly record struct LookupEntry(ushort NormalKeyCode, ushort ShiftState);

    private static readonly LookupEntry[] _ctrlKeyLookup =
    [
        /* 0x0000 */ new(0, 0),
        /* 0x0001 */ new((ushort)'A', kbCtrlShift),
        /* 0x0002 */ new((ushort)'B', kbCtrlShift),
        /* 0x0003 */ new((ushort)'C', kbCtrlShift),
        /* 0x0004 */ new((ushort)'D', kbCtrlShift),
        /* 0x0005 */ new((ushort)'E', kbCtrlShift),
        /* 0x0006 */ new((ushort)'F', kbCtrlShift),
        /* 0x0007 */ new((ushort)'G', kbCtrlShift),
        /* 0x0008 */ new((ushort)'H', kbCtrlShift),
        /* 0x0009 */ new((ushort)'I', kbCtrlShift),
        /* 0x000A */ new((ushort)'J', kbCtrlShift),
        /* 0x000B */ new((ushort)'K', kbCtrlShift),
        /* 0x000C */ new((ushort)'L', kbCtrlShift),
        /* 0x000D */ new((ushort)'M', kbCtrlShift),
        /* 0x000E */ new((ushort)'N', kbCtrlShift),
        /* 0x000F */ new((ushort)'O', kbCtrlShift),
        /* 0x0010 */ new((ushort)'P', kbCtrlShift),
        /* 0x0011 */ new((ushort)'Q', kbCtrlShift),
        /* 0x0012 */ new((ushort)'R', kbCtrlShift),
        /* 0x0013 */ new((ushort)'S', kbCtrlShift),
        /* 0x0014 */ new((ushort)'T', kbCtrlShift),
        /* 0x0015 */ new((ushort)'U', kbCtrlShift),
        /* 0x0016 */ new((ushort)'V', kbCtrlShift),
        /* 0x0017 */ new((ushort)'W', kbCtrlShift),
        /* 0x0018 */ new((ushort)'X', kbCtrlShift),
        /* 0x0019 */ new((ushort)'Y', kbCtrlShift),
        /* 0x001A */ new((ushort)'Z', kbCtrlShift),
    ];

    private static readonly LookupEntry[] _extKeyLookup =
    [
        /* 0x0000 */ new(0, 0),
        /* 0x0100 */ new(kbEsc, kbAltShift),
        /* 0x0200 */ new((ushort)' ', kbAltShift),
        /* 0x0300 */ new(0, 0),
        /* 0x0400 */ new(kbIns, kbCtrlShift),
        /* 0x0500 */ new(kbIns, kbShift),
        /* 0x0600 */ new(kbDel, kbCtrlShift),
        /* 0x0700 */ new(kbDel, kbShift),
        /* 0x0800 */ new(0, 0),
        /* 0x0900 */ new(0, 0),
        /* 0x0A00 */ new(0, 0),
        /* 0x0B00 */ new(0, 0),
        /* 0x0C00 */ new(0, 0),
        /* 0x0D00 */ new(0, 0),
        /* 0x0E00 */ new(kbBack, kbAltShift),
        /* 0x0F00 */ new(kbTab, kbShift),
        /* 0x1000 */ new((ushort)'Q', kbAltShift),
        /* 0x1100 */ new((ushort)'W', kbAltShift),
        /* 0x1200 */ new((ushort)'E', kbAltShift),
        /* 0x1300 */ new((ushort)'R', kbAltShift),
        /* 0x1400 */ new((ushort)'T', kbAltShift),
        /* 0x1500 */ new((ushort)'Y', kbAltShift),
        /* 0x1600 */ new((ushort)'U', kbAltShift),
        /* 0x1700 */ new((ushort)'I', kbAltShift),
        /* 0x1800 */ new((ushort)'O', kbAltShift),
        /* 0x1900 */ new((ushort)'P', kbAltShift),
        /* 0x1A00 */ new(0, 0),
        /* 0x1B00 */ new(0, 0),
        /* 0x1C00 */ new(0, 0),
        /* 0x1D00 */ new(0, 0),
        /* 0x1E00 */ new((ushort)'A', kbAltShift),
        /* 0x1F00 */ new((ushort)'S', kbAltShift),
        /* 0x2000 */ new((ushort)'D', kbAltShift),
        /* 0x2100 */ new((ushort)'F', kbAltShift),
        /* 0x2200 */ new((ushort)'G', kbAltShift),
        /* 0x2300 */ new((ushort)'H', kbAltShift),
        /* 0x2400 */ new((ushort)'J', kbAltShift),
        /* 0x2500 */ new((ushort)'K', kbAltShift),
        /* 0x2600 */ new((ushort)'L', kbAltShift),
        /* 0x2700 */ new(0, 0),
        /* 0x2800 */ new(0, 0),
        /* 0x2900 */ new(0, 0),
        /* 0x2A00 */ new(0, 0),
        /* 0x2B00 */ new(0, 0),
        /* 0x2C00 */ new((ushort)'Z', kbAltShift),
        /* 0x2D00 */ new((ushort)'X', kbAltShift),
        /* 0x2E00 */ new((ushort)'C', kbAltShift),
        /* 0x2F00 */ new((ushort)'V', kbAltShift),
        /* 0x3000 */ new((ushort)'B', kbAltShift),
        /* 0x3100 */ new((ushort)'N', kbAltShift),
        /* 0x3200 */ new((ushort)'M', kbAltShift),
        /* 0x3300 */ new(0, 0),
        /* 0x3400 */ new(0, 0),
        /* 0x3500 */ new((ushort)'/', kbCtrlShift),
        /* 0x3600 */ new(0, 0),
        /* 0x3700 */ new((ushort)'*', kbCtrlShift),
        /* 0x3800 */ new(0, 0),
        /* 0x3900 */ new(0, 0),
        /* 0x3A00 */ new(0, 0),
        /* 0x3B00 */ new(kbF1, 0),
        /* 0x3C00 */ new(kbF2, 0),
        /* 0x3D00 */ new(kbF3, 0),
        /* 0x3E00 */ new(kbF4, 0),
        /* 0x3F00 */ new(kbF5, 0),
        /* 0x4000 */ new(kbF6, 0),
        /* 0x4100 */ new(kbF7, 0),
        /* 0x4200 */ new(kbF8, 0),
        /* 0x4300 */ new(kbF9, 0),
        /* 0x4400 */ new(kbF10, 0),
        /* 0x4500 */ new(0, 0),
        /* 0x4600 */ new(0, 0),
        /* 0x4700 */ new(kbHome, 0),
        /* 0x4800 */ new(kbUp, 0),
        /* 0x4900 */ new(kbPgUp, 0),
        /* 0x4A00 */ new((ushort)'-', kbCtrlShift),
        /* 0x4B00 */ new(kbLeft, 0),
        /* 0x4C00 */ new(0, 0),
        /* 0x4D00 */ new(kbRight, 0),
        /* 0x4E00 */ new((ushort)'+', kbCtrlShift),
        /* 0x4F00 */ new(kbEnd, 0),
        /* 0x5000 */ new(kbDown, 0),
        /* 0x5100 */ new(kbPgDn, 0),
        /* 0x5200 */ new(kbIns, 0),
        /* 0x5300 */ new(kbDel, 0),
        /* 0x5400 */ new(kbF1, kbShift),
        /* 0x5500 */ new(kbF2, kbShift),
        /* 0x5600 */ new(kbF3, kbShift),
        /* 0x5700 */ new(kbF4, kbShift),
        /* 0x5800 */ new(kbF5, kbShift),
        /* 0x5900 */ new(kbF6, kbShift),
        /* 0x5A00 */ new(kbF7, kbShift),
        /* 0x5B00 */ new(kbF8, kbShift),
        /* 0x5C00 */ new(kbF9, kbShift),
        /* 0x5D00 */ new(kbF10, kbShift),
        /* 0x5E00 */ new(kbF1, kbCtrlShift),
        /* 0x5F00 */ new(kbF2, kbCtrlShift),
        /* 0x6000 */ new(kbF3, kbCtrlShift),
        /* 0x6100 */ new(kbF4, kbCtrlShift),
        /* 0x6200 */ new(kbF5, kbCtrlShift),
        /* 0x6300 */ new(kbF6, kbCtrlShift),
        /* 0x6400 */ new(kbF7, kbCtrlShift),
        /* 0x6500 */ new(kbF8, kbCtrlShift),
        /* 0x6600 */ new(kbF9, kbCtrlShift),
        /* 0x6700 */ new(kbF10, kbCtrlShift),
        /* 0x6800 */ new(kbF1, kbAltShift),
        /* 0x6900 */ new(kbF2, kbAltShift),
        /* 0x6A00 */ new(kbF3, kbAltShift),
        /* 0x6B00 */ new(kbF4, kbAltShift),
        /* 0x6C00 */ new(kbF5, kbAltShift),
        /* 0x6D00 */ new(kbF6, kbAltShift),
        /* 0x6E00 */ new(kbF7, kbAltShift),
        /* 0x6F00 */ new(kbF8, kbAltShift),
        /* 0x7000 */ new(kbF9, kbAltShift),
        /* 0x7100 */ new(kbF10, kbAltShift),
        /* 0x7200 */ new(kbCtrlPrtSc, kbCtrlShift),
        /* 0x7300 */ new(kbLeft, kbCtrlShift),
        /* 0x7400 */ new(kbRight, kbCtrlShift),
        /* 0x7500 */ new(kbEnd, kbCtrlShift),
        /* 0x7600 */ new(kbPgDn, kbCtrlShift),
        /* 0x7700 */ new(kbHome, kbCtrlShift),
        /* 0x7800 */ new((ushort)'1', kbAltShift),
        /* 0x7900 */ new((ushort)'2', kbAltShift),
        /* 0x7A00 */ new((ushort)'3', kbAltShift),
        /* 0x7B00 */ new((ushort)'4', kbAltShift),
        /* 0x7C00 */ new((ushort)'5', kbAltShift),
        /* 0x7D00 */ new((ushort)'6', kbAltShift),
        /* 0x7E00 */ new((ushort)'7', kbAltShift),
        /* 0x7F00 */ new((ushort)'8', kbAltShift),
        /* 0x8000 */ new((ushort)'9', kbAltShift),
        /* 0x8100 */ new((ushort)'0', kbAltShift),
        /* 0x8200 */ new((ushort)'-', kbAltShift),
        /* 0x8300 */ new((ushort)'=', kbAltShift),
        /* 0x8400 */ new(kbPgUp, kbCtrlShift),
        /* 0x8500 */ new(kbF11, 0),
        /* 0x8600 */ new(kbF12, 0),
        /* 0x8700 */ new(kbF11, kbShift),
        /* 0x8800 */ new(kbF12, kbShift),
        /* 0x8900 */ new(kbF11, kbCtrlShift),
        /* 0x8A00 */ new(kbF12, kbCtrlShift),
        /* 0x8B00 */ new(kbF11, kbAltShift),
        /* 0x8C00 */ new(kbF12, kbAltShift),
        /* 0x8D00 */ new(kbUp, kbCtrlShift),
        /* 0x8E00 */ new(0, 0),
        /* 0x8F00 */ new(0, 0),
        /* 0x9000 */ new(0, 0),
        /* 0x9100 */ new(kbDown, kbCtrlShift),
        /* 0x9200 */ new(0, 0),
        /* 0x9300 */ new(0, 0),
        /* 0x9400 */ new(kbTab, kbCtrlShift),
        /* 0x9500 */ new(0, 0),
        /* 0x9600 */ new(0, 0),
        /* 0x9700 */ new(kbHome, kbAltShift),
        /* 0x9800 */ new(kbUp, kbAltShift),
        /* 0x9900 */ new(kbPgUp, kbAltShift),
        /* 0x9A00 */ new(0, 0),
        /* 0x9B00 */ new(kbLeft, kbAltShift),
        /* 0x9C00 */ new(0, 0),
        /* 0x9D00 */ new(kbRight, kbAltShift),
        /* 0x9E00 */ new(0, 0),
        /* 0x9F00 */ new(kbEnd, kbAltShift),
        /* 0xA000 */ new(kbDown, kbAltShift),
        /* 0xA100 */ new(kbPgDn, kbAltShift),
        /* 0xA200 */ new(kbIns, kbAltShift),
        /* 0xA300 */ new(kbDel, kbAltShift),
        /* 0xA400 */ new(0, 0),
        /* 0xA500 */ new(kbTab, kbAltShift),
        /* 0xA600 */ new(kbEnter, kbAltShift),
    ];

    private static readonly LookupEntry _kbCtrlBackEntry = new(kbBack, kbCtrlShift);
    private static readonly LookupEntry _kbCtrlEnterEntry = new(kbEnter, kbCtrlShift);

    /// <summary>The normalized key code.</summary>
    public ushort Code { get; }

    /// <summary>The normalized modifiers: a combination of <c>kbShift</c>, <c>kbCtrlShift</c> and <c>kbAltShift</c>.</summary>
    public ushort Mods { get; }

    /// <summary>Creates a normalized key combination from a key code and a keyboard state mask.</summary>
    public TKey(ushort keyCode, ushort shiftState = 0)
    {
        var code = keyCode;
        var mods = (ushort)(
            ((shiftState & kbShift) != 0 ? kbShift : 0) |
            ((shiftState & kbCtrlShift) != 0 ? kbCtrlShift : 0) |
            ((shiftState & kbAltShift) != 0 ? kbAltShift : 0));
        var scanCode = (byte)(keyCode >> 8);
        var charCode = (byte)keyCode;

        LookupEntry? entry = null;
        if (keyCode <= kbCtrlZ || IsRawCtrlKey(scanCode, charCode))
            entry = _ctrlKeyLookup[charCode];
        else if (charCode == 0)
        {
            if (scanCode < _extKeyLookup.Length)
                entry = _extKeyLookup[scanCode];
        }
        else if (IsPrintableCharacter(charCode))
        {
            if (charCode >= 'a' && charCode <= 'z')
                code = (ushort)(charCode - 'a' + 'A');
            else if (!IsKeypadCharacter(scanCode))
                code &= 0xFF;
        }
        else
        {
            switch (keyCode)
            {
                case kbCtrlBack: entry = _kbCtrlBackEntry; break;
                case kbCtrlEnter: entry = _kbCtrlEnterEntry; break;
            }
        }

        if (entry is { } e)
        {
            mods |= e.ShiftState;
            if (e.NormalKeyCode != 0)
                code = e.NormalKeyCode;
        }

        Code = code;
        Mods = code != kbNoKey ? mods : (ushort)0;
    }

    // Whether the key is a BIOS Ctrl+letter, i.e. the character code is the letter's
    // control code and the scan code is that letter's key.
    private static bool IsRawCtrlKey(byte scanCode, byte charCode)
    {
        const string scanKeys = "QWERTYUIOP\0\0\0\0ASDFGHJKL\0\0\0\0\0ZXCVBNM";
        return scanCode >= 16 && scanCode < 16 + 35
            && scanCode - 16 < scanKeys.Length
            && scanKeys[scanCode - 16] == charCode - 1 + 'A';
    }

    private static bool IsPrintableCharacter(byte charCode) =>
        charCode >= ' ' && charCode != 0x7F && charCode != 0xFF;

    private static bool IsKeypadCharacter(byte scanCode) =>
        scanCode is 0x35 or 0x37 or 0x4A or 0x4E;

    /// <summary>Converts a key code with no modifiers.</summary>
    public static implicit operator TKey(ushort keyCode) => new(keyCode);

    /// <inheritdoc />
    public static bool operator ==(TKey a, TKey b) => a.Equals(b);

    /// <inheritdoc />
    public static bool operator !=(TKey a, TKey b) => !a.Equals(b);

    /// <inheritdoc />
    public bool Equals(TKey other) => Code == other.Code && Mods == other.Mods;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TKey other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Code | (Mods << 16);

    /// <inheritdoc />
    public override string ToString() => $"{{code: 0x{Code:X4}, mods: 0x{Mods:X4}}}";
}
