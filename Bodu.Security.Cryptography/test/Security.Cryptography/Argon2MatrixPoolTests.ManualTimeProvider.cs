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
        /// Moves the clock forward.
        /// </summary>
        /// <param name="interval">The time to advance by.</param>
        public void Advance(TimeSpan interval) =>
            _timestamp += interval.Ticks;
    }
}
