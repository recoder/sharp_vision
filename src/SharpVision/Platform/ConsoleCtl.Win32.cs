// Ported from source/platform/conctl.cpp (Windows part).

using System;

using SharpVision.Objects;

using static SharpVision.Platform.Interop.Kernel32;

namespace SharpVision.Platform;

internal sealed partial class ConsoleCtl
{
    private const int Input = 0;
    private const int StartupOutput = 1;
    private const int ActiveOutput = 2;

    // Handles from GetStdHandle must not be closed; the ones we create (Owning) must.
    private readonly (nint Handle, bool Owning)[] _cn = new (nint, bool)[3];
    private bool _ownsConsole;

    private static bool IsValid(nint h) => h != 0 && h != INVALID_HANDLE_VALUE;

    private static bool IsConsole(nint h) => GetConsoleMode(h, out _) != 0;

    private static COORD WindowSize(SMALL_RECT srWindow) =>
        new((short)(srWindow.Right - srWindow.Left + 1), (short)(srWindow.Bottom - srWindow.Top + 1));

    // Uses the standard handles when they are consoles (some terminal emulators misbehave
    // with CONIN$/CONOUT$), allocates a console when none of them is, and draws into a new
    // screen buffer so that the startup one is restored on exit.
    private (nint In, nint Out) InitWin32()
    {
        (int Std, int Index)[] channels =
        [
            (STD_INPUT_HANDLE, Input),
            (STD_OUTPUT_HANDLE, StartupOutput),
            (STD_ERROR_HANDLE, StartupOutput),
        ];
        var haveConsole = false;
        foreach (var c in channels)
        {
            var h = GetStdHandle(c.Std);
            if (IsConsole(h))
            {
                haveConsole = true;
                if (!IsValid(_cn[c.Index].Handle))
                    _cn[c.Index] = (h, false);
            }
        }
        if (!haveConsole)
        {
            FreeConsole();
            AllocConsole();
            _ownsConsole = true;
        }
        if (!IsValid(_cn[Input].Handle))
            _cn[Input] = (CreateFileW("CONIN$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ, 0, OPEN_EXISTING, 0, 0), true);
        if (!IsValid(_cn[StartupOutput].Handle))
            _cn[StartupOutput] = (CreateFileW("CONOUT$", GENERIC_READ | GENERIC_WRITE, FILE_SHARE_WRITE, 0, OPEN_EXISTING, 0, 0), true);
        _cn[ActiveOutput] = (CreateConsoleScreenBuffer(GENERIC_READ | GENERIC_WRITE, 0, 0, CONSOLE_TEXTMODE_BUFFER, 0), true);
        // Force the screen buffer size to match the window size. The Console API guarantees
        // this, but some implementations are not compliant (e.g. Wine).
        GetConsoleScreenBufferInfo(_cn[StartupOutput].Handle, out var sbInfo);
        SetConsoleScreenBufferSize(_cn[ActiveOutput].Handle, WindowSize(sbInfo.srWindow));
        SetConsoleActiveScreenBuffer(_cn[ActiveOutput].Handle);
        foreach (var c in _cn)
        {
            if (!IsValid(c.Handle))
            {
                Console.Error.WriteLine("Error: cannot get a console.");
                Environment.Exit(1);
            }
        }
        return (_cn[Input].Handle, _cn[ActiveOutput].Handle);
    }

    private void DisposeWin32()
    {
        GetConsoleScreenBufferInfo(_cn[ActiveOutput].Handle, out var activeSbInfo);
        GetConsoleScreenBufferInfo(_cn[StartupOutput].Handle, out var startupSbInfo);

        var activeWindowSize = WindowSize(activeSbInfo.srWindow);
        var startupWindowSize = WindowSize(startupSbInfo.srWindow);

        // Preserve the current window size.
        if (activeWindowSize.X != startupWindowSize.X || activeWindowSize.Y != startupWindowSize.Y)
        {
            // The buffer may not be smaller than the window, so enlarge it if necessary, but
            // do not shrink it, to avoid losing data.
            var dwSize = startupSbInfo.dwSize;
            if (dwSize.X < activeWindowSize.X)
                dwSize.X = activeWindowSize.X;
            if (dwSize.Y < activeWindowSize.Y)
                dwSize.Y = activeWindowSize.Y;
            SetConsoleScreenBufferSize(_cn[StartupOutput].Handle, dwSize);
            // Get the updated cursor position, in case it changed after the resize.
            GetConsoleScreenBufferInfo(_cn[StartupOutput].Handle, out startupSbInfo);
            // Make sure the cursor is visible. If possible, show it in the bottom row.
            var srWindow = startupSbInfo.srWindow;
            var dwCursorPosition = startupSbInfo.dwCursorPosition;
            srWindow.Right = Math.Max(dwCursorPosition.X, (short)(activeWindowSize.X - 1));
            srWindow.Left = (short)(srWindow.Right - (activeWindowSize.X - 1));
            srWindow.Bottom = Math.Max(dwCursorPosition.Y, (short)(activeWindowSize.Y - 1));
            srWindow.Top = (short)(srWindow.Bottom - (activeWindowSize.Y - 1));
            SetConsoleWindowInfo(_cn[StartupOutput].Handle, 1, srWindow);
        }

        SetConsoleActiveScreenBuffer(_cn[StartupOutput].Handle);
        foreach (var c in _cn)
        {
            if (c.Owning)
                CloseHandle(c.Handle);
        }
        if (_ownsConsole)
            FreeConsole();
    }

    private void WriteWin32(ReadOnlySpan<byte> data)
    {
        // Writing 0 bytes makes the cursor invisible for a moment in old consoles.
        if (!data.IsEmpty)
            WriteConsoleA(Out, ref System.Runtime.InteropServices.MemoryMarshal.GetReference(data), (uint)data.Length, 0, 0);
    }

    private TPoint GetSizeWin32()
    {
        if (GetConsoleScreenBufferInfo(Out, out var sbInfo) != 0)
        {
            var srWindow = sbInfo.srWindow;
            return new TPoint(Math.Max(srWindow.Right - srWindow.Left + 1, 0), Math.Max(srWindow.Bottom - srWindow.Top + 1, 0));
        }
        return default;
    }

    private TPoint GetFontSizeWin32()
    {
        if (GetCurrentConsoleFont(Out, 0, out var fontInfo) != 0)
            return new TPoint(fontInfo.dwFontSize.X, fontInfo.dwFontSize.Y);
        return default;
    }
}
