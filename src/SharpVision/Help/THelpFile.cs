using System.IO;

using SharpVision.Objects;
using SharpVision.Streams;

namespace SharpVision.Help;

/// <summary>A help file: a header, topics and an index of topic positions by context number.</summary>
/// Ported from source/tvision/helpbase.cpp, source/tvision/tvtext2.cpp, include/tvision/helpbase.h (class THelpFile).
public class THelpFile : TObject
{
    /// <summary>The text of the topic shown for contexts without help.</summary>
    public static string InvalidContext = "\n No help available in this context.";

    /// <summary>The file's stream.</summary>
    public PStream Stream;

    /// <summary>Whether topics were added, so <see cref="ShutDown"/> writes the index and header.</summary>
    public bool Modified;

    /// <summary>The topic positions.</summary>
    public THelpIndex Index;

    /// <summary>Where the index is (and where the next topic goes).</summary>
    public int IndexPos;

    /// <summary>Reads the header and the index of <paramref name="s"/>, or starts a new file if it has no valid header.</summary>
    public THelpFile(PStream s)
    {
        var magic = 0;
        s.Seekg(0, SeekOrigin.End);
        var size = (int)s.Tellg();
        s.Seekg(0);
        if (size > sizeof(int))
            magic = s.ReadInt();
        if (magic == HelpBase.magicHeader && size >= 12)
        {
            s.Seekg(8);
            IndexPos = s.ReadInt();
            s.Seekg(IndexPos);
            // UPSTREAM: a null index pointer is not checked.
            Index = ReadIndex(s)!;
            Modified = false;
        }
        else
        {
            IndexPos = 12;
            Index = new THelpIndex();
            Modified = true;
        }
        Stream = s;
    }

    private static THelpIndex? ReadIndex(PStream s)
    {
        if (s.ReadPointerPrefix() is null)
            return null;
        var index = new THelpIndex();
        index.Read(s);
        s.ReadSuffix();
        return index;
    }

    /// <summary>Writes the index and the header if the file was modified, then closes the stream.</summary>
    public override void ShutDown()
    {
        if (Modified)
        {
            EnsureStreamSize(Stream, IndexPos);
            Stream.Seekp(IndexPos);
            Stream.WritePointerPrefix(THelpIndex.Name);
            Index.Write(Stream);
            Stream.WriteSuffix();
            Stream.Seekp(0, SeekOrigin.End);
            var size = (int)Stream.Tellp() - 8;
            Stream.Seekp(0);
            Stream.WriteInt(HelpBase.magicHeader);
            Stream.WriteInt(size);
            Stream.WriteInt(IndexPos);
        }
        Stream.Close();
    }

    /// <summary>Reads the topic of context <paramref name="i"/>, or <see cref="InvalidTopic"/> if there is none.</summary>
    public THelpTopic GetTopic(int i)
    {
        var pos = Index.Position(i);
        if (pos > 0)
        {
            Stream.Seekg(pos);
            // UPSTREAM: a null topic pointer is returned as is.
            if (Stream.ReadPointerPrefix() is null)
                return null!;
            var topic = new THelpTopic();
            topic.Read(Stream);
            Stream.ReadSuffix();
            return topic;
        }
        return InvalidTopic();
    }

    /// <summary>A topic holding <see cref="InvalidContext"/>.</summary>
    public THelpTopic InvalidTopic()
    {
        var topic = new THelpTopic();
        topic.AddParagraph(new TParagraph { Text = InvalidContext, Wrap = false });
        return topic;
    }

    /// <summary>Records that context <paramref name="i"/>'s topic is the next one written.</summary>
    public void RecordPositionInIndex(int i)
    {
        Index.Add(i, IndexPos);
        Modified = true;
    }

    /// <summary>Writes <paramref name="topic"/> at the end of the topics.</summary>
    public void PutTopic(THelpTopic topic)
    {
        EnsureStreamSize(Stream, IndexPos);
        Stream.Seekp(IndexPos);
        Stream.WritePointerPrefix(THelpTopic.Name);
        topic.Write(Stream);
        Stream.WriteSuffix();
        IndexPos = (int)Stream.Tellp();
        Modified = true;
    }

    // Extends the stream with zero bytes up to desiredSize.
    private static void EnsureStreamSize(PStream stream, int desiredSize)
    {
        stream.Seekg(0, SeekOrigin.End);
        var currentSize = (int)stream.Tellg();
        if (currentSize < desiredSize)
        {
            stream.Seekp(0, SeekOrigin.End);
            for (var i = currentSize; i < desiredSize; ++i)
                stream.WriteByte(0);
        }
    }
}
