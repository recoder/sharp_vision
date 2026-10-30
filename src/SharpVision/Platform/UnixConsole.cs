using SharpVision.Platform.Interop;

namespace SharpVision.Platform;

/// <summary>
/// A Unix terminal set up for Turbo Vision: raw input, alternate screen, extended keyboard
/// and mouse reporting. Disposing it restores the terminal.
/// </summary>
/// Ported from source/platform/unixcon.cpp (UnixConsoleAdapter) and the terminal setup that
/// upstream delegates to ncurses (newterm, raw, noecho, nonl, endwin).
internal sealed class UnixConsole : ConsoleAdapter
{
    private readonly ConsoleCtl _con;
    private readonly LibC.Termios _savedTermios;
    private readonly bool _hasSavedTermios;
    private bool _disposed;

    private UnixConsole(ConsoleCtl con, TerminalInput input, SigwinchSource sigwinch)
        : base(new AnsiDisplay(con), [input, sigwinch])
    {
        _con = con;
        Sigwinch = sigwinch;

        // Capture all keyboard input, without echo or CR/LF translation (ncurses' raw,
        // noecho and nonl).
        if (LibC.tcgetattr((int)con.In, out _savedTermios) == 0)
        {
            _hasSavedTermios = true;
            var raw = _savedTermios;
            LibC.cfmakeraw(ref raw);
            LibC.tcsetattr((int)con.In, LibC.TCSANOW, raw);
        }
        con.Write("\x1B[?1049h"); // Switch to the alternate screen, like ncurses' smcup.
        TermIO.KeyModsOn(con);
        con.Write(TermIO.MouseOnSeq);
        // Don't miss a resize that happened before the handler was installed.
        sigwinch.Signal();
    }

    public SigwinchSource Sigwinch { get; }

    /// <summary>Sets up the terminal, or returns null if there is none.</summary>
    public static UnixConsole? Create()
    {
        var con = new ConsoleCtl();
        if (LibC.isatty((int)con.In) == 0)
        {
            con.Dispose();
            return null;
        }
        return new UnixConsole(con, new TerminalInput(con, new InputState()), new SigwinchSource());
    }

    public override void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _con.Write(TermIO.MouseOffSeq);
        TermIO.KeyModsOff(_con);
        _con.Write(
            "\x1B[0m" +      // Reset attributes.
            "\x1B[0 q" +     // Default cursor shape.
            "\x1B[?25h" +    // Show the cursor.
            "\x1B[?1049l");  // Leave the alternate screen, like ncurses' rmcup.
        if (_hasSavedTermios)
            LibC.tcsetattr((int)_con.In, LibC.TCSANOW, _savedTermios);
        Sigwinch.Dispose();
        _con.Dispose();
    }
}
