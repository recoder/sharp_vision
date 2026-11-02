using System;

using SharpVision.Platform;

namespace SharpVision.Events;

/// <summary>
/// The system clipboard, falling back to a clipboard local to the application when the console offers none.
/// Requested text arrives as paste key events.
/// </summary>
/// Ported from source/tvision/tclipbrd.cpp, include/tvision/system.h (class TClipboard).
/// <remarks>A magiblot/tvision extension; not part of Turbo Vision 2.0.</remarks>
public static class TClipboard
{
    private static string _localText = "";

    /// <summary>Puts <paramref name="text"/> on the clipboard.</summary>
    public static void SetText(ReadOnlySpan<char> text)
    {
        if (THardwareInfo.SetClipboardText(text))
            return;
        _localText = text.ToString();
    }

    /// <summary>Asks for the clipboard's text, which arrives through <see cref="TEventQueue.SetPasteText"/>.</summary>
    public static void RequestText()
    {
        if (THardwareInfo.RequestClipboardText(text => TEventQueue.SetPasteText(text)))
            return;
        TEventQueue.SetPasteText(_localText);
    }
}
