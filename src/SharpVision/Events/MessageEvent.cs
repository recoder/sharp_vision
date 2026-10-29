// Ported from include/tvision/system.h (struct MessageEvent).

namespace SharpVision.Events;

/// <summary>The data of a command, broadcast or user-defined message event.</summary>
/// <remarks>
/// UPSTREAM: upstream's <c>infoPtr</c> shares storage with the integer fields. Here
/// <see cref="InfoPtr"/> is separate from <see cref="InfoLong"/>, and the smaller integer
/// fields read the low bits of <see cref="InfoLong"/>. When upstream passes an integer
/// through <c>infoPtr</c> (e.g. <c>cmSelectWindowNum</c>), port it through
/// <see cref="InfoLong"/> instead.
/// </remarks>
public struct MessageEvent
{
    /// <summary>Command (<c>cmXxx</c>).</summary>
    public ushort Command;

    /// <summary>An object accompanying the message (upstream's <c>void *infoPtr</c>).</summary>
    public object? InfoPtr;

    /// <summary>An integer accompanying the message.</summary>
    public int InfoLong;

    /// <summary>The low 16 bits of <see cref="InfoLong"/>, unsigned.</summary>
    public ushort InfoWord
    {
        readonly get => (ushort)InfoLong;
        set => InfoLong = value;
    }

    /// <summary>The low 16 bits of <see cref="InfoLong"/>, signed.</summary>
    public short InfoInt
    {
        readonly get => (short)InfoLong;
        set => InfoLong = value;
    }

    /// <summary>The low 8 bits of <see cref="InfoLong"/>.</summary>
    public byte InfoByte
    {
        readonly get => (byte)InfoLong;
        set => InfoLong = value;
    }
}
