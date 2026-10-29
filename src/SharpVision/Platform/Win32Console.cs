// Ported from include/tvision/internal/win32con.h and source/platform/win32con.cpp
// (Win32ConsoleAdapter).

using System.Runtime.InteropServices;

using static SharpVision.Platform.Interop.Kernel32;

namespace SharpVision.Platform;

/// <summary>
/// The Windows console set up for Turbo Vision: raw keyboard and mouse input, UTF-8 code
/// pages and, when supported, VT output. Disposing it restores the console.
/// </summary>
internal sealed class Win32Console : ConsoleAdapter
{
    private readonly ConsoleCtl _con;
    private readonly uint _startupMode;
    private readonly uint _cpInput;
    private readonly uint _cpOutput;
    private bool _disposed;

    private Win32Console(ConsoleCtl con, uint startupMode, uint cpInput, uint cpOutput, Win32Display display, Win32Input input)
        : base(display, [input])
    {
        _con = con;
        _startupMode = startupMode;
        _cpInput = cpInput;
        _cpOutput = cpOutput;
    }

    public static Win32Console Create()
    {
        var con = new ConsoleCtl();
        var startupMode = InitInputMode(con);
        var isLegacyConsole = InitOutputMode(con);
        // shortcut: upstream also switches the legacy console away from bitmap fonts
        // (disableBitmapFont); add it if box-drawing glyphs render badly there.
        InitEncoding(isLegacyConsole, out var cpInput, out var cpOutput);
        return new Win32Console(con, startupMode, cpInput, cpOutput, new Win32Display(con, isLegacyConsole), new Win32Input(con));
    }

    public override void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        SetConsoleCP(_cpInput);
        SetConsoleOutputCP(_cpOutput);
        SetConsoleMode(_con.In, _startupMode);
        _con.Dispose();
    }

    private static uint InitInputMode(ConsoleCtl con)
    {
        GetConsoleMode(con.In, out var consoleMode);
        var startupMode = consoleMode;
        consoleMode |= ENABLE_WINDOW_INPUT; // Report changes in buffer size.
        consoleMode |= ENABLE_MOUSE_INPUT; // Report mouse events.
        consoleMode &= ~ENABLE_PROCESSED_INPUT; // Report Ctrl+C and Shift+Arrow events.
        consoleMode &= ~(ENABLE_ECHO_INPUT | ENABLE_LINE_INPUT); // Report Ctrl+S.
        consoleMode |= ENABLE_EXTENDED_FLAGS; // Disable the Quick Edit mode,
        consoleMode &= ~ENABLE_QUICK_EDIT_MODE; // which inhibits the mouse.
        SetConsoleMode(con.In, consoleMode);
        return startupMode;
    }

    // Returns whether this is the legacy console, which does not support VT sequences.
    private static bool InitOutputMode(ConsoleCtl con)
    {
        GetConsoleMode(con.Out, out var consoleMode);
        consoleMode &= ~ENABLE_WRAP_AT_EOL_OUTPUT; // Avoid scrolling when reaching end of line.
        SetConsoleMode(con.Out, consoleMode);

        // Wine reports success when enabling VT processing but does not support it.
        if (IsWine())
            return true;
        consoleMode |= DISABLE_NEWLINE_AUTO_RETURN; // Do not do CR on LF.
        consoleMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING; // Allow ANSI escape sequences.
        SetConsoleMode(con.Out, consoleMode);
        GetConsoleMode(con.Out, out consoleMode);
        return (consoleMode & ENABLE_VIRTUAL_TERMINAL_PROCESSING) == 0;
    }

    private static void InitEncoding(bool isLegacyConsole, out uint cpInput, out uint cpOutput)
    {
        cpInput = GetConsoleCP();
        cpOutput = GetConsoleOutputCP();
        // The legacy console with a bitmap font cannot display UTF-8, but the OEM code page
        // (usually the one the font supports) gets similar replacements for missing glyphs.
        SetConsoleCP(CP_UTF8);
        SetConsoleOutputCP(isLegacyConsole ? GetOEMCP() : CP_UTF8);
    }

    private static bool IsWine() =>
        NativeLibrary.TryLoad("ntdll.dll", out var ntdll) && NativeLibrary.TryGetExport(ntdll, "wine_get_version", out _);
}
