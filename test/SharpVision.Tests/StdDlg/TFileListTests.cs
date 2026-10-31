using System;
using System.IO;
using System.Linq;

using SharpVision.Dialogs;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.StdDlg;
using SharpVision.Tests.Dialogs;
using SharpVision.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;
using static SharpVision.StdDlg.Dir;
using static SharpVision.StdDlg.StdDlgCommands;

namespace SharpVision.Tests.StdDlg;

public sealed class TFileListTests : CommandStateTest, IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("sv-files").FullName;

    // Records the entry of the last cmFileFocused broadcast.
    private sealed class Probe() : TView(new TRect(0, 0, 0, 0))
    {
        public TSearchRec? Focused;

        public override void HandleEvent(ref TEvent ev)
        {
            if (ev.What == evBroadcast && ev.Message.Command == cmFileFocused)
                Focused = (TSearchRec?)ev.Message.InfoPtr;
        }
    }

    public TFileListTests()
    {
        foreach (var f in new[] { "b.txt", "a.txt", "x.c" })
            File.WriteAllText(Path.Combine(_dir, f), "");
        Directory.CreateDirectory(Path.Combine(_dir, "sub"));
        Directory.CreateDirectory(Path.Combine(_dir, ".git"));
    }

    public new void Dispose()
    {
        Directory.Delete(_dir, true);
        base.Dispose();
    }

    private static (TFileList List, Probe Probe) Setup()
    {
        TView.ShowMarkers = false;
        var (_, desk) = DialogDesk.Create();
        var d = new TDialog(new TRect(0, 0, 40, 10), null);
        var probe = new Probe { EventMask = evBroadcast };
        var list = new TFileList(new TRect(1, 1, 31, 6), null);
        d.Insert(probe);
        d.Insert(list);
        desk.Insert(d);
        return (list, probe);
    }

    [Fact]
    public void Should_ListMatchingFiles_Then_Directories_Then_Parent()
    {
        var (list, probe) = Setup();

        list.ReadDirectory(_dir + Path.DirectorySeparatorChar, "*.txt");

        Assert.Equal(["a.txt", "b.txt", "sub\\", "..\\"], Enumerable.Range(0, list.Range).Select(i => list.GetText(i, 255)));
        Assert.Same(list.List()![0], probe.Focused);
        Assert.Equal(0, list.DataSize());
    }

    [Fact]
    public void Should_ListOnlyParent_When_DirectoryMissing()
    {
        var (list, probe) = Setup();

        list.ReadDirectory(Path.Combine(_dir, "nope", "*.*"));

        Assert.Equal("..", Assert.Single(list.List()!).Name);
        Assert.Equal("..", probe.Focused!.Name);
    }

    [Fact]
    public void Should_SearchDirectories_When_ShiftHeld()
    {
        var (list, probe) = Setup();
        list.ReadDirectory(Path.Combine(_dir, "*.*"));

        // Without Shift, 's' searches the files and stops at "x.c", which does not match.
        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = 's' } };
        list.HandleEvent(ref ev);
        Assert.Equal(0, list.Focused);

        ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = 'S', ControlKeyState = kbShift } };
        list.HandleEvent(ref ev);
        Assert.Equal("sub\\", list.GetText(list.Focused, 255));
        Assert.Equal("sub", probe.Focused!.Name);
    }

    [Fact]
    public void Should_OrderFiles_Before_Directories_And_Parent_Last()
    {
        var c = new TFileCollection();
        c.Insert(new TSearchRec { Name = "..", Attr = faDirec });
        c.Insert(new TSearchRec { Name = "a", Attr = faDirec });
        c.Insert(new TSearchRec { Name = "z" });
        c.Insert(new TSearchRec { Name = "b" });
        c.Insert(new TSearchRec { Name = "b" });

        Assert.Equal(["b", "z", "a", ".."], c.Select(r => r.Name));
    }
}
