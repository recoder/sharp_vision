using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Streams;

namespace SharpVision.Help;

/// <summary>The paragraphs and cross-references of one help topic, wrapped into lines on demand.</summary>
/// <remarks>
/// Help files store paragraphs as bytes and cross-reference offsets and lengths in bytes.
/// <see cref="Read"/> decodes valid UTF-8 as such and other bytes as CP437, as upstream's text
/// functions display them (the upstream demo help is CP437); <see cref="Write"/> writes UTF-8,
/// except U+00A0, written as byte 0xFF like the help compiler writes the spaces of link text.
/// Both convert the cross-references to and from UTF-16 code units.
/// </remarks>
/// Ported from source/tvision/helpbase.cpp, include/tvision/helpbase.h (class THelpTopic).
public class THelpTopic : TObject
{
    /// <summary>The class name in streams.</summary>
    public const string Name = "THelpTopic";

    /// <summary>The first paragraph, or null.</summary>
    public TParagraph? Paragraphs;

    /// <summary>The number of cross-references.</summary>
    public int NumRefs;

    /// <summary>The cross-references, in text order.</summary>
    public TCrossRef[] CrossRefs = [];

    private int _width;
    private int _lastOffset;
    private int _lastLine = int.MaxValue;
    private TParagraph? _lastParagraph;

    /// <summary>Appends a cross-reference.</summary>
    public void AddCrossRef(TCrossRef aRef)
    {
        var p = new TCrossRef[NumRefs + 1];
        CrossRefs.AsSpan(0, NumRefs).CopyTo(p);
        CrossRefs = p;
        CrossRefs[NumRefs] = aRef;
        ++NumRefs;
    }

    /// <summary>Appends a paragraph.</summary>
    public void AddParagraph(TParagraph p)
    {
        if (Paragraphs is null)
            Paragraphs = p;
        else
        {
            var back = Paragraphs;
            while (back.Next is not null)
                back = back.Next;
            back.Next = p;
        }
        p.Next = null;
    }

    /// <summary>
    /// Finds cross-reference <paramref name="i"/> in the wrapped text: its column and 1-based
    /// line, its width in columns and its target.
    /// </summary>
    public void GetCrossRef(int i, out TPoint loc, out byte length, out int aRef)
    {
        var paraOffset = 0;
        var curOffset = 0;
        var line = 0;
        var crossRef = CrossRefs[i];
        var offset = crossRef.Offset;
        // UPSTREAM: no end-of-list check; an offset past the text dereferences a null paragraph.
        var p = Paragraphs!;
        while (true)
        {
            var lineOffset = curOffset;
            WrapText(p.Text, ref curOffset, p.Wrap);
            ++line;
            if (offset <= paraOffset + curOffset)
            {
                var refOffset = offset - (paraOffset + lineOffset) - 1;
                var textBefore = p.Text.AsSpan(lineOffset, refOffset);
                var refText = p.Text.AsSpan(lineOffset + refOffset, crossRef.Length);
                loc = new TPoint(TText.Width(textBefore), line);
                length = (byte)TText.Width(refText);
                aRef = crossRef.Ref;
                return;
            }
            if (curOffset >= p.Size)
            {
                paraOffset += p.Size;
                p = p.Next!;
                curOffset = 0;
            }
        }
    }

    /// <summary>The 1-based wrapped line <paramref name="line"/>, without trailing blanks; empty past the end.</summary>
    public string GetLine(int line)
    {
        int offset;
        TParagraph? p;
        if (_lastLine < line)
        {
            var i = line;
            line -= _lastLine;
            _lastLine = i;
            offset = _lastOffset;
            p = _lastParagraph;
        }
        else
        {
            p = Paragraphs;
            offset = 0;
            _lastLine = line;
        }
        while (p is not null)
        {
            while (offset < p.Size)
            {
                --line;
                var lineText = WrapText(p.Text, ref offset, p.Wrap);
                if (line == 0)
                {
                    _lastOffset = offset;
                    _lastParagraph = p;
                    return lineText;
                }
            }
            p = p.Next;
            offset = 0;
        }
        return "";
    }

    /// <summary>The number of cross-references.</summary>
    public int GetNumCrossRefs() => NumRefs;

    /// <summary>The width in columns of the widest wrapped line.</summary>
    public int LongestLineWidth()
    {
        var maxWidth = 0;
        var lineCount = NumLines();
        for (var i = 1; i <= lineCount; ++i)
            maxWidth = Math.Max(maxWidth, TText.Width(GetLine(i)));
        return maxWidth;
    }

    /// <summary>The number of wrapped lines.</summary>
    public int NumLines()
    {
        var lines = 0;
        for (var p = Paragraphs; p is not null; p = p.Next)
        {
            var offset = 0;
            while (offset < p.Size)
            {
                ++lines;
                WrapText(p.Text, ref offset, p.Wrap);
            }
        }
        return lines;
    }

    /// <summary>Replaces cross-reference <paramref name="i"/>, if it exists.</summary>
    public void SetCrossRef(int i, TCrossRef aRef)
    {
        if (i < NumRefs)
            CrossRefs[i] = aRef;
    }

