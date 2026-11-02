using System;

using SharpVision.Objects;
using SharpVision.Views;

namespace SharpVision.Editors;

/// <summary>A scroller that text can be written to, like a stream buffer.</summary>
/// Ported from source/tvision/textview.cpp, include/tvision/textview.h (class TTextDevice).
/// <remarks>Upstream derives from <c>streambuf</c>; here text is written with <see cref="DoSputn"/> and <see cref="Overflow"/>.</remarks>
public abstract class TTextDevice : TScroller
{
    /// <summary>Creates the device with its optional scroll bars.</summary>
    protected TTextDevice(TRect bounds, TScrollBar? aHScrollBar, TScrollBar? aVScrollBar)
        : base(bounds, aHScrollBar, aVScrollBar)
    {
    }

    /// <summary>Writes <paramref name="s"/>; returns the number of characters written.</summary>
    public abstract int DoSputn(ReadOnlySpan<char> s);

    /// <summary>Writes the character <paramref name="c"/>, unless it is -1 (EOF); returns 1.</summary>
    public virtual int Overflow(int c = -1)
    {
        if (c != -1)
            DoSputn([(char)c]);
        return 1;
    }
}
