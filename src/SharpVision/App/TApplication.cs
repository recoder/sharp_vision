// Ported from include/tvision/app.h (TAppInit, TApplication) and source/tvision/tapplica.cpp.

using System;
using System.Diagnostics;

using SharpVision.Events;
using SharpVision.Menus;
using SharpVision.Objects;
using SharpVision.Platform;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.App;

/// <summary>
/// The base class of applications: a <see cref="TProgram"/> that also sets up the console
/// subsystems (screen, mouse, event queue) and handles cascading, tiling and the shell.
/// </summary>
/// <remarks>
/// Call <see cref="TObject.Destroy"/> on the application when it ends, to restore the console.
/// </remarks>
public class TApplication : TProgram
{
    private static bool _subsystemsReady;

    /// <summary>Creates the application with the default status line, menu bar and desktop.</summary>
    protected TApplication()
        : this(InitStatusLine, InitMenuBar, InitDeskTop)
    {
    }

    /// <summary>Creates the application, building its parts with the given factories (null to omit a part).</summary>
    protected TApplication(
        Func<TRect, TStatusLine?>? createStatusLine,
        Func<TRect, TMenuBar?>? createMenuBar,
        Func<TRect, TDeskTop?>? createDeskTop)
        : base(InitSubsystems(createStatusLine), createMenuBar, createDeskTop)
    {
        // TBD: initHistory() once THistory is ported.
    }

    // Upstream's TAppInit: the subsystems must be ready before TProgram's constructor runs,
    // so this is called while evaluating the base constructor's arguments.
    private static T InitSubsystems<T>(T value)
    {
        if (!_subsystemsReady)
        {
            TMouse.Resume();
            TScreen.Initialize();
            TEventQueue.Initialize();
            _subsystemsReady = true;
        }
        return value;
    }

    /// <summary>Shuts down the application and restores the console.</summary>
    public override void ShutDown()
    {
        base.ShutDown();
        if (_subsystemsReady)
        {
            TEventQueue.Done();
            TScreen.Done();
            TMouse.Suspend();
            _subsystemsReady = false;
        }
    }

    /// <summary>Gives the console back to the system.</summary>
    public override void Suspend()
    {
        TEventQueue.Suspend();
        TScreen.Suspend();
    }

    /// <summary>Takes the console back.</summary>
    public override void Resume()
    {
        TScreen.Resume();
        TEventQueue.Resume();
    }

    /// <summary>Cascades the windows on the desktop.</summary>
    public void Cascade() => DeskTop?.Cascade(GetTileRect());

    /// <summary>Runs a command shell, then returns to the application.</summary>
    /// <remarks>
    /// UPSTREAM: on Unix, upstream suspends the process with SIGTSTP (the user returns with
    /// 'fg'). This port runs <c>$SHELL</c> (or <c>/bin/sh</c>) and waits for it to exit.
    /// </remarks>
    public void DosShell()
    {
        Suspend();
        WriteShellMsg();
        var shell = OperatingSystem.IsWindows()
            ? Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe"
            : Environment.GetEnvironmentVariable("SHELL") ?? "/bin/sh";
        try
        {
            using var process = Process.Start(new ProcessStartInfo(shell) { UseShellExecute = false });
            process?.WaitForExit();
        }
        catch (System.ComponentModel.Win32Exception)
        {
        }
        Resume();
        Redraw();
    }

    /// <summary>The area <see cref="Cascade"/> and <see cref="Tile"/> arrange windows in: the whole desktop.</summary>
    public virtual TRect GetTileRect() => DeskTop!.GetExtent();

    /// <summary>Handles <c>cmDosShell</c>, <c>cmCascade</c> and <c>cmTile</c>.</summary>
    public override void HandleEvent(ref TEvent ev)
    {
        base.HandleEvent(ref ev);
        if (ev.What == evCommand)
        {
            switch (ev.Message.Command)
            {
                case cmDosShell:
                    DosShell();
                    break;
                case cmCascade:
                    Cascade();
                    break;
                case cmTile:
                    Tile();
                    break;
                default:
                    return;
            }
            ClearEvent(ref ev);
        }
    }

    /// <summary>Tiles the windows on the desktop.</summary>
    public void Tile() => DeskTop?.Tile(GetTileRect());

    /// <summary>Prints the message shown before running the shell.</summary>
    public virtual void WriteShellMsg() => Console.WriteLine("Type EXIT to return...");
}
