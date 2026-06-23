// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FakeTimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

/// <summary>
/// Provides a deterministic, manually advanced <see cref="TimeProvider" /> for time-dependent tests. Time only
/// progresses when <see cref="Advance(TimeSpan)" /> is called, at which point any due single-shot timers fire.
/// </summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private readonly List<FakeTimer> _timers = new();
    private long _ticks;

    /// <inheritdoc />
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    /// <inheritdoc />
    public override long GetTimestamp()
    {
        lock (_gate)
        {
            return _ticks;
        }
    }

    /// <summary>
    /// Advances the simulated clock by the specified amount and fires any timers that become due.
    /// </summary>
    /// <param name="delta">The amount of simulated time to advance.</param>
    public void Advance(TimeSpan delta)
    {
        List<FakeTimer> due = new();
        long now;
        lock (_gate)
        {
            _ticks += delta.Ticks;
            now = _ticks;
            foreach (var timer in _timers)
            {
                if (timer.IsScheduled && timer.DueTicks <= now)
                    due.Add(timer);
            }
        }

        foreach (var timer in due)
            timer.Fire();
    }

    /// <inheritdoc />
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new FakeTimer(this, callback, state);
        lock (_gate)
        {
            _timers.Add(timer);
        }

        timer.Change(dueTime, period);
        return timer;
    }

    private long CurrentTicks
    {
        get
        {
            lock (_gate)
            {
                return _ticks;
            }
        }
    }

    private void Remove(FakeTimer timer)
    {
        lock (_gate)
        {
            _timers.Remove(timer);
        }
    }

    /// <summary>
    /// A single-shot timer scheduled against a <see cref="FakeTimeProvider" />.
    /// </summary>
    private sealed class FakeTimer : ITimer
    {
        private readonly FakeTimeProvider _provider;
        private readonly TimerCallback _callback;
        private readonly object? _state;

        public FakeTimer(FakeTimeProvider provider, TimerCallback callback, object? state)
        {
            _provider = provider;
            _callback = callback;
            _state = state;
        }

        public bool IsScheduled { get; private set; }

        public long DueTicks { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            if (dueTime == Timeout.InfiniteTimeSpan)
            {
                IsScheduled = false;
                return true;
            }

            DueTicks = _provider.CurrentTicks + dueTime.Ticks;
            IsScheduled = true;
            return true;
        }

        public void Fire()
        {
            IsScheduled = false;
            _callback(_state);
        }

        public void Dispose() => _provider.Remove(this);

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
