using System;
using System.IO;

using SharpVision.Editors;
using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Tests.Dialogs;
using Xunit;

using static SharpVision.Editors.EditorCommands;
using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests.Editors;

public sealed class TFileEditorTests : CommandStateTest, IDisposable
{
    private readonly TEditorDialog _savedDialog = TEditor.EditorDialog;
    private readonly ushort _savedFlags = TEditor.EditorFlags;
    private readonly string _dir = Directory.CreateTempSubdirectory("sv-fed").FullName;

    public new void Dispose()
    {
        TEditor.EditorDialog = _savedDialog;
        TEditor.EditorFlags = _savedFlags;
        Directory.Delete(_dir, true);
        base.Dispose();
    }

    private string PathOf(string name) => Path.Combine(_dir, name);

    private static TFileEditor Open(string fileName) => new(new TRect(0, 0, 20, 5), null, null, null, fileName);

    private static string AllText(TEditor e)
    {
        var buf = new char[e.BufLen];
        e.GetText(0, buf);
        return new string(buf);
    }

    [Fact]
    public void Should_LoadFile_Into_A4KBuffer()
    {
        File.WriteAllText(PathOf("a.txt"), "héllo\n");

        var e = Open(PathOf("a.txt"));

        Assert.Equal((true, "héllo\n", 0x1000), (e.IsValid, AllText(e), e.BufSize));
        Assert.Equal(PathOf("a.txt"), e.FileName);
    }

    [Fact]
    public void Should_StartEmpty_When_FileIsMissing()
    {
        var e = Open(PathOf("missing.txt"));

        Assert.Equal((true, 0), (e.IsValid, e.BufLen));
    }

    [Fact]
    public void Should_GrowBuffer_And_KeepTextAfterTheGap()
    {
        File.WriteAllText(PathOf("a.txt"), "head|tail");
        var e = Open(PathOf("a.txt"));
        e.SetCurPtr(5, 0);

        e.InsertText(new string('x', 5000), false);

        Assert.Equal(0x2000, e.BufSize);
        Assert.Equal("head|" + new string('x', 5000) + "tail", AllText(e));
    }

    [Fact]
    public void Should_SaveAsUtf8_And_KeepBackup()
    {
        TEditor.EditorFlags = efBackupFiles;
        File.WriteAllText(PathOf("a.txt"), "old");
        var e = Open(PathOf("a.txt"));
        e.SetCurPtr(3, 0);
        e.InsertText("€", false);

        Assert.True(e.Modified);
        Assert.True(e.Save());

        Assert.False(e.Modified);
        Assert.Equal("old€"u8.ToArray(), File.ReadAllBytes(PathOf("a.txt")));
        Assert.Equal("old", File.ReadAllText(PathOf("a.bak")));
    }

    [Fact]
    public void Should_SaveUntitled_ToTheChosenName()
    {
        TEditor.EditorFlags = 0;
        var asked = -1;
        TEditor.EditorDialog = (dialog, args) =>
        {
            asked = dialog;
            ((object?[])args[0]!)[0] = PathOf("new.txt");
            return cmOK;
        };
        var (_, desk) = DialogDesk.Create(30, 10);
        var w = new TEditWindow(new TRect(0, 0, 30, 10), "", 1);
        desk.Insert(w);
        Assert.Equal("Untitled", w.GetTitle(80));
        w.Editor.InsertText("hi", false);

        var ev = new TEvent { What = evCommand, Message = new MessageEvent { Command = cmSave } };
        w.Editor.HandleEvent(ref ev);

        Assert.Equal((edSaveAs, evNothing), (asked, ev.What));
        Assert.Equal("hi", File.ReadAllText(PathOf("new.txt")));
        Assert.Equal(PathOf("new.txt"), w.GetTitle(80));
    }

    [Theory]
    [InlineData(cmYes, true, false)]
    [InlineData(cmNo, true, false)]
    [InlineData(cmCancel, false, true)]
    public void Should_AskToSave_When_ClosingModified(ushort answer, bool valid, bool stillModified)
    {
        TEditor.EditorFlags = 0;
        var asked = -1;
        TEditor.EditorDialog = (dialog, _) =>
        {
            asked = dialog;
            return answer;
        };
        var e = Open(PathOf("a.txt"));
        e.InsertText("x", false);

        Assert.Equal(valid, e.Valid(cmClose));
        Assert.Equal((edSaveModify, stillModified), (asked, e.Modified));
        Assert.Equal(answer == cmYes, File.Exists(PathOf("a.txt")));
    }
}
