using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SharpVision.StdDlg;

/// <summary>
/// The Borland run-time directory functions and the path helpers the standard dialogs use. Paths are
/// DOS-style internally (<c>\</c> separators, a drive letter); on Unix, drive C is emulated and
/// <see cref="FnMerge"/> turns the result back into a Unix path.
/// </summary>
/// Ported from source/platform/dir.cpp, source/platform/findfrst.cpp (Unix path),
/// include/tvision/internal/pathconv.h, source/tvision/tfillist.cpp (squeeze, fexpand),
/// source/tvision/tdircoll.cpp (driveValid, isDir, pathValid, validFileName, getCurDir, isWild),
/// include/tvision/util.h, include/tvision/compat/borland/dir.h, dos.h.
/// <remarks>
/// Upstream's <c>MAXPATH</c>-style buffer limits are not enforced. <see cref="FindFirst"/> uses the Unix
/// matching rules on every platform.
/// </remarks>
public static class Dir
{
    /// <summary><see cref="FnSplit"/> flag: the name or extension has wildcards.</summary>
    public const int WILDCARDS = 0x01;
    /// <summary><see cref="FnSplit"/> flag: there is an extension.</summary>
    public const int EXTENSION = 0x02;
    /// <summary><see cref="FnSplit"/> flag: there is a file name.</summary>
    public const int FILENAME = 0x04;
    /// <summary><see cref="FnSplit"/> flag: there is a directory.</summary>
    public const int DIRECTORY = 0x08;
    /// <summary><see cref="FnSplit"/> flag: there is a drive.</summary>
    public const int DRIVE = 0x10;

    /// <summary>File attribute: none.</summary>
    public const byte faNormal = 0x00;
    /// <summary>File attribute: read-only.</summary>
    public const byte faRdOnly = 0x01;
    /// <summary>File attribute: hidden (on Unix, the name starts with a dot).</summary>
    public const byte faHidden = 0x02;
    /// <summary>File attribute: system (on Unix, not a regular file).</summary>
    public const byte faSystem = 0x04;
    /// <summary>File attribute: volume label.</summary>
    public const byte faLabel = 0x08;
    /// <summary>File attribute: directory.</summary>
    public const byte faDirec = 0x10;
    /// <summary>File attribute: archive.</summary>
    public const byte faArch = 0x20;

    private static readonly SearchValues<char> _illegalChars = SearchValues.Create("<>|\"\\");

    private const int SpecialBits = faDirec | faHidden | faSystem;

    private static char At(string s, int i) => i < s.Length ? s[i] : '\0';

    private static bool IsSep(char c) => c == '\\' || c == '/';

    /// <summary>Splits <paramref name="path"/> into drive, directory, name and extension; returns the <c>FnSplit</c> flags.</summary>
    public static int FnSplit(string? path, out string drive, out string dir, out string name, out string ext)
    {
        var flags = 0;
        drive = dir = name = ext = "";
        if (string.IsNullOrEmpty(path))
            return flags;

        var len = path.Length;
        int caret = -1, slash = -1, lastDot = -1, firstDot = -1;
        for (var i = len - 1; i >= 0; --i)
        {
            switch (path[i])
            {
                case '?':
                case '*':
                    // Wildcards are only detected in filename or extension.
                    if (slash < 0)
                        flags |= WILDCARDS;
                    break;
                case '.':
                    if (slash < 0)
                    {
                        if (lastDot < 0)
                            lastDot = i;
                        firstDot = i;
                    }
                    break;
                case '\\':
                case '/':
                    if (slash < 0)
                        slash = i;
                    break;
                case ':':
                    if (i == 1)
                    {
                        caret = i;
                        i = 0; // Exit loop, we don't check the drive letter.
                    }
                    break;
            }
        }

        var driveEnd = caret >= 0 ? caret + 1 : 0;
        var dirEnd = slash >= 0 ? slash + 1 : driveEnd;
        var nameEnd = lastDot >= 0 ? lastDot : len;
        // Special case: path ends with '.' or '..', thus there's no filename.
        if (lastDot == len - 1 && lastDot - firstDot < 2 && firstDot == dirEnd)
            dirEnd = nameEnd = len;

        drive = path[..driveEnd];
        dir = path[driveEnd..dirEnd];
        name = path[dirEnd..nameEnd];
        ext = path[nameEnd..];
        if (drive.Length > 0)
            flags |= DRIVE;
        if (dir.Length > 0)
            flags |= DIRECTORY;
        if (name.Length > 0)
            flags |= FILENAME;
        if (ext.Length > 0)
            flags |= EXTENSION;
        return flags;
    }

