using System.Runtime.InteropServices;

namespace SharpVision.Events;

/// <summary>
/// The two halves of a key code: the character code (low byte) and the scan code (high byte).
/// </summary>
/// Ported from include/tvision/system.h (struct CharScanType).
[StructLayout(LayoutKind.Sequential, Size = 2)]
public struct CharScanType
{
    /// <summary>Character code, in the current code page (CP437 by default). 0 for non-character keys.</summary>
    public byte CharCode;

    /// <summary>Scan code.</summary>
    public byte ScanCode;
}
