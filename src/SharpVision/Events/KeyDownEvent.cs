using System;
using System.Runtime.InteropServices;

namespace SharpVision.Events;

/// <summary>The data of a keyboard event.</summary>
/// <remarks>
/// <see cref="KeyCode"/> and <see cref="CharScan"/> overlap, as in upstream's union, so
/// writing <c>CharScan.CharCode</c> changes the low byte of <see cref="KeyCode"/>
/// (assuming a little-endian platform, like every platform .NET supports).
/// </remarks>
/// Ported from include/tvision/system.h (struct KeyDownEvent).
[StructLayout(LayoutKind.Explicit)]
public struct KeyDownEvent
{
    /// <summary>Key code (<c>kbXxx</c>).</summary>
    [FieldOffset(0)]
    public ushort KeyCode;

    /// <summary>The key code split into character code and scan code.</summary>
    [FieldOffset(0)]
    public CharScanType CharScan;

    /// <summary>Keyboard state (<c>kbShift</c>, <c>kbCtrlShift</c>, ...).</summary>
    [FieldOffset(2)]
    public ushort ControlKeyState;

    /// <summary>The typed text as one Unicode character (possibly a surrogate pair), or null.</summary>
    /// <remarks>UPSTREAM: upstream stores up to 4 bytes of UTF-8 plus a length.</remarks>
    [FieldOffset(8)]
    public string? Text;

    /// <summary>Returns the typed text, or an empty span if the key produced none.</summary>
    public readonly ReadOnlySpan<char> GetText() => Text;

    /// <summary>Returns the key combination of this event.</summary>
    public static implicit operator TKey(KeyDownEvent keyDown) => new(keyDown.KeyCode, keyDown.ControlKeyState);
}
