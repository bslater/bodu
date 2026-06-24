// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateGate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics;

namespace Bodu.Threading;

/// <summary>
/// Provides a synchronous, leading-edge admission gate that admits at most one invocation per fixed interval, dropping
/// any calls that arrive while the cool-down window opened by the previous admitted call is still open.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="RateGate" /> is a <b>synchronous</b> gate: it has no <c>await</c> path of its own and does not queue,
/// schedule, or invoke any work. It is intended to guard <i>async</i> workflows by deciding, cheaply and without
/// blocking, whether a given trigger should proceed. <see cref="TryInvoke" /> returns <see langword="true" /> when a
/// call is admitted and <see langword="false" /> when it falls inside the current cool-down window; the first call
/// after construction is always admitted. <see cref="TimeUntilNext" /> reports how long remains before the next call
/// would be admitted.
/// </para>
/// <para>
/// Timing uses the supplied <see cref="TimeProvider" /> (defaulting to <see cref="TimeProvider.System" />), which
/// allows deterministic testing with a controllable provider. The type is safe to call concurrently from multiple
/// threads. It implements leading-edge throttling only: there is no trailing invocation, queued execution, or callback.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// var gate = new RateGate(TimeSpan.FromSeconds(1));
///
/// // Refreshes at most once per second regardless of call frequency.
/// if (gate.TryInvoke())
///     _ = RefreshAsync();
///]]>
/// </example>
[DebuggerDisplay("Interval = {_interval}, TimeUntilNext = {TimeUntilNext}")]
public sealed class RateGate
{
    private readonly object _gate = new();
    private readonly TimeSpan _interval;
    private readonly TimeProvider _timeProvider;
    private long _lastTimestamp;
    private bool _hasFired;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateGate" /> class.
    /// </summary>
    /// <param name="interval">The minimum interval between admitted invocations.</param>
    /// <param name="timeProvider">
    /// The time provider used to measure the interval, or <see langword="null" /> to use
    /// <see cref="TimeProvider.System" />.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="interval" /> is less than or equal to zero.
    /// </exception>
    public RateGate(TimeSpan interval, TimeProvider? timeProvider = null)
    {
        ThrowHelper.ThrowIfZeroOrNegative(interval.Ticks, nameof(interval));

        _interval = interval;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Attempts to admit an invocation.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the invocation is admitted; <see langword="false" /> if it falls within the current
    /// cool-down window.
    /// </returns>
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
    /// <value>
    /// <see cref="TimeSpan.Zero" /> when an invocation can be admitted immediately; otherwise, the remaining cool-down.
    /// </value>
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
