// Ported from include/tvision/internal/conctl.h and source/platform/conctl.cpp (Unix part).

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

using SharpVision.Objects;
using SharpVision.Platform.Interop;

namespace SharpVision.Platform;

/// <summary>
/// The terminal's file descriptors: <c>/dev/tty</c> when available (so the application
/// works with redirected stdin/stdout), otherwise stdin and stdout.
/// </summary>
internal sealed class ConsoleCtl : IDisposable
{
    private readonly SafeFileHandle? _tty;

    public ConsoleCtl()
    {
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
            In = Out = (int)_tty.DangerousGetHandle();
        }
        else
        {
            In = 0;
            Out = 1;
        }
    }

    /// <summary>The descriptor to read input from.</summary>
    public int In { get; }

    /// <summary>The descriptor to write output to.</summary>
    public int Out { get; }

    /// <summary>Writes all of <paramref name="data"/>, retrying on partial writes.</summary>
    public void Write(ReadOnlySpan<byte> data)
    {
        while (!data.IsEmpty)
        {
            var r = LibC.write(Out, ref MemoryMarshal.GetReference(data), (nuint)data.Length);
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
        int cols = 0, rows = 0;
        if (OperatingSystem.IsLinux())
        {
            foreach (var fd in new[] { In, Out })
            {
                if (LibC.ioctl(fd, LibC.TIOCGWINSZ_Linux, out var ws) != -1)
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

    public void Dispose() => _tty?.Dispose();

    internal static int EnvInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var v) ? v : fallback;
}
