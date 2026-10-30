using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

using SharpVision.Objects;
using SharpVision.Platform.Interop;

namespace SharpVision.Platform;

/// <summary>
/// The console's input and output handles. On Unix, file descriptors: <c>/dev/tty</c> when
/// available (so the application works with redirected stdin/stdout), otherwise stdin and
/// stdout. On Windows, see <c>ConsoleCtl.Win32.cs</c>.
/// </summary>
/// Ported from include/tvision/internal/conctl.h and source/platform/conctl.cpp (Unix part;
/// the Windows part is in ConsoleCtl.Win32.cs).
internal sealed partial class ConsoleCtl : IDisposable
{
    private readonly SafeFileHandle? _tty;

    public ConsoleCtl()
    {
        if (OperatingSystem.IsWindows())
        {
            (In, Out) = InitWin32();
            return;
        }
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TVISION_USE_STDIO")))
        {
            try
            {
                // .NET opens files with O_CLOEXEC, so subprocesses do not inherit the descriptor.
                _tty = File.OpenHandle("/dev/tty", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                _tty = null;
            }
        }
        if (_tty is not null)
        {
            In = Out = _tty.DangerousGetHandle();
        }
        else
        {
            In = 0;
            Out = 1;
        }
    }

    /// <summary>The descriptor or handle to read input from.</summary>
    public nint In { get; }

    /// <summary>The descriptor or handle to write output to.</summary>
    public nint Out { get; }

    /// <summary>Writes all of <paramref name="data"/>, retrying on partial writes.</summary>
    public void Write(ReadOnlySpan<byte> data)
    {
        if (OperatingSystem.IsWindows())
        {
            WriteWin32(data);
            return;
        }
        while (!data.IsEmpty)
        {
            var r = LibC.write((int)Out, ref MemoryMarshal.GetReference(data), (nuint)data.Length);
            if (r < 0)
            {
                if (Marshal.GetLastPInvokeError() == 4) // EINTR
                    continue;
                return;
            }
            data = data[(int)r..];
        }
    }

    /// <summary>Writes an ASCII escape sequence.</summary>
    public void Write(string ascii)
    {
        Span<byte> buf = ascii.Length <= 512 ? stackalloc byte[ascii.Length] : new byte[ascii.Length];
        for (var i = 0; i < ascii.Length; ++i)
            buf[i] = (byte)ascii[i];
        Write(buf);
    }

    /// <summary>The terminal size in columns and rows, limited by <c>COLUMNS</c>/<c>LINES</c> if set.</summary>
    public TPoint GetSize()
    {
        if (OperatingSystem.IsWindows())
            return GetSizeWin32();
        int cols = 0, rows = 0;
        if (OperatingSystem.IsLinux())
        {
            foreach (var fd in new[] { In, Out })
            {
                if (LibC.ioctl((int)fd, LibC.TIOCGWINSZ_Linux, out var ws) != -1)
                {
                    cols = ws.Col;
                    rows = ws.Row;
                    break;
                }
            }
        }
        else
        {
            // UPSTREAM: upstream uses ioctl(TIOCGWINSZ) everywhere. P/Invoke cannot call the
            // variadic ioctl correctly on macOS arm64, so ask .NET's console instead.
            try
            {
                cols = Console.WindowWidth;
                rows = Console.WindowHeight;
            }
            catch (IOException)
            {
            }
        }
        var envCol = EnvInt("COLUMNS", int.MaxValue);
        var envRow = EnvInt("LINES", int.MaxValue);
        return new TPoint(Math.Min(Math.Max(cols, 0), Math.Max(envCol, 0)), Math.Min(Math.Max(rows, 0), Math.Max(envRow, 0)));
    }

    /// <summary>The font size in pixels, or 0 if unknown (only known on Windows).</summary>
    public TPoint GetFontSize() => OperatingSystem.IsWindows() ? GetFontSizeWin32() : default;

    public void Dispose()
    {
        if (OperatingSystem.IsWindows())
            DisposeWin32();
        _tty?.Dispose();
    }

    internal static int EnvInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var v) ? v : fallback;
}
