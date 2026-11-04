using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace SharpVision.Streams;

/// <summary>
/// Reads and writes upstream's persistent-stream wire format: little-endian integers,
/// length-prefixed strings and object pointers (a marker byte, then <c>'['</c>, the class name,
/// the object's data and <c>']'</c>).
/// </summary>
/// <remarks>
/// Only the subset that help files use: there is no <c>TStreamable</c> registry, so callers
/// read and write each object's data themselves, and indexed pointers (references to objects
/// already in the stream) are not supported. Strings are UTF-8.
/// </remarks>
/// Ported from source/tvision/tobjstrm.cpp, include/tvision/tobjstrm.h (classes pstream, ipstream, opstream, iopstream; subset).
public sealed class PStream(Stream stream)
{
    /// <summary>A null pointer.</summary>
    public const byte ptNull = 0;
    /// <summary>A reference to an object already in the stream (not supported here).</summary>
    public const byte ptIndexed = 1;
    /// <summary>An object written in full.</summary>
    public const byte ptObject = 2;
    /// <summary>The length byte of a null string.</summary>
    public const byte nullStringLen = 255;

    /// <summary>The underlying stream.</summary>
    public Stream BaseStream { get; } = stream;

    /// <summary>The read position.</summary>
    public long Tellg() => BaseStream.Position;

    /// <summary>Moves the read position to <paramref name="pos"/>.</summary>
    public void Seekg(long pos) => BaseStream.Seek(pos, SeekOrigin.Begin);

    /// <summary>Moves the read position by <paramref name="off"/> from <paramref name="dir"/>.</summary>
    public void Seekg(long off, SeekOrigin dir) => BaseStream.Seek(off, dir);

    /// <summary>The write position (the same as the read position).</summary>
    public long Tellp() => BaseStream.Position;

    /// <summary>Moves the write position to <paramref name="pos"/>.</summary>
    public void Seekp(long pos) => BaseStream.Seek(pos, SeekOrigin.Begin);

    /// <summary>Moves the write position by <paramref name="off"/> from <paramref name="dir"/>.</summary>
    public void Seekp(long off, SeekOrigin dir) => BaseStream.Seek(off, dir);

    /// <summary>Reads a byte; 0xFF at the end of the stream, like upstream's <c>sbumpc</c> cast to <c>uchar</c>.</summary>
    public byte ReadByte() => (byte)BaseStream.ReadByte();

    /// <summary>Reads <paramref name="count"/> bytes; missing bytes at the end of the stream are zero.</summary>
    public byte[] ReadBytes(int count)
    {
        var buf = new byte[count];
        BaseStream.ReadAtLeast(buf, count, throwOnEndOfStream: false);
        return buf;
    }

    /// <summary>Reads a 16-bit word.</summary>
    public ushort ReadWord() => BinaryPrimitives.ReadUInt16LittleEndian(ReadBytes(2));

    /// <summary>Reads a 32-bit integer (upstream's <c>int</c> and <c>int32_t</c>).</summary>
    public int ReadInt() => BinaryPrimitives.ReadInt32LittleEndian(ReadBytes(4));

    /// <summary>Reads a length-prefixed string; null for <see cref="nullStringLen"/>.</summary>
    public string? ReadString()
    {
        var len = ReadByte();
        return len == nullStringLen ? null : Encoding.UTF8.GetString(ReadBytes(len));
    }

    /// <summary>
    /// Reads a pointer's marker and, for an object, its prefix. Returns the class name, or null
    /// for a null pointer; the caller then reads the data and calls <see cref="ReadSuffix"/>.
    /// </summary>
    public string? ReadPointerPrefix()
    {
        switch (ReadByte())
        {
            case ptNull:
                return null;
            case ptObject:
                if (ReadByte() != '[')
                    throw new InvalidDataException("pstream error: missing object prefix");
                return ReadString();
            case ptIndexed:
                // shortcut: no object registry, so back-references fail; port TPReadObjects when a stream uses them.
                throw new NotSupportedException("pstream error: indexed pointers are not supported");
            default:
                throw new InvalidDataException("pstream error: invalid type encountered");
        }
    }

    /// <summary>Reads the end of an object.</summary>
    public void ReadSuffix()
    {
        if (ReadByte() != ']')
            throw new InvalidDataException("pstream error: missing object suffix");
    }

    /// <summary>Writes a byte.</summary>
    public void WriteByte(byte value) => BaseStream.WriteByte(value);

    /// <summary>Writes bytes.</summary>
    public void WriteBytes(ReadOnlySpan<byte> data) => BaseStream.Write(data);

    /// <summary>Writes a 16-bit word.</summary>
    public void WriteWord(ushort value)
    {
        Span<byte> b = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16LittleEndian(b, value);
        WriteBytes(b);
    }

    /// <summary>Writes a 32-bit integer.</summary>
    public void WriteInt(int value)
    {
        Span<byte> b = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(b, value);
        WriteBytes(b);
    }

    /// <summary>Writes a length-prefixed string, truncated to 254 bytes; null writes <see cref="nullStringLen"/>.</summary>
    public void WriteString(string? str)
    {
        if (str is null)
        {
            WriteByte(nullStringLen);
            return;
        }
        var bytes = Encoding.UTF8.GetBytes(str);
        var len = Math.Min(bytes.Length, nullStringLen - 1);
        WriteByte((byte)len);
        WriteBytes(bytes.AsSpan(0, len));
    }

    /// <summary>
    /// Writes a pointer's marker and, when <paramref name="name"/> is not null, the object
    /// prefix; the caller then writes the data and calls <see cref="WriteSuffix"/>.
    /// </summary>
    public void WritePointerPrefix(string? name)
    {
        if (name is null)
        {
            WriteByte(ptNull);
            return;
        }
        WriteByte(ptObject);
        WriteByte((byte)'[');
        WriteString(name);
    }

    /// <summary>Writes the end of an object.</summary>
    public void WriteSuffix() => WriteByte((byte)']');

    /// <summary>Closes the underlying stream.</summary>
    public void Close() => BaseStream.Dispose();
}