    /// <summary>Resizes the cross-reference array to <paramref name="i"/> entries, keeping the first ones.</summary>
    public void SetNumCrossRefs(int i)
    {
        if (NumRefs == i)
            return;
        var p = new TCrossRef[i];
        CrossRefs.AsSpan(0, Math.Min(i, NumRefs)).CopyTo(p);
        CrossRefs = p;
        NumRefs = i;
    }

    /// <summary>Sets the width that wrapped paragraphs are wrapped to.</summary>
    public void SetWidth(int aWidth) => _width = aWidth;

    /// <summary>Reads the object's data.</summary>
    public void Read(PStream s)
    {
        var raw = new List<byte>();
        TParagraph? last = null;
        for (var i = s.ReadInt(); i > 0; --i)
        {
            var size = s.ReadWord();
            var wrap = s.ReadInt() != 0;
            var bytes = s.ReadBytes(size);
            raw.AddRange(bytes);
            var p = new TParagraph { Wrap = wrap, Text = Decode(bytes) };
            if (last is null)
                Paragraphs = p;
            else
                last.Next = p;
            last = p;
        }

        var all = raw.ToArray();
        NumRefs = s.ReadInt();
        CrossRefs = new TCrossRef[NumRefs];
        for (var i = 0; i < NumRefs; ++i)
        {
            var aRef = s.ReadInt();
            var offset = s.ReadInt();
            var length = s.ReadByte();
            var (start, len) = Convert(all.Length, offset, length, (a, b) => Decode(all.AsSpan(a, b)).Length);
            CrossRefs[i] = new TCrossRef { Ref = aRef, Offset = start, Length = (byte)len };
        }
        _width = 0;
        _lastLine = int.MaxValue;
    }

    /// <summary>Writes the object's data, passing each target through <see cref="HelpBase.CrossRefHandler"/> if one is set.</summary>
    public void Write(PStream s)
    {
        var text = new StringBuilder();
        var count = 0;
        for (var p = Paragraphs; p is not null; p = p.Next)
            count++;
        s.WriteInt(count);
        for (var p = Paragraphs; p is not null; p = p.Next)
        {
            var bytes = Encode(p.Text);
            s.WriteWord((ushort)bytes.Length);
            s.WriteInt(p.Wrap ? 1 : 0);
            s.WriteBytes(bytes);
            text.Append(p.Text);
        }

        var all = text.ToString();
        s.WriteInt(NumRefs);
        var handler = HelpBase.CrossRefHandler;
        var notAssigned = handler == HelpBase.NotAssigned;
        for (var i = 0; i < NumRefs; ++i)
        {
            var r = CrossRefs[i];
            var (offset, length) = Convert(all.Length, r.Offset, r.Length, (a, b) => Encode(all.AsSpan(a, b)).Length);
            if (notAssigned)
                s.WriteInt(r.Ref);
            else
                handler(s, r.Ref);
            s.WriteInt(offset);
            s.WriteByte((byte)length);
        }
    }

    // Converts a 1-based offset and a length between byte and code-unit units, given a
    // function counting the target units in a source range of the topic text.
    private static (int Offset, int Length) Convert(int total, int offset, int length, Func<int, int, int> count)
    {
        var start = Math.Clamp(offset - 1, 0, total);
        var end = Math.Clamp(start + length, start, total);
        return (offset - 1 - start + count(0, start) + 1, count(start, end - start));
    }

    private static string Decode(ReadOnlySpan<byte> bytes)
    {
        var sb = new StringBuilder(bytes.Length);
        while (!bytes.IsEmpty)
        {
            if (Rune.DecodeFromUtf8(bytes, out var r, out var n) == OperationStatus.Done)
                sb.Append(r.ToString());
            else
            {
                sb.Append(TText.FromCodePage(bytes[0]));
                n = 1;
            }
            bytes = bytes[n..];
        }
        return sb.ToString();
    }

    private static byte[] Encode(ReadOnlySpan<char> text)
    {
        var bytes = new List<byte>(text.Length);
        Span<byte> buf = stackalloc byte[4];
        foreach (var r in text.EnumerateRunes())
        {
            if (r.Value == 0xA0)
                bytes.Add(0xFF);
            else
                bytes.AddRange(buf[..r.EncodeToUtf8(buf)]);
        }
        return [.. bytes];
    }

    private static bool IsBlank(char ch) => ch is ' ' or '\t' or '\n' or '\v' or '\f' or '\r';

    private string WrapText(string text, ref int offset, bool wrap)
    {
        var lineEnd = text.IndexOf('\n', offset);
        var line = text.AsSpan(offset, (lineEnd < 0 ? text.Length : lineEnd + 1) - offset);
        if (wrap)
        {
            var wrappedSize = TText.Scroll(line, _width, false);
            if (0 < wrappedSize && wrappedSize < line.Length)
            {
                var newSize = wrappedSize;
                // Omit the last word if it was cut off by wrapping.
                while (newSize > 0 && !IsBlank(line[newSize]))
                    --newSize;
                // Unless it fills the whole line.
                if (newSize == 0)
                    newSize = wrappedSize;
                // If a space follows, keep it so 'offset' points past it.
                if (newSize < text.Length && IsBlank(line[newSize]))
                    ++newSize;
                line = line[..newSize];
            }
        }
        offset += line.Length;
        return line.TrimEnd(" \t\n\v\f\r").ToString();
    }
}
