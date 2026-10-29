// Ported from include/tvision/system.h and source/tvision/ttimerqu.cpp (class TTimerQueue).

using System;

using SharpVision.Platform;

namespace SharpVision.Events;

/// <summary>A list of one-shot and periodic timers, checked by polling.</summary>
public sealed class TTimerQueue
{
    private sealed class TTimer
    {
        public ulong ExpiresAt;
        public int Period;
        public TTimer? Next;
        public object? CollectId;
    }

    private readonly Func<ulong> _getTimeMs;
    private TTimer? _first;

    /// <summary>Creates a queue using the system clock.</summary>
    public TTimerQueue()
        : this(THardwareInfo.GetTickCountMs)
    {
    }

    /// <summary>Creates a queue using <paramref name="getTimeMs"/> as the clock (in milliseconds).</summary>
    public TTimerQueue(Func<ulong> getTimeMs)
    {
        _getTimeMs = getTimeMs;
    }

    /// <summary>
    /// Starts a timer that expires after <paramref name="timeoutMs"/> and then, if
    /// <paramref name="periodMs"/> is not negative, every <paramref name="periodMs"/>.
    /// Returns its id.
    /// </summary>
    public object SetTimer(uint timeoutMs, int periodMs = -1)
    {
        var timer = new TTimer { ExpiresAt = _getTimeMs() + timeoutMs, Period = periodMs };
        if (_first is null)
            _first = timer;
        else
        {
            var p = _first;
            while (p.Next is not null)
                p = p.Next;
            p.Next = timer;
        }
        return timer;
    }

    /// <summary>Stops the timer <paramref name="id"/>.</summary>
    public void KillTimer(object? id)
    {
        TTimer? prev = null;
        for (var p = _first; p is not null; prev = p, p = p.Next)
        {
            if (p == id)
            {
                if (prev is null)
                    _first = p.Next;
                else
                    prev.Next = p.Next;
                break;
            }
        }
    }

    // Pre: expiresAt <= now && period > 0.
    private static ulong CalcNextExpiresAt(ulong expiresAt, ulong now, int period) =>
        (1 + (now - expiresAt + (ulong)period) / (ulong)period) * (ulong)period + expiresAt - (ulong)period;

    /// <summary>
    /// Calls <paramref name="func"/> with the id of every expired timer. One-shot timers are
    /// removed first; periodic ones are rescheduled. <paramref name="func"/> may start and
    /// stop timers.
    /// </summary>
    public void CollectExpiredTimers(Action<object> func)
    {
        if (_first is null)
            return;
        // The list may change while expired timers are processed, so it is scanned from
        // the beginning every time. Processed timers are marked with an object identifying
        // the current invocation.
        var collectId = new object();
        var now = _getTimeMs();
        while (true)
        {
            TTimer? prev = null;
            var p = _first;
            while (p is not null && (p.CollectId is not null || now < p.ExpiresAt))
            {
                prev = p;
                p = p.Next;
            }
            if (p is null)
                break;
            if (p.Period >= 0)
            {
                p.CollectId = collectId;
                if (p.Period > 0)
                    p.ExpiresAt = CalcNextExpiresAt(p.ExpiresAt, now, p.Period);
            }
            else if (prev is null) // One-shot timer.
                _first = p.Next;
            else
                prev.Next = p.Next;
            func(p); // May mutate the timer list.
        }
        for (var timer = _first; timer is not null; timer = timer.Next)
        {
            if (timer.CollectId == collectId)
                timer.CollectId = null;
        }
    }

    /// <summary>Milliseconds until the next timer expires, 0 if one already has, or -1 if there are none.</summary>
    public int TimeUntilNextTimeout()
    {
        if (_first is null)
            return -1;
        var now = _getTimeMs();
        const uint maxValue = uint.MaxValue >> 1;
        var timeout = (int)maxValue;
        for (var timer = _first; timer is not null; timer = timer.Next)
        {
            if (timer.ExpiresAt <= now)
                return 0;
            var aTimeout = (uint)Math.Min(timer.ExpiresAt - now, maxValue);
            timeout = Math.Min(timeout, (int)aTimeout);
        }
        return timeout;
    }
}
