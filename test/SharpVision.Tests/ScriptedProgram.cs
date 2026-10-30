using System.Collections.Generic;
using System.Linq;

using SharpVision.App;
using SharpVision.Events;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Tests;

/// <summary>A program on an in-memory 80x25 screen whose events come from <see cref="Events"/>.</summary>
internal sealed class ScriptedProgram : TProgram
{
    public ScriptedProgram()
        : base(null, null, InitDeskTop)
    {
    }

    public Queue<TEvent> Events { get; } = new();

    public List<string> Snapshot { get; } = [];

    public override void GetEvent(ref TEvent ev)
    {
        if (Snapshot.Count == 0)
        {
            for (var y = 0; y < Size.Y; y++)
                Snapshot.Add(string.Concat(Buffer!.Skip(y * Size.X).Take(Size.X).Select(c => c.Character.GetText())));
        }
        if (Pending.What != evNothing)
        {
            ev = Pending;
            Pending.What = evNothing;
            return;
        }
        // Cancel once the script runs out, so a broken test fails instead of hanging.
        ev = Events.Count > 0 ? Events.Dequeue() : new TEvent { What = evCommand, Message = new MessageEvent { Command = cmCancel } };
    }
}
