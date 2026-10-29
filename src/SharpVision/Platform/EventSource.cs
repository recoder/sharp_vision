// Ported from include/tvision/internal/events.h and source/platform/events.cpp
// (EventSource, WakeUpEventSource, EventWaiter) and source/platform/sigwinch.cpp.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;

using SharpVision.Events;
using SharpVision.Platform.Interop;

using static SharpVision.Events.EventCodes;
using static SharpVision.Views.Commands;

namespace SharpVision.Platform;

/// <summary>Something that produces events and has a descriptor that becomes readable when it does.</summary>
internal abstract class EventSource(int handle)
{
    /// <summary>The descriptor to poll.</summary>
    public int Handle { get; } = handle;

    /// <summary>Whether events can be read without waiting on <see cref="Handle"/>.</summary>
    public virtual bool HasPendingEvents() => false;

    /// <summary>Reads an event. Returns false if the input did not produce one.</summary>
    public abstract bool GetEvent(ref TEvent ev);
}

/// <summary>
/// An event source that is signaled from other threads (or signal handlers) through a pipe,
/// optionally producing an event through a callback.
/// </summary>
internal class WakeUpEventSource : EventSource, IDisposable
{
    private readonly AnonymousPipeServerStream _readEnd;
    private readonly AnonymousPipeClientStream _writeEnd;
    private readonly Func<TEvent?>? _callback;
    private int _signaled;

    public WakeUpEventSource(Func<TEvent?>? callback)
        : this(new AnonymousPipeServerStream(PipeDirection.In, System.IO.HandleInheritability.None), callback)
    {
    }

    private WakeUpEventSource(AnonymousPipeServerStream readEnd, Func<TEvent?>? callback)
        : base((int)readEnd.SafePipeHandle.DangerousGetHandle())
    {
        _readEnd = readEnd;
        _writeEnd = new AnonymousPipeClientStream(PipeDirection.Out, readEnd.ClientSafePipeHandle);
        _callback = callback;
    }

    /// <summary>Wakes up the waiter. Safe to call from any thread.</summary>
    public void Signal()
    {
        if (Interlocked.Exchange(ref _signaled, 1) == 0)
        {
            _writeEnd.WriteByte(1);
            _writeEnd.Flush();
        }
    }

    public override bool GetEvent(ref TEvent ev)
    {
        if (Interlocked.Exchange(ref _signaled, 0) == 1)
        {
            Span<byte> b = stackalloc byte[64];
            LibC.read(Handle, ref MemoryMarshal.GetReference(b), (nuint)b.Length);
            if (_callback?.Invoke() is { } e)
            {
                ev = e;
                return true;
            }
        }
        return false;
    }

    public void Dispose()
    {
        _writeEnd.Dispose();
        _readEnd.Dispose();
    }
}

/// <summary>Reports terminal resizes (SIGWINCH) as <c>cmScreenChanged</c> commands.</summary>
internal sealed class SigwinchSource : WakeUpEventSource
{
    private readonly PosixSignalRegistration? _registration;

    public SigwinchSource()
        : base(static () =>
        {
            var ev = new TEvent { What = evCommand };
            ev.Message.Command = cmScreenChanged;
            return ev;
        })
    {
        if (!OperatingSystem.IsWindows())
        {
            _registration = PosixSignalRegistration.Create(PosixSignal.SIGWINCH, ctx =>
            {
                ctx.Cancel = true;
                Signal();
            });
        }
    }

    public new void Dispose()
    {
        _registration?.Dispose();
        base.Dispose();
    }
}

/// <summary>Waits on several event sources at once with <c>poll</c>.</summary>
[SuppressMessage("Design", "CA1001", Justification = "Owned by the Platform singleton, which lives as long as the process, as upstream.")]
internal sealed class EventWaiter
{
    private readonly List<EventSource> _sources = new();
    private readonly WakeUpEventSource _wakeUp = new(null);
    private bool[] _ready = [];
    private TEvent _readyEvent;
    private bool _readyEventPresent;

    public EventWaiter()
    {
        AddSource(_wakeUp);
    }

    public void AddSource(EventSource source)
    {
        _sources.Add(source);
        _ready = new bool[_sources.Count];
    }

    public void RemoveSource(EventSource source)
    {
        _sources.Remove(source);
        _ready = new bool[_sources.Count];
    }

    private void PollSources(int timeoutMs)
    {
        var hasEvents = false;
        for (var i = 0; i < _sources.Count; ++i)
            hasEvents |= _ready[i] = _sources[i].HasPendingEvents();
        if (hasEvents)
            return;

        Span<LibC.PollFd> fds = stackalloc LibC.PollFd[_sources.Count];
        for (var i = 0; i < _sources.Count; ++i)
            fds[i] = new LibC.PollFd { Fd = _sources[i].Handle, Events = LibC.POLLIN };
        if (LibC.poll(ref MemoryMarshal.GetReference(fds), (nuint)fds.Length, timeoutMs) > 0)
        {
            for (var i = 0; i < fds.Length; ++i)
                _ready[i] = (fds[i].REvents & (LibC.POLLIN | LibC.POLLHUP | LibC.POLLERR)) != 0;
        }
    }

    private bool HasReadyEvent()
    {
        if (!_readyEventPresent)
        {
            for (var i = 0; i < _sources.Count; ++i)
            {
                if (_ready[i])
                {
                    _ready[i] = false;
                    _readyEvent = default;
                    if (_sources[i].GetEvent(ref _readyEvent))
                    {
                        _readyEventPresent = true;
                        break;
                    }
                }
            }
        }
        return _readyEventPresent;
    }

    /// <summary>Returns an event if one is available without waiting.</summary>
    public bool GetEvent(ref TEvent ev)
    {
        if (!HasReadyEvent())
        {
            PollSources(0);
            HasReadyEvent();
        }
        if (_readyEventPresent)
        {
            ev = _readyEvent;
            _readyEventPresent = false;
            return true;
        }
        return false;
    }

    /// <summary>Waits until an event is available or <paramref name="ms"/> elapse (forever if negative).</summary>
    public void WaitForEvents(int ms)
    {
        var start = Stopwatch.GetTimestamp();
        while (!HasReadyEvent())
        {
            int timeout;
            if (ms < 0)
                timeout = -1;
            else
            {
                var elapsed = (int)Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                if (elapsed > ms)
                    break;
                timeout = ms - elapsed;
            }
            PollSources(timeout);
            if (ms == 0)
                break;
        }
    }

    /// <summary>Makes a pending <see cref="WaitForEvents"/> return. Safe to call from any thread.</summary>
    public void InterruptEventWait() => _wakeUp.Signal();
}