    /// <summary>Joins path components, adding the separators. On Unix, the drive is dropped and <c>\</c> becomes <c>/</c>.</summary>
    public static string FnMerge(string? drive, string? dir, string? name, string? ext)
    {
        var path = "";
        if (OperatingSystem.IsWindows() && !string.IsNullOrEmpty(drive))
        {
            path += drive;
            if (path[^1] != ':')
                path += ':';
        }
        if (!string.IsNullOrEmpty(dir))
        {
            path += dir;
            if (!IsSep(path[^1]))
                path += '\\';
        }
        if (!string.IsNullOrEmpty(name))
            path += name;
        if (!string.IsNullOrEmpty(ext))
        {
            if (ext[0] != '.')
                path += '.';
            path += ext;
        }
        // fnmerge is often used before accessing files, so producing a
        // UNIX-formatted path fixes most cases of filesystem access.
        return OperatingSystem.IsWindows() ? path : PathDos2Unix(path);
    }

    private static string PathDos2Unix(string s, bool drive = true)
    {
        s = s.Replace('\\', '/');
        if (drive && s.Length > 1 && s[1] == ':' && char.IsAsciiLetter(s[0]))
            s = s[2..];
        return s;
    }

    /// <summary>The current drive, 0 for A. On Unix, always C.</summary>
    public static int GetDisk() =>
        OperatingSystem.IsWindows() ? char.ToUpperInvariant(Environment.CurrentDirectory[0]) - 'A' : 'C' - 'A';

    /// <summary>
    /// Gets the current directory of <paramref name="drive"/> (0 = default, 1 = A) without the drive or the
    /// leading separator. Returns 0 on success, -1 otherwise.
    /// </summary>
    public static int GetCurDir(int drive, out string direc)
    {
        direc = "";
        if (OperatingSystem.IsWindows())
        {
            try
            {
                direc = Path.GetFullPath($"{(char)((drive != 0 ? drive - 1 : GetDisk()) + 'A')}:")[3..];
                return 0;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return -1;
            }
        }
        if (drive == 0 || drive - 1 == GetDisk())
        {
            direc = Environment.CurrentDirectory.Replace('/', '\\')[1..];
            return 0;
        }
        return -1;
    }

    /// <summary>The current directory of <paramref name="drive"/> (0 = A, -1 = current) as <c>X:\dir\</c>.</summary>
    public static string GetCurDir(int drive = -1)
    {
        var dir = $"{(char)(((uint)drive <= 'Z' - 'A' ? drive : GetDisk()) + 'A')}:\\";
        GetCurDir(dir[0] - 'A' + 1, out var cur);
        dir += cur;
        if (dir.Length > 3)
            dir += '\\';
        return dir;
    }

    /// <summary>Gets the home directory (and, on Windows, its drive). Returns false if it is not set.</summary>
    public static bool GetHomeDir(ref string drive, ref string dir)
    {
        if (OperatingSystem.IsWindows())
        {
            var homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE");
            var homePath = Environment.GetEnvironmentVariable("HOMEPATH");
            if (homeDrive is null || homePath is null)
                return false;
            drive = homeDrive;
            dir = homePath;
            return true;
        }
        var home = Environment.GetEnvironmentVariable("HOME");
        if (home is null)
            return false;
        dir = home;
        return true;
    }

    /// <summary>Removes <c>.</c> and <c>..</c> components and repeated separators from a <c>\</c>-separated path.</summary>
    public static string Squeeze(string path)
    {
        var dest = new char[path.Length];
        int d = 0, s = 0;
        var last = '\0';
        while (At(path, s) != '\0')
        {
            if (last == '\\')
                Skip(ref s); // skip repeated '\'
            if ((last == '\0' || last == '\\') && At(path, s) == '.')
            {
                s++;
                if (At(path, s) == '\0' || At(path, s) == '\\') // have a '.' or '.\'
                    Skip(ref s);
                else if (At(path, s) == '.' && (At(path, s + 1) == '\0' || At(path, s + 1) == '\\'))
                {
                    // have a '..' or '..\'
                    s++; // skip the following '.'
                    Skip(ref s);
                    d--; // back up to the previous '\'
                    while (d > 0 && dest[--d] != '\\') // back up to the previous '\'
                    {
                    }
                    d++; // move to the next position
                }
                else
                    last = dest[d++] = path[s - 1]; // copy the '.' we just skipped
            }
            else
                last = dest[d++] = path[s++]; // just copy it...
        }
        return new string(dest, 0, d);

        void Skip(ref int i)
        {
            while (At(path, i) == '\\')
                i++;
        }
    }

