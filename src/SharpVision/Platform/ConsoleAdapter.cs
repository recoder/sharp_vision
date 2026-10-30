using System;

namespace SharpVision.Platform;

/// <summary>A console backend: its display and the event sources that produce its input.</summary>
/// Ported from include/tvision/internal/platform.h (class ConsoleAdapter).
internal abstract class ConsoleAdapter(DisplayAdapter display, EventSource[] sources) : IDisposable
{
    public DisplayAdapter Display { get; } = display;

    public EventSource[] Sources { get; } = sources;

    /// <summary>Restores the console.</summary>
    public abstract void Dispose();
}
