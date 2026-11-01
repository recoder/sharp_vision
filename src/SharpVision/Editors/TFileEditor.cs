using System;
using System.IO;
using System.Text;

using SharpVision.Events;
using SharpVision.Objects;
using SharpVision.Views;

using static SharpVision.Editors.EditorCommands;
using static SharpVision.Events.EventCodes;
using static SharpVision.StdDlg.Dir;
using static SharpVision.Views.Commands;

namespace SharpVision.Editors;

/// <summary>
/// An editor bound to a file: it loads the file, saves it (optionally keeping a <c>.bak</c> backup), grows
/// its buffer in 4K steps, and asks to save changes before closing.
/// </summary>
/// Ported from source/tvision/tfiledtr.cpp, source/tvision/tvtext1.cpp (backupExt),
/// include/tvision/editors.h (class TFileEditor).
/// <remarks>
/// Files are read as UTF-8 (or as the encoding their byte order mark names) and written as UTF-8 without a
/// byte order mark, since the buffer holds UTF-16 text. Invalid UTF-8 is read as U+FFFD.
/// </remarks>
public class TFileEditor : TEditor
{
    /// <summary>The extension of backup files.</summary>
    public static string BackupExt = ".bak";

    private static readonly UTF8Encoding _utf8 = new(false);

    /// <summary>The expanded file name, or empty for an untitled editor.</summary>
    public string FileName;

    /// <summary>
    /// Creates an editor for the file <paramref name="aFileName"/> and loads it; an empty name makes an
    /// untitled editor.
    /// </summary>
    public TFileEditor(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar, TIndicator? aIndicator, string aFileName)
        : base(bounds, aHScrollBar, aVScrollBar, aIndicator, 0)
    {
        // Upstream frees and reallocates the buffer here to use malloc; the base buffer is already right.
        if (aFileName.Length == 0)
            FileName = "";
        else
        {
            FileName = FExpand(aFileName);
            if (IsValid)
                IsValid = LoadFile();
        }
    }

    /// <summary>Handles <c>cmSave</c> and <c>cmSaveAs</c>.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What != evCommand)
            return;
        switch (ev.Message.Command)
        {
            case cmSave:
                Save();
                break;
            case cmSaveAs:
                SaveAs();
                break;
            default:
                return;
        }
        ClearEvent(ref ev);
    }

    /// <summary>Loads <see cref="FileName"/>; a file that cannot be opened gives an empty buffer.</summary>
    public bool LoadFile()
    {
        FileStream f;
        try
        {
            f = File.OpenRead(FileName);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            SetBufLen(0);
            return true;
        }
        string text;
        try
        {
            using var r = new StreamReader(f, _utf8);
            text = r.ReadToEnd();
        }
        catch (IOException)
        {
            EditorDialog(edReadError, FileName);
            return false;
        }
        if (!SetBufSize(text.Length))
        {
            EditorDialog(edOutOfMemory);
            return false;
        }
        text.CopyTo(Buffer.AsSpan(BufSize - text.Length));
        SetBufLen(text.Length);
        return true;
    }

    /// <summary>Saves to <see cref="FileName"/>, or asks for a name if untitled.</summary>
    public bool Save() => FileName.Length == 0 ? SaveAs() : SaveFile();

    /// <summary>
    /// Asks for a name through <c>edSaveAs</c>, whose argument is a one-slot data record holding the name,
    /// and saves to it.
    /// </summary>
    public bool SaveAs()
    {
        var res = false;
        var rec = new object?[] { FileName };
        if (EditorDialog(edSaveAs, (object)rec) != cmCancel)
        {
            FileName = FExpand(rec[0] as string ?? "");
            Message(Owner, evBroadcast, cmUpdateTitle, null);
            res = SaveFile();
            if (IsClipboard())
                FileName = "";
        }
        return res;
    }

    /// <summary>Writes the buffer to <see cref="FileName"/>, first renaming the old file to a backup if <c>efBackupFiles</c> is set.</summary>
    public bool SaveFile()
    {
        if ((EditorFlags & efBackupFiles) != 0)
        {
            FnSplit(FileName, out var drive, out var dir, out var file, out _);
            var backupName = FnMerge(drive, dir, file, BackupExt);
            // Like unlink() and rename(), failures are ignored.
            try
            {
                File.Delete(backupName);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
            try
            {
                File.Move(FileName, backupName);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }

        FileStream f;
        try
        {
            f = File.Create(FileName);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            EditorDialog(edCreateError, FileName);
            return false;
        }
        try
        {
            using var w = new StreamWriter(f, _utf8);
            w.Write(Buffer, 0, CurPtr);
            w.Write(Buffer, CurPtr + GapLen, BufLen - CurPtr);
        }
        catch (IOException)
        {
            EditorDialog(edWriteError, FileName);
            return false;
        }
        Modified = false;
        Update(ufUpdate);
        return true;
    }

    /// <summary>Resizes the buffer to <paramref name="newSize"/> rounded up to 4K, keeping the text on both sides of the gap.</summary>
    public override bool SetBufSize(int newSize)
    {
        if (newSize == 0)
            newSize = 0x1000;
        else if (newSize > int.MaxValue - 0x0FFF)
            newSize = int.MaxValue - 0x1F;
        else
            newSize = (newSize + 0x0FFF) & -0x1000;
        if (newSize != BufSize)
        {
            // Allocation failure throws in .NET, so upstream's failure branch is gone.
            var temp = Buffer;
            Buffer = new char[newSize];
            var n = BufLen - CurPtr + DelCount;
            Array.Copy(temp, Buffer, Math.Min(newSize, BufSize));
            Array.Copy(temp, BufSize - n, Buffer, newSize - n, n);
            BufSize = newSize;
            GapLen = BufSize - BufLen;
        }
        return true;
    }

    /// <inheritdoc />
    public override void ShutDown()
    {
        SetCmdState(cmSave, false);
        SetCmdState(cmSaveAs, false);
        base.ShutDown();
    }

    /// <summary>Also enables <c>cmSave</c> and <c>cmSaveAs</c>.</summary>
    public override void UpdateCommands()
    {
        base.UpdateCommands();
        SetCmdState(cmSave, true);
        SetCmdState(cmSaveAs, true);
    }

    /// <summary>
    /// <c>cmValid</c> checks the buffer; other commands ask through <c>edSaveModify</c> or <c>edSaveUntitled</c>
    /// whether to save changes.
    /// </summary>
    public override bool Valid(ushort command)
    {
        if (command == cmValid)
            return IsValid;
        if (Modified)
        {
            var d = FileName.Length == 0 ? edSaveUntitled : edSaveModify;
            switch (EditorDialog(d, FileName))
            {
                case cmYes:
                    return Save();
                case cmNo:
                    Modified = false;
                    return true;
                case cmCancel:
                    return false;
            }
        }
        return true;
    }
}
