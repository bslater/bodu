// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Argon2MatrixPoolTests.ManualTimeProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Argon2MatrixPoolTests
{
    /// <summary>
    /// A clock whose timestamp moves only when a test advances it, so idle release can be tested without waiting.
    /// </summary>
    private sealed class ManualTimeProvider
        : TimeProvider
    {
        /// <summary>The current timestamp, in ticks.</summary>
        private long _timestamp;

        /// <summary>
        /// Gets the number of timestamp ticks per second: one tick is one <see cref="TimeSpan" /> tick.
        /// </summary>
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        /// <summary>
        /// Returns the current timestamp.
        /// </summary>
        /// <returns>The timestamp the test last advanced to.</returns>
        public override long GetTimestamp() =>
            _timestamp;

        /// <summary>
        /// Gets the number of timers created on this clock.
        /// </summary>
        public int TimersCreated { get; private set; }

        /// <summary>
        /// Moves the clock forward.
        /// </summary>
        /// <param name="interval">The time to advance by.</param>
        public void Advance(TimeSpan interval) =>
            _timestamp += interval.Ticks;

        /// <summary>
        /// Creates a timer and counts it. The timer runs in real time, so it fires only after its due time elapses.
        /// </summary>
        /// <param name="callback">The callback the timer invokes.</param>
        /// <param name="state">The state passed to <paramref name="callback" />.</param>
        /// <param name="dueTime">The delay before the first invocation.</param>
        /// <param name="period">The interval between invocations.</param>
        /// <returns>The timer.</returns>
        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            TimersCreated++;
            return base.CreateTimer(callback, state, dueTime, period);
        }
    }
}
