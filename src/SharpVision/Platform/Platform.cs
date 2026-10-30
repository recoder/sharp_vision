using System;

using SharpVision.Drawing;
using SharpVision.Events;

namespace SharpVision.Platform;

/// <summary>
/// The connection to the actual console: event sources, the display buffer and the
/// terminal backend. A singleton created on first use.
/// </summary>
/// Ported from include/tvision/internal/platform.h and source/platform/platform.cpp.
internal sealed class Platform : IDisplayOutput
{
    private static readonly Lazy<Platform> _instance = new(() => new Platform());

    private readonly object _lock = new();
    private readonly EventWaiter _waiter = new();
    private readonly DisplayBuffer _displayBuf = new(ConsoleCtl.EnvInt("TVISION_MAX_FPS", -1));
    private readonly DisplayAdapter _dummyDisplay = new();
    private ConsoleAdapter? _console;

    private Platform()
    {
        // Restore the terminal if the process ends without shutting down the application.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => RestoreConsole();
        AppDomain.CurrentDomain.UnhandledException += (_, _) => RestoreConsole();
    }

    public static Platform Instance => _instance.Value;

    private DisplayAdapter Display => _console?.Display ?? _dummyDisplay;

    public void SetUpConsole()
    {
        lock (_lock)
        {
            if (_console is null)
            {
                _console = OperatingSystem.IsWindows() ? Win32Console.Create() : UnixConsole.Create();
                if (_console is not null)
                {
                    foreach (var source in _console.Sources)
                        _waiter.AddSource(source);
                }
            }
        }
    }

    public void RestoreConsole()
    {
        lock (_lock)
        {
            if (_console is not null)
            {
                _displayBuf.FlushScreen(_console.Display);
                foreach (var source in _console.Sources)
                    _waiter.RemoveSource(source);
                _console.Dispose();
                _console = null;
            }
        }
    }

    public bool GetEvent(ref TEvent ev) => _waiter.GetEvent(ref ev);

    public void WaitForEvents(int ms)
    {
        var waitTimeoutMs = ms;
        // When the display buffer has pending changes, wake up in time to flush them.
        var flushTimeoutMs = _displayBuf.TimeUntilPendingFlushMs();
        if (ms < 0)
            waitTimeoutMs = flushTimeoutMs;
        else if (flushTimeoutMs >= 0)
            waitTimeoutMs = Math.Min(ms, flushTimeoutMs);
        _waiter.WaitForEvents(waitTimeoutMs);
    }

    public void InterruptEventWait() => _waiter.InterruptEventWait();

    // The caret size is reported in the range 1 to 100, as the original
    // THardwareInfo::getCaretSize() does and as TScreen expects.
    public int GetCaretSize() => Math.Min(Math.Max(_displayBuf.CaretSize, 1), 100);

    public bool IsCaretVisible() => _displayBuf.CaretSize > 0;

    public void ClearScreen()
    {
        lock (_lock)
            _displayBuf.ClearScreen(Display);
    }

    public int GetScreenRows() => _displayBuf.Size.Y;

    public int GetScreenCols() => _displayBuf.Size.X;

    public void SetCaretPosition(int x, int y) => _displayBuf.SetCaretPosition(x, y);

    public void SetCaretSize(int size) => _displayBuf.SetCaretSize(size);

    public void ScreenWrite(int x, int y, ReadOnlySpan<TScreenCell> cells) => _displayBuf.ScreenWrite(x, y, cells);

    public ushort GetScreenMode()
    {
        lock (_lock)
        {
            var colorCount = Display.GetColorCount();
            var mode = colorCount == 0 ? TDisplay.smMono : TDisplay.smCO80;
            if (colorCount >= 256)
                mode |= TDisplay.smColor256;
            if (colorCount >= 256 * 256 * 256)
                mode |= TDisplay.smColorHigh;
            var fontSize = Display.GetFontSize();
            if (fontSize.X > 0 && fontSize.Y > 0 && fontSize.X >= fontSize.Y)
                mode |= TDisplay.smFont8x8;
            return mode;
        }
    }

    public void FlushScreen()
    {
        lock (_lock)
            _displayBuf.FlushScreen(Display);
    }

    public TScreenCell[] ReloadScreenInfo()
    {
        lock (_lock)
            return _displayBuf.ReloadScreenInfo(Display);
    }

    public void FreeScreenBuffer() => _displayBuf.ReloadScreenInfo(_dummyDisplay);
}