    private static bool IsHomeExpand(string path) => At(path, 0) == '~' && IsSep(At(path, 1));

    private static bool IsAbsolute(string path) =>
        IsSep(At(path, 0)) || (At(path, 0) != '\0' && At(path, 1) == ':' && IsSep(At(path, 2)));

    private static int GetPathDrive(string path)
    {
        if (At(path, 0) != '\0' && At(path, 1) == ':')
        {
            var drive = char.ToUpperInvariant(path[0]) - 'A';
            if (drive is >= 0 and <= 'Z' - 'A')
                return drive;
        }
        return -1;
    }

    /// <summary>Expands <paramref name="rpath"/> to an absolute path relative to the current directory.</summary>
    public static string FExpand(string rpath) => FExpand(rpath, GetCurDir(GetPathDrive(rpath)));

    /// <summary>Expands <paramref name="rpath"/> to an absolute path relative to <paramref name="relativeTo"/>, expanding a leading <c>~</c>.</summary>
    public static string FExpand(string rpath, string relativeTo)
    {
        int drv;
        // Prioritize drive letter in 'rpath'.
        if ((drv = GetPathDrive(rpath)) == -1 && (drv = GetPathDrive(relativeTo)) == -1)
            drv = GetDisk();
        var drive = $"{(char)(drv + 'A')}:";

        var flags = FnSplit(rpath, out _, out var dir, out var file, out var ext);
        if ((flags & DIRECTORY) == 0 || !IsSep(dir[0]))
        {
            var rbase = "";
            if (IsHomeExpand(dir) && GetHomeDir(ref drive, ref rbase)) // Home expansion. Overwrite drive if necessary.
                rbase += dir[1..]; // 'dir' begins with "~/" or "~\", so we can reuse the separator.
            else
            {
                // If 'rpath' is relative but contains a drive letter, just swap drives.
                if (GetPathDrive(rpath) != -1)
                {
                    if (GetCurDir(drv + 1, out rbase) != 0)
                        rbase = "";
                }
                else
                {
                    // Expand 'relativeTo'.
                    rbase = relativeTo;
                    if (!IsAbsolute(rbase))
                        rbase = FExpand(rbase, GetCurDir(drv));
                    // Skip drive letter in 'rbase'.
                    if (GetPathDrive(rbase) != -1)
                        rbase = rbase[2..];
                }
                // Ensure 'rbase' ends with a separator.
                if (!(rbase.Length > 0 && IsSep(rbase[^1])))
                    rbase += '\\';
                rbase += dir;
            }
            dir = IsSep(At(rbase, 0)) ? rbase : "\\" + rbase;
        }

        dir = Squeeze(dir.Replace('/', '\\'));
        return FnMerge(drive, dir, file, ext);
    }

    /// <summary>Whether <paramref name="drive"/> (a letter) exists. On Unix, only the emulated drive C does.</summary>
    public static bool DriveValid(char drive) =>
        OperatingSystem.IsWindows()
            ? Directory.GetLogicalDrives().Any(d => char.ToUpperInvariant(d[0]) == char.ToUpperInvariant(drive))
            : drive - 'A' == GetDisk();

    /// <summary>Whether <paramref name="str"/> names a directory.</summary>
    public static bool IsDir(string str) =>
        FindFirst(str, faDirec).FirstOrDefault() is { } r && (r.Attr & faDirec) != 0;

    /// <summary>Whether <paramref name="path"/> expands to an existing directory.</summary>
    public static bool PathValid(string path)
    {
        var expPath = FExpand(path);
        var len = expPath.Length;
        if (OperatingSystem.IsWindows())
        {
            if (len <= 3)
                return DriveValid(expPath[0]);
        }
        else if (len == 1 && IsSep(expPath[0]))
            return true; // Root directory is always valid.
        if (IsSep(expPath[len - 1]))
            expPath = expPath[..(len - 1)];
        return IsDir(expPath);
    }

    /// <summary>Whether <paramref name="fileName"/> is in an existing directory and has no illegal characters.</summary>
    public static bool ValidFileName(string fileName)
    {
        FnSplit(fileName, out var path, out var dir, out var name, out var ext);
        path += dir;
        if (dir.Length != 0 && !PathValid(path))
            return false;
        var extTail = ext.Length > 0 ? ext[1..] : "";
        return name.AsSpan().IndexOfAny(_illegalChars) < 0 &&
            extTail.AsSpan().IndexOfAny(_illegalChars) < 0 &&
            !extTail.Contains('.', StringComparison.Ordinal);
    }

