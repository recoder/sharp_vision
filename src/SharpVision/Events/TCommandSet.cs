// Ported from include/tvision/views.h and source/tvision/tcmdset.cpp (class TCommandSet).

using System;
using System.Runtime.CompilerServices;

namespace SharpVision.Events;

/// <summary>
/// A set of commands 0-255, used to track which commands are enabled. Commands outside
/// that range are never in the set; adding or removing them has no effect.
/// </summary>
/// <remarks>A value type: assigning a set copies it, as in upstream.</remarks>
public struct TCommandSet : IEquatable<TCommandSet>
{
    [InlineArray(32)]
    private struct Bits
    {
        private byte _element0;
    }

    private Bits _cmds;

    private static uint Loc(int cmd) => (uint)cmd / 8;

    private static int Mask(int cmd) => 1 << (cmd & 0x07);

    /// <summary>Whether <paramref name="cmd"/> is in the set.</summary>
    public readonly bool Has(int cmd)
    {
        if (Loc(cmd) < 32)
            return (_cmds[(int)Loc(cmd)] & Mask(cmd)) != 0;
        return false;
    }

    /// <summary>Removes <paramref name="cmd"/> from the set.</summary>
    public void DisableCmd(int cmd)
    {
        if (Loc(cmd) < 32)
            _cmds[(int)Loc(cmd)] &= (byte)~Mask(cmd);
    }

    /// <summary>Adds <paramref name="cmd"/> to the set.</summary>
    public void EnableCmd(int cmd)
    {
        if (Loc(cmd) < 32)
            _cmds[(int)Loc(cmd)] |= (byte)Mask(cmd);
    }

    /// <summary>Removes every command in <paramref name="tc"/> from the set.</summary>
    public void DisableCmd(in TCommandSet tc)
    {
        for (var i = 0; i < 32; i++)
            _cmds[i] &= (byte)~tc._cmds[i];
    }

    /// <summary>Adds every command in <paramref name="tc"/> to the set.</summary>
    public void EnableCmd(in TCommandSet tc)
    {
        for (var i = 0; i < 32; i++)
            _cmds[i] |= tc._cmds[i];
    }

    /// <summary>Whether the set is empty.</summary>
    public readonly bool IsEmpty()
    {
        for (var i = 0; i < 32; i++)
        {
            if (_cmds[i] != 0)
                return false;
        }
        return true;
    }

    /// <summary>Returns a copy with <paramref name="cmd"/> added. Enables <c>set += cmd</c>.</summary>
    public static TCommandSet operator +(TCommandSet tc, int cmd)
    {
        tc.EnableCmd(cmd);
        return tc;
    }

    /// <summary>Returns a copy with <paramref name="cmd"/> removed. Enables <c>set -= cmd</c>.</summary>
    public static TCommandSet operator -(TCommandSet tc, int cmd)
    {
        tc.DisableCmd(cmd);
        return tc;
    }

    /// <summary>Returns the union. Enables <c>set += other</c>.</summary>
    public static TCommandSet operator +(TCommandSet tc1, TCommandSet tc2)
    {
        tc1.EnableCmd(tc2);
        return tc1;
    }

    /// <summary>Returns <paramref name="tc1"/> without the commands in <paramref name="tc2"/>. Enables <c>set -= other</c>.</summary>
    public static TCommandSet operator -(TCommandSet tc1, TCommandSet tc2)
    {
        tc1.DisableCmd(tc2);
        return tc1;
    }

    /// <summary>Returns the intersection.</summary>
    public static TCommandSet operator &(TCommandSet tc1, TCommandSet tc2)
    {
        for (var i = 0; i < 32; i++)
            tc1._cmds[i] &= tc2._cmds[i];
        return tc1;
    }

    /// <summary>Returns the union.</summary>
    public static TCommandSet operator |(TCommandSet tc1, TCommandSet tc2)
    {
        tc1.EnableCmd(tc2);
        return tc1;
    }

    /// <inheritdoc />
    public static bool operator ==(TCommandSet tc1, TCommandSet tc2) => tc1.Equals(tc2);

    /// <inheritdoc />
    public static bool operator !=(TCommandSet tc1, TCommandSet tc2) => !tc1.Equals(tc2);

    /// <inheritdoc />
    public readonly bool Equals(TCommandSet other) =>
        ((ReadOnlySpan<byte>)_cmds).SequenceEqual(other._cmds);

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is TCommandSet other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode()
    {
        var hash = new HashCode();
        hash.AddBytes(_cmds);
        return hash.ToHashCode();
    }
}
