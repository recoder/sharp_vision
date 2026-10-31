using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpVision.Dialogs;

/// <summary>
/// The global input line history: strings tagged with a history id, oldest first, in a block of
/// <see cref="HistorySize"/> bytes. Adding a string removes its earlier copy; when the block is full,
/// the oldest records are dropped.
/// </summary>
/// Ported from source/tvision/histlist.cpp.
/// <remarks>
/// Records keep upstream's byte accounting: each takes its UTF-8 length plus 3 bytes, and the block
/// starts with an empty record (id 0).
/// </remarks>
public static class HistList
{
    /// <summary>The size of the history block in bytes.</summary>
    public static ushort HistorySize = 1024;

    private static readonly List<(byte Id, string Str, int Len)> _records = [(0, "", 3)];

    // UPSTREAM: lookups always skip the first record of the block. It starts as the empty record,
    // but once that is dropped to make room, the oldest real string becomes invisible.
    private static IEnumerable<string> Strings(byte id) =>
        _records.Skip(1).Where(r => r.Id == id).Select(r => r.Str);

    /// <summary>The number of strings recorded for <paramref name="id"/>.</summary>
    public static ushort HistoryCount(byte id) => (ushort)Strings(id).Count();

    /// <summary>Records <paramref name="str"/> for <paramref name="id"/> as the newest string. Empty strings are ignored.</summary>
    public static void HistoryAdd(byte id, string str)
    {
        if (str.Length == 0)
            return;
        for (var i = 1; i < _records.Count; i++)
        {
            if (_records[i].Id == id && _records[i].Str == str)
                _records.RemoveAt(i--);
        }
        InsertString(id, str);
    }

    /// <summary>The <paramref name="index"/>th string (oldest first) for <paramref name="id"/>, or null.</summary>
    public static string? HistoryStr(byte id, int index) => Strings(id).ElementAtOrDefault(index);

    /// <summary>Removes all strings.</summary>
    public static void ClearHistory()
    {
        _records.Clear();
        _records.Add((0, "", 3));
    }

    /// <summary>Prepares an empty history. Called by <c>TApplication</c>.</summary>
    public static void InitHistory() => ClearHistory();

    /// <summary>Releases the history. Called by <c>TApplication</c>.</summary>
    public static void DoneHistory() => ClearHistory();

    private static void InsertString(byte id, string str)
    {
        var len = Encoding.UTF8.GetByteCount(str) + 3;
        // UPSTREAM: a string longer than the whole block loops forever (and the uchar length wraps
        // past 252 bytes); here it empties the block and is stored anyway.
        while (len > HistorySize - _records.Sum(r => r.Len) && _records.Count > 0)
            _records.RemoveAt(0);
        _records.Add((id, str, len));
    }
}
