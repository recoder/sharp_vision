using System;

namespace SharpVision.Platform;

/// <summary>A console backend: its display and the event sources that produce its input.</summary>
/// Ported from include/tvision/internal/platform.h (class ConsoleAdapter).
internal abstract class ConsoleAdapter(DisplayAdapter display, EventSource[] sources) : IDisposable
{
    public DisplayAdapter Display { get; } = display;

    public EventSource[] Sources { get; } = sources;

    /// <summary>Puts <paramref name="text"/> on the system clipboard; false if not supported.</summary>
    public virtual bool SetClipboardText(ReadOnlySpan<char> text) => false;

    /// <summary>Asks for the system clipboard's text, delivered to <paramref name="accept"/>; false if not supported.</summary>
    public virtual bool RequestClipboardText(Action<string> accept) => false;

    /// <summary>Restores the console.</summary>
    public abstract void Dispose();
}
