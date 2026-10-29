using System.Runtime.InteropServices;

namespace SharpVision.Platform.Interop;

/// <summary>
/// The few libc functions the Unix backend needs. Variadic functions (<c>open</c>,
/// <c>fcntl</c>, <c>ioctl</c>) are avoided where possible, because P/Invoke cannot call
/// them correctly on every ABI (e.g. macOS arm64 passes variadic arguments on the stack).
/// </summary>
internal static partial class LibC
{
    private const string Lib = "libc";

    public const short POLLIN = 0x001;
    public const short POLLERR = 0x008;
    public const short POLLHUP = 0x010;
    public const short POLLNVAL = 0x020;

    public const int TCSANOW = 0;

    // Linux value of TIOCGWINSZ. Only used on Linux, where variadic ioctl calls work.
    public const nuint TIOCGWINSZ_Linux = 0x5413;

    [StructLayout(LayoutKind.Sequential)]
    public struct PollFd
    {
        public int Fd;
        public short Events;
        public short REvents;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WinSize
    {
        public ushort Row;
        public ushort Col;
        public ushort XPixel;
        public ushort YPixel;
    }

    // An opaque 'struct termios', larger than the platforms' actual sizes (60 bytes on
    // Linux, 72 on macOS). It is only modified through cfmakeraw.
    [StructLayout(LayoutKind.Sequential, Size = 256)]
    public struct Termios
    {
        private readonly byte _data;
    }

    [LibraryImport(Lib, SetLastError = true)]
    public static partial nint read(int fd, ref byte buf, nuint count);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial nint write(int fd, ref byte buf, nuint count);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int poll(ref PollFd fds, nuint nfds, int timeout);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int isatty(int fd);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int tcgetattr(int fd, out Termios termios);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int tcsetattr(int fd, int optionalActions, in Termios termios);

    [LibraryImport(Lib)]
    public static partial void cfmakeraw(ref Termios termios);

    [LibraryImport(Lib, SetLastError = true)]
    public static partial int ioctl(int fd, nuint request, out WinSize ws);
}
