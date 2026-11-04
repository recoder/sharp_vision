using System;

using SharpVision.Streams;

namespace SharpVision.Help;

/// <summary>Maps help context numbers to topic positions in a help file.</summary>
/// Ported from source/tvision/helpbase.cpp, include/tvision/helpbase.h (class THelpIndex).
public class THelpIndex
{
    /// <summary>The class name in streams.</summary>
    public const string Name = "THelpIndex";

    /// <summary>The number of entries.</summary>
    public ushort Size;

    /// <summary>The topic position of each context, or -1.</summary>
    public int[]? Index;

    /// <summary>The position of context <paramref name="i"/>, or -1.</summary>
    public int Position(int i) => i < Size ? Index![i] : -1;

    /// <summary>Sets the position of context <paramref name="i"/>, growing the index in steps of 10 filled with -1.</summary>
    public void Add(int i, int val)
    {
        const int delta = 10;
        if (i >= Size)
        {
            var newSize = (i + delta) / delta * delta;
            var p = new int[newSize];
            Index?.AsSpan(0, Size).CopyTo(p);
            p.AsSpan(Size).Fill(-1);
            Index = p;
            Size = (ushort)newSize;
        }
        Index![i] = val;
    }

    /// <summary>Reads the object's data.</summary>
    public void Read(PStream s)
    {
        Size = s.ReadWord();
        if (Size == 0)
            Index = null;
        else
        {
            Index = new int[Size];
            for (var i = 0; i < Size; ++i)
                Index[i] = s.ReadInt();
        }
    }

    /// <summary>Writes the object's data.</summary>
    public void Write(PStream s)
    {
        s.WriteWord(Size);
        for (var i = 0; i < Size; ++i)
            s.WriteInt(Index![i]);
    }
}
