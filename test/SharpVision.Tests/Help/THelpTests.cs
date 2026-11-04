using System;
using System.IO;
using System.Text;

using SharpVision.Events;
using SharpVision.Help;
using SharpVision.Objects;
using SharpVision.Streams;
using SharpVision.Tests.Dialogs;
using SharpVision.Tests.Views;
using Xunit;

using static SharpVision.Events.EventCodes;
using static SharpVision.Events.KeyCodes;

namespace SharpVision.Tests.Help;

public sealed class THelpTests : CommandStateTest
{
    private static THelpTopic Topic(string text, bool wrap, params (int Offset, byte Length, int Ref)[] refs)
    {
        var t = new THelpTopic();
        t.AddParagraph(new TParagraph { Text = text, Wrap = wrap });
        foreach (var (offset, length, r) in refs)
            t.AddCrossRef(new TCrossRef { Offset = offset, Length = length, Ref = r });
        return t;
    }

    // Writes topics for contexts 1 and 2 the way the help compiler does, and returns the file bytes.
    private static byte[] HelpFileBytes()
    {
        var ms = new MemoryStream();
        var f = new THelpFile(new PStream(ms));
        f.RecordPositionInIndex(1);
        f.PutTopic(Topic("Café: see Next here.\n", false, (11, 4, 2)));
        f.RecordPositionInIndex(2);
        f.PutTopic(Topic("Second\n", false));
        TObject.Destroy(f);
        return ms.ToArray();
    }

    [Fact]
    public void Should_WritePointersInUpstreamFormat()
    {
        var ms = new MemoryStream();
        var s = new PStream(ms);
        s.WritePointerPrefix("AB");
        s.WriteWord(0x0102);
        s.WriteSuffix();
        s.WritePointerPrefix(null);

        Assert.Equal(new byte[] { 2, (byte)'[', 2, (byte)'A', (byte)'B', 2, 1, (byte)']', 0 }, ms.ToArray());
        s.Seekg(0);
        Assert.Equal("AB", s.ReadPointerPrefix());
        Assert.Equal(0x0102, s.ReadWord());
        s.ReadSuffix();
        Assert.Null(s.ReadPointerPrefix());
    }

    [Fact]
    public void Should_WrapAtWordsAndKeepLines_When_NotWrapped()
    {
        var t = Topic("aaa bbb ccc\n", true);
        t.AddParagraph(new TParagraph { Text = "x y\nlonger line\n", Wrap = false });
        t.SetWidth(7);

        Assert.Equal(4, t.NumLines());
        Assert.Equal(["aaa bbb", "ccc", "x y", "longer line", ""], [t.GetLine(1), t.GetLine(2), t.GetLine(3), t.GetLine(4), t.GetLine(5)]);
        Assert.Equal(11, t.LongestLineWidth());
    }

    [Fact]
    public void Should_LocateCrossRefInWrappedLines()
    {
        var t = Topic("aaa bbb ccc\n", true, (9, 3, 7));
        t.SetWidth(7);

        t.GetCrossRef(0, out var loc, out var length, out var r);

        Assert.Equal((0, 2, 3, 7), (loc.X, loc.Y, (int)length, r));
    }

    [Fact]
    public void Should_RoundTripTopics_With_ByteOffsetsInTheFile()
    {
        var bytes = HelpFileBytes();

        Assert.Equal("FBHF", Encoding.ASCII.GetString(bytes, 0, 4));
        // The cross-reference as stored: ref 2, byte offset 12 ('é' is two bytes), length 4.
        Assert.True(bytes.AsSpan().IndexOf(new byte[] { 2, 0, 0, 0, 12, 0, 0, 0, 4 }) > 0);
        var f = new THelpFile(new PStream(new MemoryStream(bytes)));
        var t = f.GetTopic(1);
        Assert.Equal("Café: see Next here.", t.GetLine(1));
        Assert.Equal(11, t.CrossRefs[0].Offset); // 12 in the file: 'é' is two bytes
        t.GetCrossRef(0, out var loc, out _, out var r);
        Assert.Equal((10, 1, 2), (loc.X, loc.Y, r));
        Assert.Equal(THelpFile.InvalidContext.TrimEnd(), f.GetTopic(3).GetLine(1) + "\n" + f.GetTopic(3).GetLine(2));
    }

    [Fact]
    public void Should_HighlightCrossRefs_And_FollowThemWithEnter()
    {
        var (screen, desk) = DialogDesk.Create(24, 3);
        var f = new THelpFile(new PStream(new MemoryStream(HelpFileBytes())));
        var v = new THelpViewer(new TRect(0, 0, 24, 3), null, null, f, 1);
        desk.Insert(v);

        Assert.Equal("Café: see Next here.", screen.Row(0)[..20]);
        Assert.Equal((6, 8, 6), ((int)screen.AttrAt(9, 0), (int)screen.AttrAt(10, 0), (int)screen.AttrAt(14, 0)));

        var ev = new TEvent { What = evKeyDown, KeyDown = new KeyDownEvent { KeyCode = kbEnter } };
        v.HandleEvent(ref ev);

        Assert.Equal("Second", screen.Row(0)[..6]);
    }
}
