using System;
using System.IO;
using System.Linq;

using SharpVision.App;
using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Platform;
using SharpVision.StdDlg;
using SharpVision.Tests.Dialogs;
using Xunit;

using static SharpVision.StdDlg.StdDlgCommands;
using static SharpVision.StdDlg.TFileDialog;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.StdDlg;

public sealed class TFileDialogTests : CommandStateTest, IDisposable
{
    private readonly ushort _width = TScreen.ScreenWidth, _height = TScreen.ScreenHeight;
    private readonly TScreenCell[]? _buffer = TScreen.ScreenBuffer;
    private readonly ScriptedProgram _app;
    private readonly string _dir = Directory.CreateTempSubdirectory("sv-fdlg").FullName;
    private readonly char _sep = Path.DirectorySeparatorChar;

    public TFileDialogTests()
    {
        TScreen.ScreenWidth = 80;
        TScreen.ScreenHeight = 25;
        TScreen.ScreenBuffer = new TScreenCell[80 * 25];
        _app = new ScriptedProgram();
        File.WriteAllText(Path.Combine(_dir, "a.txt"), "hello");
        File.SetLastWriteTime(Path.Combine(_dir, "a.txt"), new DateTime(2024, 3, 7, 13, 5, 0));
        File.WriteAllText(Path.Combine(_dir, "b.c"), "");
        Directory.CreateDirectory(Path.Combine(_dir, "sub"));
    }

    public new void Dispose()
    {
        TObject.Destroy(_app);
        TScreen.ScreenWidth = _width;
        TScreen.ScreenHeight = _height;
        TScreen.ScreenBuffer = _buffer;
        Directory.Delete(_dir, true);
        base.Dispose();
    }

    private TFileDialog Open(string wildCard)
    {
        var d = new TFileDialog("*.*", "Open a file", "~N~ame", fdOpenButton | fdNoLoadDir, 7);
        d.SetData(new object?[] { Path.Combine(_dir, wildCard) });
        return d;
    }

    private static string[] Names(TFileDialog d) => d.FileList!.List()!.Select(r => r.Name).ToArray();

    [Fact]
    public void Should_ListWildcard_When_SetToPattern()
    {
        var d = Open("*.txt");

        Assert.Equal("*.txt", d.WildCard);
        Assert.Equal(["a.txt", "sub", ".."], Names(d));
        // The line keeps the typed pattern; it is selected, so it does not follow the list.
        Assert.Same(d.FileName, d.Current);
        Assert.EndsWith("*.txt", d.FileName!.Data, StringComparison.Ordinal);
        // 80 columns: the dialog spans the screen less 7 on each side.
        Assert.Equal((66, 19), (d.Size.X, d.Size.Y));
    }

    [Fact]
    public void Should_ChangeDirectory_Instead_Of_Closing()
    {
        var d = Open("*.*");
        d.FileName!.Data = "sub";

        Assert.False(d.Valid(cmFileOpen));
        Assert.Equal([".."], Names(d));
        Assert.Same(d.FileList, d.Current);
        Assert.False(d.Valid(cmFileOpen)); // The line now shows the focused '..' entry.
        Assert.Equal(["a.txt", "b.c", "sub", ".."], Names(d));
    }

    [Fact]
    public void Should_ReturnExpandedFileName()
    {
        var d = Open("*.*");
        d.FileName!.Data = "new.txt";

        var rec = new object?[1];
        d.GetData(rec);

        Assert.True(d.Valid(cmFileOpen));
        Assert.Equal(Path.Combine(_dir, "new.txt"), rec[0]);
        Assert.True(d.Valid(cmCancel));
    }

    [Fact]
    public void Should_FollowFocusedFile_In_LineAndInfoPane()
    {
        var d = Open("*.*");
        var (screen, desk) = DialogDesk.Create(80, 25);
        desk.Insert(d);
        string Screen() => string.Join('\n', Enumerable.Range(0, 25).Select(screen.Row));
        d.FileList!.Select();

        d.FileList.FocusItem(2);
        Assert.Equal("sub\\*.*", d.FileName!.Data);

        d.FileList.FocusItem(0);
        Assert.Equal("a.txt", d.FileName.Data);
        Assert.Contains(_dir + _sep + "*.*", Screen(), StringComparison.Ordinal);
        Assert.Matches(@"a\.txt +5 +Mar 07,2024  01:05p", Screen());

        // UPSTREAM: drawing stores the 12-hour value, so the redrawn time reads as AM.
        d.FirstThat(v => v is TFileInfoPane)!.DrawView();
        Assert.Matches("01:05a", Screen());
    }

    [Fact]
    public void Should_EndModal_On_FileCommands()
    {
        var d = Open("*.*");
        _app.Events.Enqueue(new() { What = SharpVision.Events.EventCodes.evCommand, Message = new() { Command = cmFileClear } });

        Assert.Equal(cmFileClear, TProgram.DeskTop!.ExecView(d));
    }
}
