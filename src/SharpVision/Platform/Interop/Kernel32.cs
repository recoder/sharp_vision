using System.Runtime.InteropServices;

namespace SharpVision.Platform.Interop;

/// <summary>The Win32 console functions the Windows backend needs.</summary>
internal static partial class Kernel32
{
    private const string Lib = "kernel32";

    public const int STD_INPUT_HANDLE = -10;
    public const int STD_OUTPUT_HANDLE = -11;
    public const int STD_ERROR_HANDLE = -12;

    public const uint GENERIC_READ = 0x80000000;
    public const uint GENERIC_WRITE = 0x40000000;
    public const uint FILE_SHARE_READ = 0x1;
    public const uint FILE_SHARE_WRITE = 0x2;
    public const uint OPEN_EXISTING = 3;
    public const uint CONSOLE_TEXTMODE_BUFFER = 1;

    // Input modes.
    public const uint ENABLE_PROCESSED_INPUT = 0x0001;
    public const uint ENABLE_LINE_INPUT = 0x0002;
    public const uint ENABLE_ECHO_INPUT = 0x0004;
    public const uint ENABLE_WINDOW_INPUT = 0x0008;
    public const uint ENABLE_MOUSE_INPUT = 0x0010;
    public const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    public const uint ENABLE_EXTENDED_FLAGS = 0x0080;

    // Output modes.
    public const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
    public const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
    public const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;

    public const uint CP_UTF8 = 65001;

    // INPUT_RECORD event types.
    public const ushort KEY_EVENT = 0x0001;
    public const ushort MOUSE_EVENT = 0x0002;
    public const ushort WINDOW_BUFFER_SIZE_EVENT = 0x0004;

    // MOUSE_EVENT_RECORD event flags.
    public const uint MOUSE_WHEELED = 0x0004;
    public const uint MOUSE_HWHEELED = 0x0008;

    public const ushort VK_MENU = 0x12;

    public const uint INFINITE = 0xFFFFFFFF;
    public const uint WAIT_OBJECT_0 = 0;

    public static readonly nint INVALID_HANDLE_VALUE = -1;

    [StructLayout(LayoutKind.Sequential)]
    public struct COORD(short x, short y)
    {
        public short X = x;
        public short Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SMALL_RECT
    {
        public short Left;
        public short Top;
        public short Right;
        public short Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_SCREEN_BUFFER_INFO
    {
        public COORD dwSize;
        public COORD dwCursorPosition;
        public ushort wAttributes;
        public SMALL_RECT srWindow;
        public COORD dwMaximumWindowSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_FONT_INFO
    {
        public uint nFont;
        public COORD dwFontSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CONSOLE_CURSOR_INFO
    {
        public uint dwSize;
        public int bVisible;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEY_EVENT_RECORD
    {
        public int bKeyDown;
        public ushort wRepeatCount;
        public ushort wVirtualKeyCode;
        public ushort wVirtualScanCode;
        public ushort UnicodeChar;
        public uint dwControlKeyState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSE_EVENT_RECORD
    {
        public COORD dwMousePosition;
        public uint dwButtonState;
        public uint dwControlKeyState;
        public uint dwEventFlags;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUT_RECORD
    {
        [FieldOffset(0)]
        public ushort EventType;

        [FieldOffset(4)]
        public KEY_EVENT_RECORD KeyEvent;

        [FieldOffset(4)]
        public MOUSE_EVENT_RECORD MouseEvent;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CHAR_INFO
    {
        public ushort UnicodeChar;
        public ushort Attributes;
    }

    [LibraryImport(Lib, SetLastError = true)]
    public static partial nint GetStdHandle(int nStdHandle);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int SetConsoleMode(nint hConsoleHandle, uint dwMode);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int AllocConsole();

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int FreeConsole();

    [LibraryImport(Lib, SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial nint CreateFileW(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        nint lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, nint hTemplateFile);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial nint CreateConsoleScreenBuffer(uint dwDesiredAccess, uint dwShareMode,
        nint lpSecurityAttributes, uint dwFlags, nint lpScreenBufferData);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int SetConsoleActiveScreenBuffer(nint hConsoleOutput);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int GetConsoleScreenBufferInfo(nint hConsoleOutput, out CONSOLE_SCREEN_BUFFER_INFO info);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int SetConsoleScreenBufferSize(nint hConsoleOutput, COORD dwSize);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int SetConsoleWindowInfo(nint hConsoleOutput, int bAbsolute, in SMALL_RECT lpConsoleWindow);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int SetConsoleCursorPosition(nint hConsoleOutput, COORD dwCursorPosition);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int SetConsoleCursorInfo(nint hConsoleOutput, in CONSOLE_CURSOR_INFO info);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int GetCurrentConsoleFont(nint hConsoleOutput, int bMaximumWindow, out CONSOLE_FONT_INFO info);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int SetConsoleTextAttribute(nint hConsoleOutput, ushort wAttributes);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int FillConsoleOutputAttribute(nint hConsoleOutput, ushort wAttribute, uint nLength,
        COORD dwWriteCoord, out uint lpNumberOfAttrsWritten);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int FillConsoleOutputCharacterW(nint hConsoleOutput, ushort cCharacter, uint nLength,
        COORD dwWriteCoord, out uint lpNumberOfCharsWritten);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int WriteConsoleA(nint hConsoleOutput, ref byte lpBuffer, uint nNumberOfCharsToWrite,
        nint lpNumberOfCharsWritten, nint lpReserved);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int WriteConsoleOutputW(nint hConsoleOutput, ref CHAR_INFO lpBuffer, COORD dwBufferSize,
        COORD dwBufferCoord, ref SMALL_RECT lpWriteRegion);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int GetNumberOfConsoleInputEvents(nint hConsoleInput, out uint lpcNumberOfEvents);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int ReadConsoleInputW(nint hConsoleInput, out INPUT_RECORD lpBuffer, uint nLength,
        out uint lpNumberOfEventsRead);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial uint GetConsoleCP();

    [LibraryImport(Lib, SetLastError = true)]
    public static partial uint GetConsoleOutputCP();

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int SetConsoleCP(uint wCodePageID);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int SetConsoleOutputCP(uint wCodePageID);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial uint GetOEMCP();

    [LibraryImport(Lib, SetLastError = true)]
    public static partial uint WaitForMultipleObjects(uint nCount, ref nint lpHandles, int bWaitAll, uint dwMilliseconds);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int CloseHandle(nint hObject);
}
