// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncThrottler.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Admits at most one invocation per fixed interval, dropping any calls that arrive while the cool-down window is
/// still open (leading-edge throttling).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TryInvoke" /> returns <see langword="true" /> when a call is admitted and <see langword="false" /> when
/// it falls inside the cool-down window opened by the previous admitted call. The first call after construction is
/// always admitted. <see cref="TimeUntilNext" /> reports how long remains before the next call would be admitted.
/// </para>
/// <para>
/// Timing uses the supplied <see cref="TimeProvider" /> (defaulting to <see cref="TimeProvider.System" />), which
/// allows deterministic testing with a controllable provider. The type is safe to call concurrently from multiple
/// threads.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var throttle = new AsyncThrottler(TimeSpan.FromSeconds(1));
///
/// // Logs at most once per second regardless of call frequency.
/// if (throttle.TryInvoke())
///     Log(progress);
///]]>
/// </example>
[DebuggerDisplay("Interval = {_interval}")]
public sealed class AsyncThrottler
{
    private readonly object _gate = new();
    private readonly TimeSpan _interval;
    private readonly TimeProvider _timeProvider;
    private long _lastTimestamp;
    private bool _hasFired;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncThrottler" /> class.
    /// </summary>
    /// <param name="interval">The minimum interval between admitted invocations.</param>
    /// <param name="timeProvider">The time provider used to measure the interval, or <see langword="null" /> to use <see cref="TimeProvider.System" />.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="interval" /> is less than or equal to zero.</exception>
    public AsyncThrottler(TimeSpan interval, TimeProvider? timeProvider = null)
    {
        ThrowHelper.ThrowIfZeroOrNegative(interval.Ticks, nameof(interval));

        _interval = interval;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Attempts to admit an invocation.
    /// </summary>
    /// <returns><see langword="true" /> if the invocation is admitted; <see langword="false" /> if it falls within the current cool-down window.</returns>
    public bool TryInvoke()
    {
        lock (_gate)
        {
            var now = _timeProvider.GetTimestamp();
            if (_hasFired && _timeProvider.GetElapsedTime(_lastTimestamp, now) < _interval)
                return false;

            _hasFired = true;
            _lastTimestamp = now;
            return true;
        }
    }

    /// <summary>
    /// Gets the time remaining before the next invocation would be admitted.
    /// </summary>
    /// <value><see cref="TimeSpan.Zero" /> when an invocation can be admitted immediately; otherwise, the remaining cool-down.</value>
    /// <returns>The time remaining before the next invocation would be admitted.</returns>
    public TimeSpan TimeUntilNext
    {
        get
        {
            lock (_gate)
            {
                if (!_hasFired)
                    return TimeSpan.Zero;

                var remaining = _interval - _timeProvider.GetElapsedTime(_lastTimestamp);
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
    }
}
