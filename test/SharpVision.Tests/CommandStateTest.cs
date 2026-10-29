using System;
using SharpVision.Events;
using SharpVision.Views;
using Xunit;

namespace SharpVision.Tests;

/// <summary>
/// Base for tests whose views change the global command set (selecting a window enables
/// <c>cmClose</c>, <c>cmZoom</c>, ...) or <see cref="TView.ShowMarkers"/>: restores both afterwards.
/// </summary>
[Collection(GlobalStateCollection.Name)]
public abstract class CommandStateTest : IDisposable
{
    private readonly TCommandSet _savedCommands;
    private readonly bool _savedShowMarkers;

    protected CommandStateTest()
    {
        TView.GetCommands(out _savedCommands);
        _savedShowMarkers = TView.ShowMarkers;
    }

    public void Dispose()
    {
        TView.SetCommands(_savedCommands);
        TView.CommandSetChanged = false;
        TView.ShowMarkers = _savedShowMarkers;
        GC.SuppressFinalize(this);
    }
}
