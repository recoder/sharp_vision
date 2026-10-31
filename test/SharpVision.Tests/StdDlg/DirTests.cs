using System;
using System.IO;
using System.Linq;

using SharpVision.StdDlg;
using Xunit;

using static SharpVision.StdDlg.Dir;

namespace SharpVision.Tests.StdDlg;

// Expected values come from upstream's fnsplit, squeeze and fexpand compiled on Linux.
public sealed class DirTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("sv-dir").FullName;

    public DirTests()
    {
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "12345");
        File.WriteAllText(Path.Combine(_dir, "b.c"), "");
        Directory.CreateDirectory(Path.Combine(_dir, "sub"));
    }

    public void Dispose() => Directory.Delete(_dir, true);

    // An absolute Unix path as FExpand returns it on this platform. On Windows it takes the drive of
    // the path, else of relativeTo, else the current drive (D: on the CI runners).
    private static string Native(string unix, string path, string relativeTo)
    {
        if (!OperatingSystem.IsWindows())
            return unix;
        static bool HasDrive(string s) => s.Length > 1 && s[1] == ':';
        var drive = HasDrive(path) ? path[..2] : HasDrive(relativeTo) ? relativeTo[..2] : Environment.CurrentDirectory[..2];
        return drive.ToUpperInvariant() + unix.Replace('/', '\\');
    }

    [Theory]
    [InlineData(@"C:\dir\file.txt", 30, "C:", @"\dir\", "file", ".txt")]
    [InlineData("a/b/c.tar.gz", 14, "", "a/b/", "c.tar", ".gz")]
    [InlineData("a/..", 8, "", "a/..", "", "")]
    [InlineData("*.*", 7, "", "", "*", ".*")]
    [InlineData("dir/x*.c?", 15, "", "dir/", "x*", ".c?")]
    [InlineData(".bashrc", 2, "", "", "", ".bashrc")]
    [InlineData("C:", 16, "C:", "", "", "")]
    public void Should_SplitPath(string path, int flags, string drive, string dir, string name, string ext)
    {
        Assert.Equal((flags, drive, dir, name, ext), (FnSplit(path, out var d, out var di, out var n, out var e), d, di, n, e));
    }

    [Theory]
    [InlineData(@"\a\.\b\..\c", @"\a\c")]
    [InlineData(@"\a\\b\", @"\a\b\")]
    [InlineData(@"\..\x", @"\x")]
    [InlineData(@"a\..\..\b", "ab")] // UPSTREAM: '..' past a relative start eats the separator.
    [InlineData(@"\a\.b\c", @"\a\.b\c")]
    [InlineData(@"\a\b\..", @"\a\")]
    public void Should_SqueezeDots(string path, string expected) => Assert.Equal(expected, Squeeze(path));

    [Theory]
    [InlineData("x.txt", @"C:\base\", "/base/x.txt")]
    [InlineData("sub/y", "/base", "/base/sub/y")]
    [InlineData("../z", @"C:\base\dir\", "/base/z")]
    [InlineData("/abs/f", @"C:\base\", "/abs/f")]
    [InlineData("*.*", @"C:\home\u\", "/home/u/*.*")]
    [InlineData("a/./b/../c.d", "/r", "/r/a/c.d")]
    [InlineData(@"C:\x\..\y", "/base", "/y")]
    public void Should_ExpandPath(string path, string relativeTo, string expected) =>
        Assert.Equal(Native(expected, path, relativeTo), FExpand(path, relativeTo));

    [Fact]
    public void Should_ExpandHome()
    {
        if (OperatingSystem.IsWindows())
            return; // Uses HOMEDRIVE and HOMEPATH instead.
        Assert.Equal(Environment.GetEnvironmentVariable("HOME")!.TrimEnd('/') + "/f", FExpand("~/f", "/base"));
    }

    [Fact]
    public void Should_FindMatchingEntries()
    {
        var all = Path.Combine(_dir, "*.*");

        Assert.Equal(["a.txt", "b.c"], FindFirst(all, faRdOnly | faArch).Select(r => r.Name).Order());
        Assert.Equal([".", "..", "a.txt", "b.c", "sub"], FindFirst(all, faDirec).Select(r => r.Name).Order(StringComparer.Ordinal));
        Assert.Equal(["b.c"], FindFirst(Path.Combine(_dir, "?.c"), 0).Select(r => r.Name));
        Assert.Equal(5, FindFirst(Path.Combine(_dir, "a.txt"), 0).Single().Size);
    }

    [Fact]
    public void Should_ValidateDirectoriesAndNames()
    {
        Assert.True(IsDir(Path.Combine(_dir, "sub")));
        Assert.False(IsDir(Path.Combine(_dir, "a.txt")));
        Assert.True(PathValid(_dir + Path.DirectorySeparatorChar));
        Assert.False(PathValid(Path.Combine(_dir, "nope")));
        Assert.True(ValidFileName(Path.Combine(_dir, "new.txt")));
        Assert.False(ValidFileName(Path.Combine(_dir, "nope", "new.txt")));
        Assert.False(ValidFileName("a<b.txt"));
        Assert.True(IsWild("*.c") && !IsWild("a.c"));
    }
}
