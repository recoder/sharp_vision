// Ported in spirit from test/tvision/ttimerqu.test.cpp.

using System.Collections.Generic;
using SharpVision.Events;
using Xunit;

namespace SharpVision.Tests.Events;

public sealed class TTimerQueueTests
{
    private ulong _now = 1000;

    private List<object> Collect(TTimerQueue queue)
    {
        var expired = new List<object>();
        queue.CollectExpiredTimers(expired.Add);
        return expired;
    }

    [Fact]
    public void Should_ExpireOneShotTimerOnce()
    {
        var queue = new TTimerQueue(() => _now);
        var id = queue.SetTimer(100);

        Assert.Empty(Collect(queue));
        Assert.Equal(100, queue.TimeUntilNextTimeout());
        _now += 100;
        Assert.Equal([id], Collect(queue));
        Assert.Empty(Collect(queue));
        Assert.Equal(-1, queue.TimeUntilNextTimeout());
    }

    [Fact]
    public void Should_RescheduleperiodicTimer_SkippingMissedPeriods()
    {
        var queue = new TTimerQueue(() => _now);
        var id = queue.SetTimer(10, 10);

        _now += 35;
        Assert.Equal([id], Collect(queue));
        Assert.Equal(5, queue.TimeUntilNextTimeout());
        _now += 5;
        Assert.Equal([id], Collect(queue));
    }

    [Fact]
    public void Should_NotExpire_When_Killed()
    {
        var queue = new TTimerQueue(() => _now);
        var a = queue.SetTimer(10);
        var b = queue.SetTimer(10);
        queue.KillTimer(a);

        _now += 10;
        Assert.Equal([b], Collect(queue));
    }

    [Fact]
    public void Should_AllowTimerChangesDuringCollection()
    {
        var queue = new TTimerQueue(() => _now);
        var periodic = queue.SetTimer(0, 0);
        object? added = null;

        var expired = new List<object>();
        queue.CollectExpiredTimers(id =>
        {
            expired.Add(id);
            added ??= queue.SetTimer(0);
        });

        Assert.Equal([periodic, added!], expired);
    }
}
