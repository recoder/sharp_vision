using System;
using System.Collections.Generic;
using System.IO;
using SharpVision.Drawing;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Dialogs.MsgBox;
using static SharpVision.Objects.Util;
using static SharpVision.Views.ViewFlags;

/// <summary>A scrolling view of a text file's lines.</summary>
/// Ported from examples/tvdemo/fileview.h, fileview.cpp (class TFileViewer).
internal sealed class TFileViewer : TScroller
{
    public string FileName = "";
    public List<string> FileLines = [];
    public bool IsValid;

    public TFileViewer(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar, string aFileName)
        : base(bounds, aHScrollBar, aVScrollBar)
    {
        GrowMode = (byte)(gfGrowHiX | gfGrowHiY);
        IsValid = true;
        ReadFile(aFileName);
    }

    public override void Draw()
    {
        var c = GetColor(1);
        for (var i = 0; i < Size.Y; i++)
        {
            var b = new TDrawBuffer();
            b.MoveChar(0, ' ', c, Size.X);

            if (Delta.Y + i < FileLines.Count)
                b.MoveStr(0, FileLines[Delta.Y + i], c, Size.X, Delta.X);
            WriteBuf(0, i, Size.X, 1, b);
        }
    }

    public override void ScrollDraw()
    {
        base.ScrollDraw();
        Draw();
    }

    public void ReadFile(string fName)
    {
        Limit.X = 0;
        FileName = fName;
        FileLines = [];
        string text;
        try
        {
            text = File.ReadAllText(fName);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            MessageBox($"Failed to open file '{fName}'.", mfError | mfOKButton);
            IsValid = false;
            Limit.Y = 0;
            return;
        }
        var i = 0;
        while (i < text.Length)
        {
            var start = i;
            while (i < text.Length && text[i] != '\n' && text[i] != '\r') // read a whole line
                i++;
            var line = text[start..i].Replace('\0', ' ');
            if (i < text.Length && text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                i++; // grab trailing newline on CRLF
            i++;
            Limit.X = Math.Max(Limit.X, Strwidth(line));
            FileLines.Add(line);
        }
        IsValid = true;
        Limit.Y = FileLines.Count;
    }

    public override void SetState(ushort aState, bool enable)
    {
        base.SetState(aState, enable);
        if (enable && (aState & sfExposed) != 0)
            SetLimit(Limit.X, Limit.Y);
    }

    public override bool Valid(ushort command) => IsValid;
}
