using System;
using System.IO;
using System.Linq;

using SharpVision.Events;
using SharpVision.StdDlg;
using SharpVision.Tests.Dialogs;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.StdDlg.StdDlgCommands;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.StdDlg;

// Changes the process's current directory, so it runs in the non-parallel global state collection.
public sealed class TChDirDialogTests : CommandStateTest, IDisposable
{
    private readonly string _savedCwd = Environment.CurrentDirectory;
    private readonly string _dir;

    public TChDirDialogTests()
    {
        Environment.CurrentDirectory = Directory.CreateTempSubdirectory("sv-chd").FullName;
        // Read back, since macOS resolves /var to /private/var.
        _dir = Environment.CurrentDirectory;
        Directory.CreateDirectory(Path.Combine(_dir, "sub", "inner"));
    }

    public new void Dispose()
    {
        Environment.CurrentDirectory = _savedCwd;
        Directory.Delete(_dir, true);
        base.Dispose();
    }

    // The current directory as the dialog sees it: '\' separators, no drive on Unix.
    private static string Dos(string path) =>
        (OperatingSystem.IsWindows() ? path : path.Replace('/', '\\')).TrimEnd('\\');

    private static TChDirDialog Open()
    {
        var (_, desk) = DialogDesk.Create(60, 22);
        var d = new TChDirDialog(TChDirDialog.cdNormal, 3);
        desk.Insert(d);
        return d;
    }

    private static void Command(TChDirDialog d, ushort command)
    {
        var ev = new TEvent { What = evCommand, Message = new MessageEvent { Command = command } };
        d.HandleEvent(ref ev);
        Assert.Equal(evNothing, ev.What);
    }

    [Fact]
    public void Should_ShowPathToCurrentDirectory_Then_Subdirectories()
    {
        var d = Open();
        var list = d.DirList!.List()!;
        var leaf = Path.GetFileName(_dir);

        Assert.Equal(Dos(Environment.CurrentDirectory), d.DirInput!.Data);
        // The current directory is the last entry on the path, and it is focused and selected.
        var cur = list.Count - 2;
        Assert.EndsWith("└─┬" + leaf, list[cur].Text, StringComparison.Ordinal);
        Assert.Equal((cur, true), (d.DirList.Focused, d.DirList.IsSelected(cur)));
        // A single subdirectory closes the tree with '└──', indented one step further.
        Assert.Equal(new string(' ', list[cur].Text.IndexOf('└', StringComparison.Ordinal) + 2) + "└──sub", list[^1].Text);
        Assert.Equal(Dos(_dir) + "\\sub", list[^1].Dir);
    }

    [Fact]
    public void Should_FollowChdir_And_Revert()
    {
        var d = Open();
        d.DirList!.FocusItem(d.DirList.Range - 1);

        Command(d, cmChangeDir);

        Assert.Equal(Dos(_dir) + "\\sub", d.DirInput!.Data);
        Assert.EndsWith("└──inner", d.DirList.List()![^1].Text, StringComparison.Ordinal);
        Assert.Same(d.DirList, d.Current);

        Command(d, cmRevert);
        Assert.Equal(Dos(_dir), d.DirInput.Data);
    }

    [Fact]
    public void Should_ChangeProcessDirectory_On_OK()
    {
        var d = Open();
        d.DirInput!.Data = "sub";

        Assert.True(d.Valid(cmOK));
        Assert.Equal(Path.Combine(_dir, "sub"), Environment.CurrentDirectory);
        Assert.True(d.Valid(cmCancel));
    }

    [Fact]
    public void Should_ListSubdirectories_With_MiddleAndLastGraphics()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "other"));
        var d = Open();
        var tail = d.DirList!.List()!.TakeLast(2).Select(e => e.Text.TrimStart()).ToArray();

        // Directory order is unspecified: the first gets '└┬─', the last is closed with ' └─'.
        Assert.Matches("^└┬─(sub|other)$", tail[0]);
        Assert.Matches("^└─(sub|other)$", tail[1]);
        Assert.NotEqual(tail[0][3..], tail[1][2..]);
    }
}