    /// <summary>Whether <paramref name="f"/> contains <c>?</c> or <c>*</c>.</summary>
    public static bool IsWild(string f) => f.IndexOfAny(['?', '*']) >= 0;

    /// <summary>
    /// The entries matching <paramref name="pathname"/> (a directory and a wildcard) whose attributes match
    /// <paramref name="attrib"/>: normal files, plus those with any of the requested directory, hidden or
    /// system bits. Replaces upstream's <c>findfirst</c>/<c>findnext</c> pair.
    /// </summary>
    public static IEnumerable<TSearchRec> FindFirst(string pathname, int attrib)
    {
        // Reject empty pathnames.
        if (pathname.Length == 0)
            yield break;
        var searchDir = PathDos2Unix(pathname, !OperatingSystem.IsWindows());
        string wildcard;
        // Win32's FindFirst is designed to reject paths ending with a
        // separator. But legacy code unaware of Unix separators may be unable
        // to remove it and call findfirst with such a pathname. Therefore,
        // we handle this case gracefully.
        if (searchDir[^1] == '/')
            wildcard = ".";
        else
        {
            var lastSlash = searchDir.LastIndexOf('/');
            // When pathname doesn't contain a '/', wildcard keeps the whole
            // string and searchDir is set to the current directory.
            wildcard = searchDir[(lastSlash + 1)..];
            searchDir = lastSlash < 0 ? "./" : searchDir[..(lastSlash + 1)];
            // '*.*' stands for 'any name, any extension'.
            if (wildcard == "*.*")
                wildcard = "*";
        }

        string[] names;
        try
        {
            // readdir also lists '.' and '..'.
            names = [".", "..", .. Directory.EnumerateFileSystemEntries(searchDir).Select(Path.GetFileName).OfType<string>()];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
        {
            yield break;
        }

        foreach (var name in names)
        {
            if (!WildcardMatch(wildcard, 0, name, 0))
                continue;
            FileSystemInfo info = new DirectoryInfo(searchDir + name);
            if (!info.Exists)
                info = new FileInfo(searchDir + name);
            if (!info.Exists)
                continue;
            var fileAttr = CvtAttr(info, name);
            if ((fileAttr & SpecialBits) == 0 || (attrib & fileAttr & SpecialBits) != 0)
            {
                yield return new TSearchRec
                {
                    // shortcut: a directory's size is 0 here (no managed stat), upstream shows st_size; P/Invoke stat if it matters.
                    Size = info is FileInfo f ? unchecked((int)f.Length) : 0,
                    Attr = fileAttr,
                    Time = CvtTime(info.LastWriteTime),
                    Name = name,
                };
            }
        }
    }

    private static bool WildcardMatch(string wildcard, int w, string filename, int f)
    {
        for (; w < wildcard.Length; ++w)
        {
            switch (wildcard[w])
            {
                case '?':
                    if (f == filename.Length)
                        return false;
                    ++f;
                    break;
                case '*':
                    if (w + 1 == wildcard.Length)
                        return true;
                    for (var i = f; i < filename.Length; ++i)
                    {
                        if (WildcardMatch(wildcard, w + 1, filename, i))
                            return true;
                    }
                    return false;
                default:
                    if (f == filename.Length || filename[f] != wildcard[w])
                        return false;
                    ++f;
                    break;
            }
        }
        return f == filename.Length;
    }

    private static byte CvtAttr(FileSystemInfo info, string filename)
    {
        // Windows attributes share the DOS bit values.
        if (OperatingSystem.IsWindows())
            return (byte)((int)info.Attributes & (faRdOnly | faHidden | faSystem | faDirec | faArch));
        byte attr = faNormal;
        if (filename[0] == '.')
            attr |= faHidden;
        if (info is DirectoryInfo)
            attr |= faDirec;
        // shortcut: non-regular files (devices, FIFOs) are not marked faSystem; needs stat's st_mode.
        else if ((info.UnixFileMode & UnixFileMode.UserWrite) == 0) // If no write access, innacurate.
            attr |= faRdOnly;
        return attr;
    }

    private static int CvtTime(DateTime lt)
    {
        var date = lt.Day | (lt.Month << 5) | (((lt.Year - 1980) & 0x7F) << 9);
        var time = (lt.Second / 2) | (lt.Minute << 5) | (lt.Hour << 11);
        return (date << 16) | time;
    }
}
